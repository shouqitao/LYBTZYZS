using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Registration.Interfaces;

namespace LYBT.Module.Registration.Services;

/// <summary>
/// 挂号域跨模块服务实现
/// 供 MedicalCase + Users 模块使用
/// </summary>
public class RegistrationCrossModuleService : IRegistrationCrossModuleService
{
    private readonly IRegistrationService _registrationService;
    private readonly IRegistrationRepository _registrationRepository;
    
    public RegistrationCrossModuleService(
        IRegistrationService registrationService,
        IRegistrationRepository registrationRepository)
    {
        _registrationService = registrationService;
        _registrationRepository = registrationRepository;
    }
    
    public async Task CompleteByMedicalCaseAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        await _registrationService.CompleteByMedicalCaseAsync(medicalCaseId);
    }
    
    public async Task HandleMedicalCaseCancelledAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        await _registrationService.HandleMedicalCaseCancelledAsync(medicalCaseId);
    }
    
    public async Task<Guid> StartVisitAsync(Guid registrationId, CancellationToken ct = default)
    {
        var result = await _registrationService.StartVisitAsync(registrationId);
        return result.IsSuccess ? result.Data : Guid.Empty;
    }
    
    public async Task<int> GetWaitingCountByDoctorAsync(Guid doctorId, CancellationToken ct = default)
    {
        var result = await _registrationService.GetWaitingQueueAsync(doctorId);
        return result.IsSuccess ? result.Data.Count : 0;
    }
    
    public async Task<bool> HasWaitingRegistrationAsync(Guid patientId, CancellationToken ct = default)
    {
        var result = await _registrationService.GetPagedAsync(
            page: 1, 
            pageSize: 1, 
            patientId: patientId);
        
        return result.IsSuccess && 
               result.Data.Items.Any(r => r.Status == Shared.Models.Enums.RegistrationStatus.Waiting);
    }
    
    public async Task LinkRegistrationToMedicalCaseAsync(Guid registrationId, Guid medicalCaseId, CancellationToken ct = default)
    {
        var registration = await _registrationRepository.GetByIdAsync(registrationId, ct);
        if (registration != null)
        {
            registration.MedicalCaseId = medicalCaseId;
            await _registrationRepository.UpdateAsync(registration, ct);
        }
    }
    
    public async Task<Guid?> GetRegistrationIdByMedicalCaseIdAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        var registration = await _registrationRepository.GetByMedicalCaseIdAsync(medicalCaseId, ct);
        return registration?.Id;
    }
}
