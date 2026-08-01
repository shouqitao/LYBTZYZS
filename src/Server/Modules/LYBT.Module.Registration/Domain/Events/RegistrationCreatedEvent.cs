using LYBT.Infrastructure.SharedKernel.Events;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Registration.Domain.Events;

/// <summary>
/// 挂号创建事件。当新挂号记录创建后发布。
/// </summary>
public sealed record RegistrationCreatedEvent(
    Guid RegistrationId,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    RegistrationSource Source,
    RegistrationStatus Status,
    int QueueNumber) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


