using LYBT.SharedKernel.Events;

namespace LYBT.Module.Formulas.Domain.Events;

/// <summary>
/// 验方验证事件。当验方被标记为已验证时触发。
/// </summary>
public sealed record FormulaValidatedEvent(
    Guid FormulaId,
    string Name,
    Guid? ValidatedBy
) : IDomainEvent
{
    /// <inheritdoc/>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


