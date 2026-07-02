using LYBT.Shared.Models.Enums;
using LYBT.SharedKernel.Events;

namespace LYBT.Module.Patients.Domain.Events;

/// <summary>
/// 患者状态变更事件。当患者启用/禁用时触发。
/// </summary>
public sealed record PatientStatusChangedEvent(
    Guid PatientId,
    string Name,
    CommonStatus OldStatus,
    CommonStatus NewStatus,
    Guid ChangedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


