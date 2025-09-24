using System;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Infrastructure.Caching.Models;

namespace LYBT.Infrastructure.Caching.Interfaces
{
    /// <summary>
    /// 缓存诊断服务接口
    /// </summary>
    public interface ICacheDiagnosticsService
    {
        /// <summary>
        /// 获取缓存健康状态
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>缓存健康状态</returns>
        Task<CacheHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行缓存诊断
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>诊断结果</returns>
        Task<CacheDiagnosticResult> RunDiagnosticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取最近的健康快照
        /// </summary>
        /// <returns>健康快照</returns>
        CacheHealthSnapshot GetLatestSnapshot();

        /// <summary>
        /// 检查是否超过阈值
        /// </summary>
        /// <param name="statistics">缓存统计信息</param>
        /// <returns>阈值检查结果</returns>
        ThresholdCheckResult CheckThresholds(CacheStatistics statistics);

        /// <summary>
        /// 记录健康快照
        /// </summary>
        /// <param name="snapshot">健康快照</param>
        void RecordSnapshot(CacheHealthSnapshot snapshot);

        /// <summary>
        /// 获取历史快照
        /// </summary>
        /// <param name="count">获取数量</param>
        /// <returns>历史快照列表</returns>
        IEnumerable<CacheHealthSnapshot> GetHistorySnapshots(int count = 10);

        /// <summary>
        /// 计算逐出速率
        /// </summary>
        /// <param name="current">当前统计</param>
        /// <param name="previous">上次统计</param>
        /// <param name="intervalSeconds">间隔秒数</param>
        /// <returns>每分钟逐出速率</returns>
        double CalculateEvictionRate(CacheStatistics current, CacheStatistics previous, int intervalSeconds);
    }

    /// <summary>
    /// 缓存健康状态
    /// </summary>
    public class CacheHealthStatus
    {
        /// <summary>
        /// 是否健康
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// 健康等级
        /// </summary>
        public HealthLevel Level { get; set; }

        /// <summary>
        /// 缓存统计
        /// </summary>
        public CacheStatistics Statistics { get; set; }

        /// <summary>
        /// 阈值检查结果
        /// </summary>
        public ThresholdCheckResult ThresholdCheck { get; set; }

        /// <summary>
        /// 健康消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 建议操作
        /// </summary>
        public List<string> Recommendations { get; set; } = new();

        /// <summary>
        /// 检查时间
        /// </summary>
        public DateTime CheckTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 健康等级
    /// </summary>
    public enum HealthLevel
    {
        /// <summary>
        /// 健康
        /// </summary>
        Healthy = 0,

        /// <summary>
        /// 警告
        /// </summary>
        Warning = 1,

        /// <summary>
        /// 降级
        /// </summary>
        Degraded = 2,

        /// <summary>
        /// 严重
        /// </summary>
        Critical = 3
    }

    /// <summary>
    /// 缓存诊断结果
    /// </summary>
    public class CacheDiagnosticResult
    {
        /// <summary>
        /// 诊断ID
        /// </summary>
        public string DiagnosticId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 诊断时间
        /// </summary>
        public DateTime DiagnosticTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 健康状态
        /// </summary>
        public CacheHealthStatus HealthStatus { get; set; }

        /// <summary>
        /// 性能指标
        /// </summary>
        public PerformanceMetrics Performance { get; set; }

        /// <summary>
        /// 容量分析
        /// </summary>
        public CapacityAnalysis Capacity { get; set; }

        /// <summary>
        /// 诊断耗时（毫秒）
        /// </summary>
        public long ElapsedMilliseconds { get; set; }
    }

    /// <summary>
    /// 性能指标
    /// </summary>
    public class PerformanceMetrics
    {
        /// <summary>
        /// 命中率
        /// </summary>
        public double HitRate { get; set; }

        /// <summary>
        /// 平均响应时间（毫秒）
        /// </summary>
        public double AverageResponseTime { get; set; }

        /// <summary>
        /// 逐出速率（每分钟）
        /// </summary>
        public double EvictionRate { get; set; }

        /// <summary>
        /// 吞吐量（每秒请求数）
        /// </summary>
        public double Throughput { get; set; }
    }

    /// <summary>
    /// 容量分析
    /// </summary>
    public class CapacityAnalysis
    {
        /// <summary>
        /// 使用的容量
        /// </summary>
        public long UsedCapacity { get; set; }

        /// <summary>
        /// 最大容量
        /// </summary>
        public long MaxCapacity { get; set; }

        /// <summary>
        /// 使用率
        /// </summary>
        public double UsageRatio { get; set; }

        /// <summary>
        /// 预计满容时间（分钟）
        /// </summary>
        public double? EstimatedTimeToFull { get; set; }

        /// <summary>
        /// 内存使用（字节）
        /// </summary>
        public long MemoryUsageBytes { get; set; }
    }

    /// <summary>
    /// 阈值检查结果
    /// </summary>
    public class ThresholdCheckResult
    {
        /// <summary>
        /// 命中率是否低于阈值
        /// </summary>
        public bool IsLowHitRate { get; set; }

        /// <summary>
        /// 容量是否高于阈值
        /// </summary>
        public bool IsHighCapacity { get; set; }

        /// <summary>
        /// 逐出率是否高于阈值
        /// </summary>
        public bool IsHighEvictionRate { get; set; }

        /// <summary>
        /// 当前命中率
        /// </summary>
        public double CurrentHitRate { get; set; }

        /// <summary>
        /// 命中率阈值
        /// </summary>
        public double HitRateThreshold { get; set; }

        /// <summary>
        /// 当前容量使用率
        /// </summary>
        public double CurrentCapacityRatio { get; set; }

        /// <summary>
        /// 容量阈值
        /// </summary>
        public double CapacityThreshold { get; set; }

        /// <summary>
        /// 当前逐出速率
        /// </summary>
        public double CurrentEvictionRate { get; set; }

        /// <summary>
        /// 逐出速率阈值
        /// </summary>
        public double EvictionRateThreshold { get; set; }

        /// <summary>
        /// 是否有任何告警
        /// </summary>
        public bool HasAnyAlert => IsLowHitRate || IsHighCapacity || IsHighEvictionRate;
    }

    /// <summary>
    /// 缓存健康快照
    /// </summary>
    public class CacheHealthSnapshot
    {
        /// <summary>
        /// 快照ID
        /// </summary>
        public string SnapshotId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 快照时间
        /// </summary>
        public DateTime SnapshotTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 统计信息
        /// </summary>
        public CacheStatistics Statistics { get; set; }

        /// <summary>
        /// 健康等级
        /// </summary>
        public HealthLevel HealthLevel { get; set; }

        /// <summary>
        /// 阈值检查结果
        /// </summary>
        public ThresholdCheckResult ThresholdCheck { get; set; }

        /// <summary>
        /// 采样窗口（秒）
        /// </summary>
        public int SamplingWindowSeconds { get; set; }
    }
}