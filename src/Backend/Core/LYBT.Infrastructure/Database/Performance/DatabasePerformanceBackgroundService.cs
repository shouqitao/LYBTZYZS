using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Infrastructure.Database.Performance
{
    /// <summary>
    /// 数据库性能监控后台服务 - UltraThink重构数据库优化
    /// 定期执行性能基准测试和健康检查
    /// </summary>
    public class DatabasePerformanceBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabasePerformanceBackgroundService> _logger;
        private readonly DatabasePerformanceOptions _options;

        public DatabasePerformanceBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<DatabasePerformanceBackgroundService> logger,
            IOptions<DatabasePerformanceOptions> options)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("数据库性能监控后台服务已启动");

            // 如果未启用自动基准测试，则退出
            if (!_options.AutoRunBenchmarks)
            {
                _logger.LogInformation("自动基准测试未启用，后台服务将退出");
                return;
            }

            // 等待一段时间后开始第一次检查（让系统启动完成）
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunPerformanceCheck(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // 正常的取消操作，忽略
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "执行性能检查时发生错误");
                }

                // 等待下一次检查
                var delay = TimeSpan.FromHours(_options.AutoRunIntervalHours);
                _logger.LogDebug("下一次性能检查将在 {Delay} 后执行", delay);
                
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("数据库性能监控后台服务已停止");
        }

        /// <summary>
        /// 执行性能检查
        /// </summary>
        private async Task RunPerformanceCheck(CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始定期性能检查...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var performanceService = scope.ServiceProvider.GetRequiredService<IDatabasePerformanceService>();

                // 运行基准测试
                var benchmarkResult = await performanceService.RunBenchmarkAsync();
                
                // 分析结果并记录警告
                AnalyzeBenchmarkResults(benchmarkResult);

                // 生成优化建议
                var recommendations = await performanceService.GetOptimizationRecommendationsAsync();
                
                if (recommendations.Any())
                {
                    _logger.LogWarning("发现 {Count} 条性能优化建议", recommendations.Count);
                    
                    var highPriorityRecommendations = recommendations.Where(r => r.Priority == "高").ToList();
                    if (highPriorityRecommendations.Any())
                    {
                        _logger.LogWarning("发现 {Count} 条高优先级性能问题需要关注", highPriorityRecommendations.Count);
                        foreach (var recommendation in highPriorityRecommendations.Take(3)) // 只记录前3条
                        {
                            _logger.LogWarning("性能问题: {Issue} - 建议: {Recommendation}", 
                                recommendation.Issue, recommendation.Recommendation);
                        }
                    }
                }

                _logger.LogInformation("性能检查完成 - 平均响应时间: {AvgTime:F2}ms, 成功率: {SuccessRate}%", 
                    benchmarkResult.AverageExecutionTime, 
                    (double)benchmarkResult.SuccessfulTests / benchmarkResult.TotalTests * 100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行性能检查时发生异常");
            }
        }

        /// <summary>
        /// 分析基准测试结果
        /// </summary>
        private void AnalyzeBenchmarkResults(PerformanceBenchmarkResult result)
        {
            if (result == null)
            {
                _logger.LogWarning("基准测试结果为空");
                return;
            }

            // 检查失败的测试
            if (result.FailedTests > 0)
            {
                _logger.LogWarning("基准测试中有 {FailedCount} 个测试失败", result.FailedTests);
            }

            // 检查平均响应时间
            if (result.AverageExecutionTime > _options.SlowQueryThresholdMs)
            {
                _logger.LogWarning("平均查询响应时间 {AvgTime:F2}ms 超过阈值 {Threshold}ms", 
                    result.AverageExecutionTime, _options.SlowQueryThresholdMs);
            }

            // 检查慢查询
            var allTests = result.GetAllTests();
            var slowQueries = allTests.Where(t => t.IsSuccessful && t.ExecutionTimeMs > _options.SlowQueryThresholdMs).ToList();
            
            if (slowQueries.Any())
            {
                _logger.LogWarning("发现 {SlowQueryCount} 个慢查询", slowQueries.Count);
                
                // 记录最慢的3个查询
                var slowestQueries = slowQueries.OrderByDescending(q => q.ExecutionTimeMs).Take(3);
                foreach (var query in slowestQueries)
                {
                    _logger.LogWarning("慢查询: {TestName} - {Duration}ms", 
                        query.TestName, query.ExecutionTimeMs);
                }
            }

            // 检查成功率
            var successRate = (double)result.SuccessfulTests / result.TotalTests;
            if (successRate < 0.95) // 成功率低于95%
            {
                _logger.LogWarning("查询成功率 {SuccessRate:P2} 低于预期", successRate);
            }

            // 检查总体测试时间
            if (result.TotalDuration.TotalMinutes > 5)
            {
                _logger.LogWarning("基准测试总耗时 {TotalTime:F2} 分钟过长", result.TotalDuration.TotalMinutes);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("正在停止数据库性能监控后台服务...");
            await base.StopAsync(cancellationToken);
        }
    }
}