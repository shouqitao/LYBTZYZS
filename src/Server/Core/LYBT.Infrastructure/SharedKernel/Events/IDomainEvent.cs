using MediatR;

namespace LYBT.Infrastructure.SharedKernel.Events;

/// <summary>
/// 领域事件契约（ADR-0018）。
/// 所有跨模块状态变更通知实现本接口，经 <see cref="IDomainEventDispatcher"/> 发布，
/// 由消费方模块的 <c>INotificationHandler&lt;TEvent&gt;</c> 处理。
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>事件唯一标识（用于追踪/审计）</summary>
    Guid EventId { get; }

    /// <summary>事件发生时刻（UTC）</summary>
    DateTime OccurredOn { get; }
}
