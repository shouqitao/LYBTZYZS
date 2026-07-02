using MediatR;

namespace LYBT.SharedKernel.Events;

/// <summary>
/// 领域事件基接口。所有领域事件必须实现此接口。
/// 事件表示领域中发生的重要业务事件。
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// 事件唯一标识
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// 事件发生时间（UTC）
    /// </summary>
    DateTime OccurredOn { get; }
}


