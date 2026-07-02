using LYBT.SharedKernel.Events;

namespace LYBT.Module.Herbs.Domain.Events;

/// <summary>
/// 药材创建事件。当新药材被创建时触发。
/// </summary>
public sealed record HerbCreatedEvent(
    Guid HerbId,
    string Name,
    decimal Price,
    Guid? CreatedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


