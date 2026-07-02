using LYBT.SharedKernel.Events;

namespace LYBT.Module.Users.Domain.Events;

/// <summary>
/// 用户登录事件。当用户成功登录时触发。
/// </summary>
public sealed record UserLoggedInEvent(
    Guid UserId,
    string UserName,
    string IpAddress,
    string? UserAgent
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


