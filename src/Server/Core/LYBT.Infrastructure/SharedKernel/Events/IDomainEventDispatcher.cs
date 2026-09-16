namespace LYBT.Infrastructure.SharedKernel.Events;

/// <summary>
/// 领域事件分发器（ADR-0018）。
/// 发布方只依赖本接口，不感知具体消费方。
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// 发布领域事件（当前为 MediatR 直投；Outbox 待 v2.0）。
    /// </summary>
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
