using MediatR;

namespace LYBT.Infrastructure.SharedKernel.Events;

/// <summary>
/// 领域事件分发器实现 — 基于 MediatR <see cref="IPublisher"/> 直投。
/// 注：ADR-0018 的 Outbox 模式待 v2.0，当前为同步 await 投递。
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;

    public DomainEventDispatcher(IPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        await _publisher.Publish(domainEvent, ct);
    }
}
