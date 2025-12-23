using LYBT.Shared.Models.Contracts.Patients;
using Prism.Events;

namespace LYBT.Desktop.Infrastructure.Events;

/// <summary>
/// 患者相关事件聚合类
/// 统一管理患者创建、更新、选择等事件
/// </summary>
/// <remarks>
/// Issue #unify-event-system: 统一事件系统架构
/// 所有患者相关的跨模块事件通过此类发布
/// </remarks>
public static class PatientEvents
{
    #region CRUD事件

    /// <summary>
    /// 患者创建事件
    /// </summary>
    public class CreatedEvent : PubSubEvent<PatientCreatedPayload> { }

    /// <summary>
    /// 患者更新事件
    /// </summary>
    public class UpdatedEvent : PubSubEvent<PatientUpdatedPayload> { }

    #endregion

    #region 选择事件

    /// <summary>
    /// 患者选择事件
    /// </summary>
    public class SelectedEvent : PubSubEvent<PatientSelectedPayload> { }

    #endregion
}

/// <summary>
/// 患者创建事件载荷
/// </summary>
/// <remarks>
/// 使用record类型符合事件Payload规范(EVENT-002)
/// </remarks>
public record PatientCreatedPayload
{
    /// <summary>
    /// 创建的患者详情
    /// </summary>
    public required PatientDetailDto Patient { get; init; }

    /// <summary>
    /// 事件时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 创建来源（可选）
    /// </summary>
    public string? Source { get; init; }
}

/// <summary>
/// 患者更新事件载荷
/// </summary>
/// <remarks>
/// 使用record类型符合事件Payload规范(EVENT-002)
/// </remarks>
public record PatientUpdatedPayload
{
    /// <summary>
    /// 更新后的患者详情
    /// </summary>
    public required PatientDetailDto Patient { get; init; }

    /// <summary>
    /// 事件时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 更新来源（可选）
    /// </summary>
    public string? Source { get; init; }
}
