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
    // ConsultationCompletedEvent / PrescriptionCompletedEvent 及载荷已于 2026-08-14 删除
    // （desktop-dead-code-cleanup 后续批次——工作区已改用 State 驱动，Prism 事件链无消费方）
}
