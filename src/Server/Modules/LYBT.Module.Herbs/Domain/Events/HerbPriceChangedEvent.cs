using LYBT.SharedKernel.Events;

namespace LYBT.Module.Herbs.Domain.Events;

/// <summary>
/// 药材价格变更事件。当药材价格被修改时触发。
/// </summary>
public sealed record HerbPriceChangedEvent(
    Guid HerbId,
    string Name,
    decimal OldPrice,
    decimal NewPrice,
    Guid ChangedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


