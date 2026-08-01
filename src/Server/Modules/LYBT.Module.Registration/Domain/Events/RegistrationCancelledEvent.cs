using LYBT.Infrastructure.SharedKernel.Events;

namespace LYBT.Module.Registration.Domain.Events;

/// <summary>
/// 挂号取消事件。当挂号记录取消后发布。
/// </summary>
public sealed record RegistrationCancelledEvent(
    Guid RegistrationId,
    Guid PatientId,
    string PatientName,
    Guid DoctorId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}


