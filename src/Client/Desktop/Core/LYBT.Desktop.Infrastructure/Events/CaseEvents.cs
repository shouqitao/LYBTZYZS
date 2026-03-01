using Prism.Events;

namespace LYBT.Desktop.Infrastructure.Events;

/// <summary>
/// 医案相关事件聚合类
/// 统一管理诊断完成、处方完成、工作区变更等事件
/// </summary>
/// <remarks>
/// Issue #unify-event-system: 统一事件系统架构
/// 所有医案相关的跨模块事件通过此类发布
/// </remarks>
public static class CaseEvents
{
    #region 诊断事件

    /// <summary>
    /// 诊断完成事件
    /// </summary>
    public class ConsultationCompletedEvent : PubSubEvent<CaseConsultationCompletedPayload> { }

    #endregion

    #region 处方事件

    /// <summary>
    /// 处方完成事件
    /// </summary>
    public class PrescriptionCompletedEvent : PubSubEvent<CasePrescriptionCompletedPayload> { }

    #endregion

    #region 工作区事件

    /// <summary>
    /// 工作区变更事件
    /// </summary>
    public class WorkspaceChangedEvent : PubSubEvent<WorkspaceChangedPayload> { }

    #endregion
}

/// <summary>
/// 诊断完成事件载荷
/// </summary>
/// <remarks>
/// 使用record类型符合事件Payload规范(EVENT-002)
/// Epic #2210 Phase 4: 用于4:6统一工作区的诊断面板与处方面板通信
/// </remarks>
public record CaseConsultationCompletedPayload
{
    /// <summary>
    /// 医案ID
    /// </summary>
    public required Guid MedicalCaseId { get; init; }

    /// <summary>
    /// 诊断ID（可选，如果有独立的诊断记录）
    /// </summary>
    public Guid? ConsultationId { get; init; }

    /// <summary>
    /// 是否需要开处方
    /// </summary>
    public bool NeedsPrescription { get; init; }

    /// <summary>
    /// 事件时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 处方完成事件载荷
/// </summary>
/// <remarks>
/// 使用record类型符合事件Payload规范(EVENT-002)
/// </remarks>
public record CasePrescriptionCompletedPayload
{
    /// <summary>
    /// 处方ID（后端创建后返回）
    /// </summary>
    public required Guid PrescriptionId { get; init; }

    /// <summary>
    /// 医案流程ID
    /// </summary>
    public Guid MedicalCaseFlowId { get; init; }

    /// <summary>
    /// 处方药品总数
    /// </summary>
    public int TotalItems { get; init; }

    /// <summary>
    /// 处方总金额
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// 是否挂起
    /// </summary>
    public bool IsSuspended { get; init; }

    /// <summary>
    /// 事件时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 工作区变更事件载荷
/// </summary>
/// <remarks>
/// 使用record类型符合事件Payload规范(EVENT-002)
/// </remarks>
public record WorkspaceChangedPayload
{
    /// <summary>
    /// 医案流程ID
    /// </summary>
    public Guid MedicalCaseFlowId { get; init; }

    /// <summary>
    /// 当前工作区状态
    /// </summary>
    public required string WorkspaceState { get; init; }

    /// <summary>
    /// 事件时间戳
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
