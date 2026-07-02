using LYBT.SharedKernel.Events;

namespace LYBT.Module.Users.Domain.Events;

/// <summary>
/// 用户登出事件。当用户登出时触发。
/// </summary>
public sealed record UserLoggedOutEvent(
    Guid UserId,
    string UserName,
    Guid SessionId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


