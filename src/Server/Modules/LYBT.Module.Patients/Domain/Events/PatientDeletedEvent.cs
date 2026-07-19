using LYBT.Infrastructure.SharedKernel.Events;

namespace LYBT.Module.Patients.Domain.Events;

/// <summary>
/// 患者删除事件。当患者被软删除时触发。
/// </summary>
public sealed record PatientDeletedEvent(
    Guid PatientId,
    string Name,
    Guid DeletedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


