using LYBT.Shared.Models.Enums;
using LYBT.Infrastructure.SharedKernel.Events;

namespace LYBT.Module.Users.Domain.Events;

/// <summary>
/// 用户创建事件。当新用户被创建时触发。
/// </summary>
public sealed record UserCreatedEvent(
    Guid UserId,
    string UserName,
    string RealName,
    UserRole Role,
    Guid? CreatedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


