using LYBT.Shared.Models.Enums;
using LYBT.SharedKernel.Events;

namespace LYBT.Module.Patients.Domain.Events;

/// <summary>
/// 患者创建事件。当新患者被创建时触发。
/// </summary>
public sealed record PatientCreatedEvent(
    Guid PatientId,
    string Name,
    Gender Gender,
    Guid? CreatedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


