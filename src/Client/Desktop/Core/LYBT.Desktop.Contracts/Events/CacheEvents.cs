using Prism.Events;

namespace LYBT.Desktop.Contracts.Events;

/// <summary>
/// 缓存失效事件聚合类
/// 用于跨模块的缓存失效通知
/// </summary>
public static class CacheEvents
{
    /// <summary>
    /// 缓存失效事件 -- 通知所有缓存订阅者清理指定域的缓存
    /// </summary>
    public class InvalidatedEvent : PubSubEvent<CacheInvalidatedPayload> { }
}

/// <summary>
/// 缓存失效事件载荷
/// </summary>
public record CacheInvalidatedPayload
{
    /// <summary>
    /// 失效的缓存域 (Patients/MedicalCases/All)
    /// </summary>
    public required CacheDomain Domain { get; init; }

    /// <summary>
    /// 失效原因
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// 事件时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 缓存域枚举
/// </summary>
public enum CacheDomain
{
    /// <summary>患者相关缓存</summary>
    Patients,

    /// <summary>医案相关缓存</summary>
    MedicalCases,

    /// <summary>药材相关缓存</summary>
    Herbs,

    /// <summary>验方相关缓存</summary>
    Formulas,

    /// <summary>用户相关缓存</summary>
    Users,

    /// <summary>全部缓存 (Sync 后使用)</summary>
    All
}
