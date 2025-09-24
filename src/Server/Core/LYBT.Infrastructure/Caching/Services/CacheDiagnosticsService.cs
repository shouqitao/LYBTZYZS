using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Infrastructure.Caching.Interfaces;
using LYBT.Infrastructure.Caching.Models;
using LYBT.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Infrastructure.Caching.Services
{
    /// <summary>
    /// 缓存诊断服务实现
    /// </summary>
    public class CacheDiagnosticsService : ICacheDiagnosticsService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<CacheDiagnosticsService> _logger;
        private readonly CacheOptions _cacheOptions;
        private readonly ConcurrentQueue<CacheHealthSnapshot> _historySnapshots;
        private CacheHealthSnapshot _latestSnapshot;
        private CacheStatistics _previousStatistics;
        private readonly object _snapshotLock = new object();

        public CacheDiagnosticsService(
            ICacheService cacheService,
            ILogger<CacheDiagnosticsService> logger,
            IOptions<CacheOptions> cacheOptions)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheOptions = cacheOptions?.Value ?? new CacheOptions();
            _historySnapshots = new ConcurrentQueue<CacheHealthSnapshot>();
        }

        /// <inheritdoc/>
        public async Task<CacheHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var statistics = await _cacheService.GetStatisticsAsync(cancellationToken);
                var thresholdCheck = CheckThresholds(statistics);

                var healthLevel = DetermineHealthLevel(thresholdCheck);
                var recommendations = GenerateRecommendations(statistics, thresholdCheck);

                return new CacheHealthStatus
                {
                    IsHealthy = healthLevel == HealthLevel.Healthy,
                    Level = healthLevel,
                    Statistics = statistics,
                    ThresholdCheck = thresholdCheck,
                    Message = GenerateHealthMessage(healthLevel, thresholdCheck),
                    Recommendations = recommendations,
                    CheckTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取缓存健康状态失败");
                return new CacheHealthStatus
                {
                    IsHealthy = false,
                    Level = HealthLevel.Critical,
                    Message = $"诊断失败: {ex.Message}",
                    CheckTime = DateTime.UtcNow
                };
            }
        }

        /// <inheritdoc/>
        public async Task<CacheDiagnosticResult> RunDiagnosticsAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var healthStatus = await GetHealthStatusAsync(cancellationToken);
                var statistics = healthStatus.Statistics;

                // 计算性能指标
                var performance = new PerformanceMetrics
                {
                    HitRate = statistics.HitRatio,
                    EvictionRate = CalculateEvictionRate(
                        statistics,
                        _previousStatistics ?? statistics,
                        _cacheOptions.Monitoring.SamplingIntervalSeconds),
                    Throughput = CalculateThroughput(statistics),
                    AverageResponseTime = EstimateAverageResponseTime(statistics)
                };

                // 容量分析
                var capacity = new CapacityAnalysis
                {
                    UsedCapacity = statistics.CurrentItemCount,
                    MaxCapacity = statistics.MaxCapacity ?? _cacheOptions.Memory.SizeLimit ?? 10000,
                    UsageRatio = statistics.CapacityUsageRatio,
                    MemoryUsageBytes = statistics.TotalMemoryUsage,
                    EstimatedTimeToFull = EstimateTimeToFull(statistics, _previousStatistics)
                };

                // 记录快照
                var snapshot = new CacheHealthSnapshot
                {
                    Statistics = statistics,
                    HealthLevel = healthStatus.Level,
                    ThresholdCheck = healthStatus.ThresholdCheck,
                    SamplingWindowSeconds = _cacheOptions.Monitoring.SamplingIntervalSeconds
                };

                RecordSnapshot(snapshot);
                _previousStatistics = statistics;

                stopwatch.Stop();

                return new CacheDiagnosticResult
                {
                    DiagnosticTime = DateTime.UtcNow,
                    HealthStatus = healthStatus,
                    Performance = performance,
                    Capacity = capacity,
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "运行缓存诊断失败");
                stopwatch.Stop();

                return new CacheDiagnosticResult
                {
                    DiagnosticTime = DateTime.UtcNow,
                    HealthStatus = new CacheHealthStatus
                    {
                        IsHealthy = false,
                        Level = HealthLevel.Critical,
                        Message = $"诊断失败: {ex.Message}"
                    },
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
                };
            }
        }

        /// <inheritdoc/>
        public CacheHealthSnapshot GetLatestSnapshot()
        {
            lock (_snapshotLock)
            {
                return _latestSnapshot;
            }
        }

        /// <inheritdoc/>
        public ThresholdCheckResult CheckThresholds(CacheStatistics statistics)
        {
            var monitoring = _cacheOptions.Monitoring;

            return new ThresholdCheckResult
            {
                CurrentHitRate = statistics.HitRatio,
                HitRateThreshold = monitoring.HitRateThreshold,
                IsLowHitRate = statistics.HitRatio < monitoring.HitRateThreshold,

                CurrentCapacityRatio = statistics.CapacityUsageRatio,
                CapacityThreshold = monitoring.CapacityThreshold,
                IsHighCapacity = statistics.CapacityUsageRatio > monitoring.CapacityThreshold,

                CurrentEvictionRate = statistics.EvictionRate,
                EvictionRateThreshold = monitoring.EvictionRateThreshold,
                IsHighEvictionRate = statistics.EvictionRate > monitoring.EvictionRateThreshold
            };
        }

        /// <inheritdoc/>
        public void RecordSnapshot(CacheHealthSnapshot snapshot)
        {
            lock (_snapshotLock)
            {
                _latestSnapshot = snapshot;
            }

            _historySnapshots.Enqueue(snapshot);

            // 保持历史快照数量在限制内
            while (_historySnapshots.Count > _cacheOptions.Monitoring.HistorySnapshotCount)
            {
                _historySnapshots.TryDequeue(out _);
            }

            _logger.LogDebug("记录缓存健康快照 - 健康等级: {Level}, 命中率: {HitRate:P}, 容量: {Capacity:P}",
                snapshot.HealthLevel,
                snapshot.Statistics.HitRatio,
                snapshot.Statistics.CapacityUsageRatio);
        }

        /// <inheritdoc/>
        public IEnumerable<CacheHealthSnapshot> GetHistorySnapshots(int count = 10)
        {
            return _historySnapshots
                .OrderByDescending(s => s.SnapshotTime)
                .Take(count)
                .ToList();
        }

        /// <inheritdoc/>
        public double CalculateEvictionRate(CacheStatistics current, CacheStatistics previous, int intervalSeconds)
        {
            if (previous == null || intervalSeconds <= 0)
                return 0;

            var evictionDiff = current.EvictionCount - previous.EvictionCount;
            if (evictionDiff < 0)
                evictionDiff = 0; // 防止计数器重置

            // 计算每分钟的逐出率
            return (evictionDiff / (double)intervalSeconds) * 60;
        }

        /// <summary>
        /// 确定健康等级
        /// </summary>
        private HealthLevel DetermineHealthLevel(ThresholdCheckResult thresholdCheck)
        {
            if (!thresholdCheck.HasAnyAlert)
                return HealthLevel.Healthy;

            var alertCount = 0;
            if (thresholdCheck.IsLowHitRate) alertCount++;
            if (thresholdCheck.IsHighCapacity) alertCount++;
            if (thresholdCheck.IsHighEvictionRate) alertCount++;

            return alertCount switch
            {
                1 => HealthLevel.Warning,
                2 => HealthLevel.Degraded,
                _ => HealthLevel.Critical
            };
        }

        /// <summary>
        /// 生成健康消息
        /// </summary>
        private string GenerateHealthMessage(HealthLevel level, ThresholdCheckResult thresholdCheck)
        {
            if (level == HealthLevel.Healthy)
                return "缓存运行正常，所有指标在正常范围内";

            var issues = new List<string>();

            if (thresholdCheck.IsLowHitRate)
                issues.Add($"命中率低({thresholdCheck.CurrentHitRate:P} < {thresholdCheck.HitRateThreshold:P})");

            if (thresholdCheck.IsHighCapacity)
                issues.Add($"容量使用率高({thresholdCheck.CurrentCapacityRatio:P} > {thresholdCheck.CapacityThreshold:P})");

            if (thresholdCheck.IsHighEvictionRate)
                issues.Add($"逐出速率高({thresholdCheck.CurrentEvictionRate:F1}/分钟 > {thresholdCheck.EvictionRateThreshold})");

            return $"缓存{level}: {string.Join(", ", issues)}";
        }

        /// <summary>
        /// 生成建议
        /// </summary>
        private List<string> GenerateRecommendations(CacheStatistics statistics, ThresholdCheckResult thresholdCheck)
        {
            var recommendations = new List<string>();

            if (thresholdCheck.IsLowHitRate)
            {
                recommendations.Add("考虑调整缓存键策略或增加缓存预热");
                if (statistics.MissCount > statistics.HitCount * 2)
                {
                    recommendations.Add("缓存未命中过多，建议检查缓存键是否合理");
                }
            }

            if (thresholdCheck.IsHighCapacity)
            {
                recommendations.Add("缓存容量接近上限，建议增加缓存大小或优化缓存策略");
                recommendations.Add("考虑实施更积极的过期策略");
            }

            if (thresholdCheck.IsHighEvictionRate)
            {
                recommendations.Add("逐出频率过高，可能存在内存压力");
                recommendations.Add("建议增加缓存容量或减少缓存项大小");
            }

            if (statistics.ExpiredKeys > statistics.TotalKeys * 0.3)
            {
                recommendations.Add("过期键占比较高，考虑调整过期时间");
            }

            return recommendations;
        }

        /// <summary>
        /// 计算吞吐量
        /// </summary>
        private double CalculateThroughput(CacheStatistics statistics)
        {
            if (_previousStatistics == null)
                return 0;

            var timeDiff = (statistics.Timestamp - _previousStatistics.Timestamp).TotalSeconds;
            if (timeDiff <= 0)
                return 0;

            var requestDiff = statistics.TotalRequests - _previousStatistics.TotalRequests;
            return requestDiff / timeDiff;
        }

        /// <summary>
        /// 估算平均响应时间
        /// </summary>
        private double EstimateAverageResponseTime(CacheStatistics statistics)
        {
            // 基于命中率的简单估算
            // 命中时约0.1ms，未命中时约10ms
            var hitTime = 0.1;
            var missTime = 10.0;

            return statistics.HitRatio * hitTime + (1 - statistics.HitRatio) * missTime;
        }

        /// <summary>
        /// 估算容量满的时间
        /// </summary>
        private double? EstimateTimeToFull(CacheStatistics current, CacheStatistics previous)
        {
            if (previous == null || current.MaxCapacity == null)
                return null;

            var timeDiff = (current.Timestamp - previous.Timestamp).TotalMinutes;
            if (timeDiff <= 0)
                return null;

            var itemDiff = current.CurrentItemCount - previous.CurrentItemCount;
            if (itemDiff <= 0)
                return null;

            var growthRate = itemDiff / timeDiff; // 每分钟增长率
            var remainingCapacity = current.MaxCapacity.Value - current.CurrentItemCount;

            if (remainingCapacity <= 0)
                return 0;

            return remainingCapacity / growthRate;
        }
    }
}