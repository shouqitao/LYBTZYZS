using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LYBT.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.Infrastructure.Database.Performance
{
    /// <summary>
    /// 数据库性能监控服务 - UltraThink重构数据库优化
    /// 提供数据库性能监控、基准测试和优化建议功能
    /// </summary>
    public interface IDatabasePerformanceService
    {
        Task<PerformanceBenchmarkResult> RunBenchmarkAsync();
        Task<List<IndexUsageInfo>> GetIndexUsageAsync();
        Task<List<PerformanceRecommendation>> GetOptimizationRecommendationsAsync();
        Task<PerformanceReport> GeneratePerformanceReportAsync();
    }

    public class DatabasePerformanceService : IDatabasePerformanceService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cacheService;
        private readonly ILogger<DatabasePerformanceService> _logger;
        private readonly QueryPerformanceAnalyzer _analyzer;
        private readonly DatabasePerformanceOptions _options;

        public DatabasePerformanceService(
            AppDbContext context,
            IMemoryCache cacheService,
            ILogger<DatabasePerformanceService> logger,
            ILoggerFactory loggerFactory,
            IOptions<DatabasePerformanceOptions> options)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new DatabasePerformanceOptions();
            _analyzer = new QueryPerformanceAnalyzer(_context, loggerFactory.CreateLogger<QueryPerformanceAnalyzer>());
        }

        /// <summary>
        /// 运行性能基准测试
        /// </summary>
        public async Task<PerformanceBenchmarkResult> RunBenchmarkAsync()
        {
            _logger.LogInformation("开始执行数据库性能基准测试");

            try
            {
                var cacheKey = "performance:benchmark:latest";
                
                // 检查缓存中是否有最近的测试结果
                if (_options.EnableCaching)
                {
                    _cacheService.TryGetValue<PerformanceBenchmarkResult>(cacheKey, out var cachedResult);
                    if (cachedResult != null && 
                        (DateTime.UtcNow - cachedResult.TestEndTime).TotalMinutes < _options.CacheExpirationMinutes)
                    {
                        _logger.LogInformation("返回缓存的性能测试结果");
                        return cachedResult;
                    }
                }

                // 执行新的基准测试
                var result = await _analyzer.RunFullBenchmarkAsync();
                
                // 缓存结果
                if (_options.EnableCaching)
                {
                    _cacheService.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CacheExpirationMinutes));
                }

                // 记录测试结果摘要
                LogBenchmarkSummary(result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行性能基准测试时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 获取索引使用情况
        /// </summary>
        public async Task<List<IndexUsageInfo>> GetIndexUsageAsync()
        {
            _logger.LogInformation("开始分析数据库索引使用情况");

            try
            {
                var cacheKey = "performance:index-usage";
                
                if (_options.EnableCaching)
                {
                    _cacheService.TryGetValue<List<IndexUsageInfo>>(cacheKey, out var cachedUsage);
                    if (cachedUsage != null)
                    {
                        _logger.LogInformation("返回缓存的索引使用情况");
                        return cachedUsage;
                    }
                }

                var indexUsage = await _analyzer.AnalyzeIndexUsageAsync();
                
                if (_options.EnableCaching)
                {
                    _cacheService.Set(cacheKey, indexUsage, TimeSpan.FromMinutes(_options.CacheExpirationMinutes / 2));
                }

                _logger.LogInformation("索引分析完成，发现 {IndexCount} 个索引", indexUsage.Count);
                return indexUsage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析索引使用情况时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 获取优化建议
        /// </summary>
        public async Task<List<PerformanceRecommendation>> GetOptimizationRecommendationsAsync()
        {
            _logger.LogInformation("开始生成性能优化建议");

            try
            {
                var benchmarkResult = await RunBenchmarkAsync();
                var recommendations = _analyzer.GenerateRecommendations(benchmarkResult);
                
                // 添加索引相关建议
                var indexUsage = await GetIndexUsageAsync();
                AddIndexRecommendations(indexUsage, recommendations);

                _logger.LogInformation("生成了 {RecommendationCount} 条优化建议", recommendations.Count);
                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成优化建议时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 生成完整的性能报告
        /// </summary>
        public async Task<PerformanceReport> GeneratePerformanceReportAsync()
        {
            _logger.LogInformation("开始生成完整性能报告");

            try
            {
                var report = new PerformanceReport
                {
                    GeneratedAt = DateTime.UtcNow,
                    BenchmarkResult = await RunBenchmarkAsync(),
                    IndexUsage = await GetIndexUsageAsync(),
                    Recommendations = await GetOptimizationRecommendationsAsync()
                };

                // 计算性能指标
                CalculatePerformanceMetrics(report);

                _logger.LogInformation("性能报告生成完成");
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成性能报告时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 记录基准测试结果摘要
        /// </summary>
        private void LogBenchmarkSummary(PerformanceBenchmarkResult result)
        {
            _logger.LogInformation("=== 性能基准测试摘要 ===");
            _logger.LogInformation("测试时间: {Duration}ms", result.TotalDuration.TotalMilliseconds);
            _logger.LogInformation("总测试数: {Total}, 成功: {Success}, 失败: {Failed}", 
                result.TotalTests, result.SuccessfulTests, result.FailedTests);
            _logger.LogInformation("平均执行时间: {Average:F2}ms", result.AverageExecutionTime);

            // 记录慢查询
            var slowQueries = result.GetAllTests().Where(t => t.IsSuccessful && t.ExecutionTimeMs > _options.SlowQueryThresholdMs).ToList();
            if (slowQueries.Any())
            {
                _logger.LogWarning("发现 {Count} 个慢查询 (>{Threshold}ms):", slowQueries.Count, _options.SlowQueryThresholdMs);
                foreach (var query in slowQueries)
                {
                    _logger.LogWarning("- {TestName}: {Duration}ms", query.TestName, query.ExecutionTimeMs);
                }
            }
        }

        /// <summary>
        /// 添加索引相关建议
        /// </summary>
        private void AddIndexRecommendations(List<IndexUsageInfo> indexUsage, List<PerformanceRecommendation> recommendations)
        {
            // 查找未使用的索引
            var unusedIndexes = indexUsage.Where(i => !i.IsUsed && i.IndexType != "CLUSTERED").ToList();
            foreach (var unused in unusedIndexes)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Category = "索引优化",
                    TestName = $"{unused.TableName}.{unused.IndexName}",
                    Issue = "索引未被使用",
                    Recommendation = "考虑删除此索引以减少维护开销",
                    Priority = "低"
                });
            }

            // 查找Scan比Seek多的索引（可能需要优化）
            var inefficientIndexes = indexUsage.Where(i => i.IsUsed && i.UserScans > i.UserSeeks * 2).ToList();
            foreach (var inefficient in inefficientIndexes)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Category = "索引优化",
                    TestName = $"{inefficient.TableName}.{inefficient.IndexName}",
                    Issue = $"扫描次数({inefficient.UserScans})远大于查找次数({inefficient.UserSeeks})",
                    Recommendation = "检查查询模式，考虑优化索引结构",
                    Priority = "中"
                });
            }
        }

        /// <summary>
        /// 计算性能指标
        /// </summary>
        private void CalculatePerformanceMetrics(PerformanceReport report)
        {
            var metrics = new PerformanceMetrics();
            var tests = report.BenchmarkResult.GetAllTests().Where(t => t.IsSuccessful).ToList();

            if (tests.Any())
            {
                metrics.AverageQueryTime = tests.Average(t => t.ExecutionTimeMs);
                metrics.MedianQueryTime = CalculateMedian(tests.Select(t => (double)t.ExecutionTimeMs));
                metrics.MaxQueryTime = tests.Max(t => t.ExecutionTimeMs);
                metrics.MinQueryTime = tests.Min(t => t.ExecutionTimeMs);
                metrics.SlowQueriesCount = tests.Count(t => t.ExecutionTimeMs > _options.SlowQueryThresholdMs);
            }

            metrics.TotalIndexes = report.IndexUsage.Count;
            metrics.UsedIndexes = report.IndexUsage.Count(i => i.IsUsed);
            metrics.UnusedIndexes = report.IndexUsage.Count(i => !i.IsUsed);
            metrics.IndexEfficiencyRatio = metrics.TotalIndexes > 0 ? (double)metrics.UsedIndexes / metrics.TotalIndexes : 0;

            report.Metrics = metrics;
        }

        /// <summary>
        /// 计算中位数
        /// </summary>
        private double CalculateMedian(IEnumerable<double> values)
        {
            var sortedValues = values.OrderBy(v => v).ToList();
            if (!sortedValues.Any()) return 0;

            if (sortedValues.Count % 2 == 0)
            {
                var mid = sortedValues.Count / 2;
                return (sortedValues[mid - 1] + sortedValues[mid]) / 2;
            }
            else
            {
                return sortedValues[sortedValues.Count / 2];
            }
        }
    }

    /// <summary>
    /// 数据库性能服务配置选项
    /// </summary>
    public class DatabasePerformanceOptions
    {
        /// <summary>
        /// 是否启用结果缓存
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// 缓存过期时间（分钟）
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 30;

        /// <summary>
        /// 慢查询阈值（毫秒）
        /// </summary>
        public long SlowQueryThresholdMs { get; set; } = 1000;

        /// <summary>
        /// 是否自动运行基准测试
        /// </summary>
        public bool AutoRunBenchmarks { get; set; } = false;

        /// <summary>
        /// 自动运行间隔（小时）
        /// </summary>
        public int AutoRunIntervalHours { get; set; } = 24;
    }

    /// <summary>
    /// 完整的性能报告
    /// </summary>
    public class PerformanceReport
    {
        public DateTime GeneratedAt { get; set; }
        public PerformanceBenchmarkResult BenchmarkResult { get; set; }
        public List<IndexUsageInfo> IndexUsage { get; set; } = new();
        public List<PerformanceRecommendation> Recommendations { get; set; } = new();
        public PerformanceMetrics Metrics { get; set; } = new();
    }

    /// <summary>
    /// 性能指标摘要
    /// </summary>
    public class PerformanceMetrics
    {
        // 查询性能指标
        public double AverageQueryTime { get; set; }
        public double MedianQueryTime { get; set; }
        public long MaxQueryTime { get; set; }
        public long MinQueryTime { get; set; }
        public int SlowQueriesCount { get; set; }

        // 索引效率指标
        public int TotalIndexes { get; set; }
        public int UsedIndexes { get; set; }
        public int UnusedIndexes { get; set; }
        public double IndexEfficiencyRatio { get; set; }

        // 整体评分（0-100）
        public double OverallScore => CalculateOverallScore();

        private double CalculateOverallScore()
        {
            double score = 100;

            // 根据平均查询时间扣分
            if (AverageQueryTime > 100) score -= Math.Min(30, AverageQueryTime / 100 * 10);
            
            // 根据慢查询数量扣分
            if (SlowQueriesCount > 0) score -= Math.Min(20, SlowQueriesCount * 5);
            
            // 根据索引效率扣分
            score -= (1 - IndexEfficiencyRatio) * 20;

            return Math.Max(0, score);
        }
    }
}