using LYBT.SharedKernel.Events;

namespace LYBT.Module.Formulas.Domain.Events;

/// <summary>
/// 验方创建事件。当新验方被创建时触发。
/// </summary>
public sealed record FormulaCreatedEvent(
    Guid FormulaId,
    string Name,
    Guid? CreatedBy
) : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


