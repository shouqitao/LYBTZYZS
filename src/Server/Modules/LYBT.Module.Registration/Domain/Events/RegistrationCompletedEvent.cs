using MediatR;

namespace LYBT.Module.Registration.Domain.Events;

/// <summary>
/// 挂号完成事件。当挂号记录完成（医案完成联动）后发布。
/// </summary>
public sealed record RegistrationCompletedEvent(
    Guid RegistrationId,
    Guid PatientId,
    Guid DoctorId,
    Guid? MedicalCaseId) : INotification;


