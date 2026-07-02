using LYBT.Shared.Models.Enums;
using LYBT.SharedKernel.Events;

namespace LYBT.Module.Users.Domain.Events;

/// <summary>
/// 用户状态变更事件。当用户启用/禁用时触发。
/// </summary>
public sealed record UserStatusChangedEvent(
    Guid UserId,
    string UserName,
    CommonStatus OldStatus,
    CommonStatus NewStatus,
    Guid ChangedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


