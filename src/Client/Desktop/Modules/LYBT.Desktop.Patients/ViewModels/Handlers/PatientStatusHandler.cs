using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Handlers;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.Models;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.ViewModels.Handlers;

/// <summary>
/// 患者状态处理实现 (仅 Restore，无 Toggle)
/// </summary>
public class PatientStatusHandler : BaseStatusHandler<PatientListDto>, IPatientStatusHandler
{
    private readonly IPatientRepository _patientRepository;

    public PatientStatusHandler(
        IPatientRepository patientRepository,
        IMasterDetailServices<PatientListDto, PatientDetailModel> masterDetailServices,
        ILogger<PatientStatusHandler> logger)
        : base(masterDetailServices.Dialog, logger)
    {
        _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
    }

    protected override string EntityTypeName => "患者";
    protected override Guid GetEntityId(PatientListDto e) => e.Id;
    protected override string GetEntityDisplayName(PatientListDto e) => e.Name;

    protected override async Task<object?> ExecuteRestoreAsync(Guid id)
        => await _patientRepository.RestoreAsync(id);
}
