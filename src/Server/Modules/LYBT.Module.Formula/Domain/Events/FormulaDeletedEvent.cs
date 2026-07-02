using LYBT.SharedKernel.Events;

namespace LYBT.Module.Formulas.Domain.Events;

/// <summary>
/// 验方删除事件。当验方被软删除时触发。
/// </summary>
public sealed record FormulaDeletedEvent(
    Guid FormulaId,
    string Name,
    Guid DeletedBy
) : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


