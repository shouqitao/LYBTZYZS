using LYBT.Infrastructure.SharedKernel.Events;

namespace LYBT.Module.Users.Domain.Events;

/// <summary>
/// 用户删除事件。当用户被软删除时触发。
/// </summary>
public sealed record UserDeletedEvent(
    Guid UserId,
    string UserName,
    string RealName,
    Guid DeletedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


