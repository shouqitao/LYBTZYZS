using LYBT.Infrastructure.SharedKernel.Events;

namespace LYBT.Module.Auth.Domain.Events;

/// <summary>
/// 会话撤销事件。当认证会话被撤销时触发。
/// </summary>
public sealed record SessionRevokedEvent(
    Guid SessionId,
    Guid UserId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


