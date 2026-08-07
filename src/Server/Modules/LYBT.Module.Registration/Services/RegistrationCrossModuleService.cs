using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Registrations.Interfaces;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Registrations.Services;

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
            // US-MC-014: 前台来源回退 Waiting（原医案已物理删除，患者回来重新接诊时新建）
            entity.RevertToWaiting();
        }
        else
        {
            // US-MC-014: 医生来源自动取消（闭环）
            entity.Cancel();
        }

        await _registrationRepository.UpdateAsync(entity, ct);
        await _registrationRepository.SaveChangesAsync(ct);
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

}


