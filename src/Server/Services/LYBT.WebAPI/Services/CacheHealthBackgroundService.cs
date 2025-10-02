using LYBT.Core.Infrastructure.Caching.Interfaces;
using LYBT.Core.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Options;

namespace LYBT.WebAPI.Services
{
    /// <summary>
    /// 缓存健康监控后台服务
    /// </summary>
    public class CacheHealthBackgroundService : BackgroundService
    {
        private readonly ICacheDiagnosticsService _diagnosticsService;
        private readonly ILogger<CacheHealthBackgroundService> _logger;
        private readonly CacheOptions _cacheOptions;
        private Timer _timer;
        private bool _isRunning;

        public CacheHealthBackgroundService(
            ICacheDiagnosticsService diagnosticsService,
            ILogger<CacheHealthBackgroundService> logger,
            IOptions<CacheOptions> cacheOptions)
        {
            _diagnosticsService = diagnosticsService ?? throw new ArgumentNullException(nameof(diagnosticsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheOptions = cacheOptions?.Value ?? new CacheOptions();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_cacheOptions.Monitoring.Enabled)
            {
                _logger.LogInformation("缓存监控已禁用，后台服务不会启动");
                return;
            }

            _logger.LogInformation("缓存健康监控服务启动，采样间隔: {Interval}秒",
                _cacheOptions.Monitoring.SamplingIntervalSeconds);

            // 延迟启动，等待应用程序完全启动
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            // 创建定时器
            var interval = TimeSpan.FromSeconds(_cacheOptions.Monitoring.SamplingIntervalSeconds);
            _timer = new Timer(
                async _ => await PerformHealthCheckAsync(stoppingToken),
                null,
                TimeSpan.Zero,
                interval);

            // 保持服务运行
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        /// <summary>
        /// 执行健康检查
        /// </summary>
        private async Task PerformHealthCheckAsync(CancellationToken cancellationToken)
        {
            if (_isRunning)
            {
                _logger.LogDebug("上一次健康检查尚未完成，跳过本次检查");
                return;
            }

            _isRunning = true;

            try
            {
                _logger.LogDebug("开始执行缓存健康检查");

                // 运行诊断
                var diagnosticResult = await _diagnosticsService.RunDiagnosticsAsync(cancellationToken);
                var healthStatus = diagnosticResult.HealthStatus;
                var thresholdCheck = healthStatus.ThresholdCheck;

                // 记录基本指标
                _logger.LogInformation("缓存健康检查完成 - 健康等级: {Level}, 命中率: {HitRate:P}, 容量: {Capacity:P}, 逐出率: {EvictionRate:F1}/分钟",
                    healthStatus.Level,
                    healthStatus.Statistics.HitRatio,
                    healthStatus.Statistics.CapacityUsageRatio,
                    diagnosticResult.Performance.EvictionRate);

                // 根据阈值检查结果记录告警
                if (thresholdCheck.HasAnyAlert)
                {
                    LogThresholdAlerts(thresholdCheck);
                }

                // 记录建议
                if (healthStatus.Recommendations?.Any() == true)
                {
                    foreach (var recommendation in healthStatus.Recommendations)
                    {
                        _logger.LogInformation("缓存优化建议: {Recommendation}", recommendation);
                    }
                }

                // 性能计数器（如果启用）
                if (_cacheOptions.Monitoring.EnablePerformanceCounters)
                {
                    LogPerformanceCounters(diagnosticResult);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行缓存健康检查时发生错误");
            }
            finally
            {
                _isRunning = false;
            }
        }

        /// <summary>
        /// 记录阈值告警
        /// </summary>
        private void LogThresholdAlerts(ThresholdCheckResult thresholdCheck)
        {
            if (thresholdCheck.IsLowHitRate)
            {
                var eventId = new EventId(_cacheOptions.Monitoring.EventIds.LowHitRate, "LowCacheHitRate");
                _logger.LogWarning(eventId,
                    "缓存命中率低于阈值 - 当前: {Current:P}, 阈值: {Threshold:P}, 采样窗口: {Window}秒",
                    thresholdCheck.CurrentHitRate,
                    thresholdCheck.HitRateThreshold,
                    _cacheOptions.Monitoring.SamplingIntervalSeconds);
            }

            if (thresholdCheck.IsHighCapacity)
            {
                var eventId = new EventId(_cacheOptions.Monitoring.EventIds.HighCapacity, "HighCacheCapacity");
                _logger.LogWarning(eventId,
                    "缓存容量使用率高于阈值 - 当前: {Current:P}, 阈值: {Threshold:P}, 采样窗口: {Window}秒",
                    thresholdCheck.CurrentCapacityRatio,
                    thresholdCheck.CapacityThreshold,
                    _cacheOptions.Monitoring.SamplingIntervalSeconds);
            }

            if (thresholdCheck.IsHighEvictionRate)
            {
                var eventId = new EventId(_cacheOptions.Monitoring.EventIds.HighEvictionRate, "HighEvictionRate");
                _logger.LogWarning(eventId,
                    "缓存逐出速率高于阈值 - 当前: {Current:F1}/分钟, 阈值: {Threshold}/分钟, 采样窗口: {Window}秒",
                    thresholdCheck.CurrentEvictionRate,
                    thresholdCheck.EvictionRateThreshold,
                    _cacheOptions.Monitoring.SamplingIntervalSeconds);
            }
        }

        /// <summary>
        /// 记录性能计数器
        /// </summary>
        private void LogPerformanceCounters(CacheDiagnosticResult diagnosticResult)
        {
            var performance = diagnosticResult.Performance;
            var capacity = diagnosticResult.Capacity;

            _logger.LogDebug(
                "缓存性能计数 - 吞吐量: {Throughput:F2}/秒, 平均响应: {ResponseTime:F2}ms, 内存使用: {Memory}MB, 预计满容: {TimeToFull}分钟",
                performance.Throughput,
                performance.AverageResponseTime,
                capacity.MemoryUsageBytes / (1024.0 * 1024.0),
                capacity.EstimatedTimeToFull?.ToString("F0") ?? "N/A");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("缓存健康监控服务正在停止");

            _timer?.Change(Timeout.Infinite, 0);
            _timer?.Dispose();

            await base.StopAsync(cancellationToken);

            _logger.LogInformation("缓存健康监控服务已停止");
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}
