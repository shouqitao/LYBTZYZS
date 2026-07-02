using LYBT.SharedKernel.Events;

namespace LYBT.Module.Herbs.Domain.Events;

/// <summary>
/// 药材删除事件。当药材被软删除时触发。
/// </summary>
public sealed record HerbDeletedEvent(
    Guid HerbId,
    string Name,
    Guid DeletedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


