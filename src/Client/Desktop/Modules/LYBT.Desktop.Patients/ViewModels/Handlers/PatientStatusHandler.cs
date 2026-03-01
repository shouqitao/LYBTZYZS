using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.Models;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.ViewModels.Handlers;

/// <summary>
/// 患者状态处理实现
/// </summary>
public class PatientStatusHandler : IPatientStatusHandler
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMasterDetailServices<PatientListDto, PatientDetailModel> _masterDetailServices;
    private readonly ILogger<PatientStatusHandler> _logger;

    public PatientStatusHandler(
        IPatientRepository patientRepository,
        IMasterDetailServices<PatientListDto, PatientDetailModel> masterDetailServices,
        ILogger<PatientStatusHandler> logger)
    {
        _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
        _masterDetailServices = masterDetailServices ?? throw new ArgumentNullException(nameof(masterDetailServices));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> RestoreAsync(PatientListDto patient)
    {
        try
        {
            var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync(
                $"确认恢复患者 [{patient.Name}] 吗？", "恢复确认");
            if (!confirmed) return false;

            var result = await _patientRepository.RestoreAsync(patient.Id);
            if (result != null)
            {
                _logger.LogInformation("患者已恢复: {PatientName}", patient.Name);
                await _masterDetailServices.Dialog.ShowSuccessAsync($"患者 '{patient.Name}' 已恢复", "操作成功");
                return true;
            }

            await _masterDetailServices.Dialog.ShowErrorAsync("恢复患者失败", "操作失败");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复患者失败");
            await _masterDetailServices.Dialog.ShowErrorAsync("恢复患者失败", "操作失败");
            return false;
        }
    }
}
