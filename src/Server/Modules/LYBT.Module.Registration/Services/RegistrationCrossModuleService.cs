using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Models.Enums;
using RegistrationEntity = LYBT.Module.Registration.Domain.Registration;

namespace LYBT.Module.Registration.Services;

/// <summary>
/// 挂号域跨模块服务实现
/// 供 MedicalCase + Users 模块使用
/// </summary>
public class RegistrationCrossModuleService : IRegistrationCrossModuleService
{
    private readonly IRegistrationRepository _registrationRepository;

    public RegistrationCrossModuleService(
        IRegistrationRepository registrationRepository)
    {
        _registrationRepository = registrationRepository;
    }

    public async Task CompleteByMedicalCaseAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        var entity = await _registrationRepository.GetByMedicalCaseIdAsync(medicalCaseId, ct);
        if (entity is null) return;

        entity.Complete();
        await _registrationRepository.UpdateAsync(entity, ct);
        await _registrationRepository.SaveChangesAsync(ct);
    }

    public async Task HandleMedicalCaseCancelledAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        var entity = await _registrationRepository.GetByMedicalCaseIdAsync(medicalCaseId, ct);
        if (entity is null) return;

        if (entity.Source == RegistrationSource.Receptionist)
        {
            entity.RevertToWaiting();
        }
        else
        {
            entity.SoftDelete(entity.Id);
        }

        await _registrationRepository.UpdateAsync(entity, ct);
        await _registrationRepository.SaveChangesAsync(ct);
    }

    public async Task<Guid> StartVisitAsync(Guid registrationId, CancellationToken ct = default)
    {
        var entity = await _registrationRepository.GetByIdAsync(registrationId, ct);
        if (entity is null) return Guid.Empty;

        entity.StartVisit();
        await _registrationRepository.UpdateAsync(entity, ct);
        await _registrationRepository.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<int> GetWaitingCountByDoctorAsync(Guid doctorId, CancellationToken ct = default)
    {
        var queue = await _registrationRepository.GetWaitingQueueAsync(doctorId, ct);
        return queue.Count;
    }

    public async Task<bool> HasWaitingRegistrationAsync(Guid patientId, CancellationToken ct = default)
    {
        return await _registrationRepository.HasWaitingRegistrationAsync(patientId, ct);
    }

    public async Task LinkRegistrationToMedicalCaseAsync(Guid registrationId, Guid medicalCaseId, CancellationToken ct = default)
    {
        var registration = await _registrationRepository.GetByIdAsync(registrationId, ct);
        if (registration != null)
        {
            registration.AssignMedicalCase(medicalCaseId);
            await _registrationRepository.UpdateAsync(registration, ct);
            await _registrationRepository.SaveChangesAsync(ct);
        }
    }

    public async Task<Guid?> GetRegistrationIdByMedicalCaseIdAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        var registration = await _registrationRepository.GetByMedicalCaseIdAsync(medicalCaseId, ct);
        return registration?.Id;
    }
}


