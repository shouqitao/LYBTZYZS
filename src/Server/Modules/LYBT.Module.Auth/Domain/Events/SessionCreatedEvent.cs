using LYBT.Infrastructure.SharedKernel.Events;

namespace LYBT.Module.Auth.Domain.Events;

/// <summary>
/// 会话创建事件。当新认证会话被创建时触发。
/// </summary>
public sealed record SessionCreatedEvent(
    Guid SessionId,
    Guid UserId,
    string IpAddress
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


