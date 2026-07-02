using LYBT.Shared.Models.Enums;
using LYBT.SharedKernel.Events;

namespace LYBT.Module.Users.Domain.Events;

/// <summary>
/// 用户角色变更事件。当用户角色被修改时触发。
/// </summary>
public sealed record UserRoleChangedEvent(
    Guid UserId,
    string UserName,
    UserRole OldRole,
    UserRole NewRole,
    Guid ChangedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


