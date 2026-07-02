using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Application.Mappers;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 切换患者状态命令处理器。
/// </summary>
public class TogglePatientStatusCommandHandler : IRequestHandler<TogglePatientStatusCommand, Result<PatientDetailDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMedicalCaseCrossModuleService _medicalCaseCrossModuleService;

    public TogglePatientStatusCommandHandler(
        IPatientRepository patientRepository,
        IMedicalCaseCrossModuleService medicalCaseCrossModuleService)
    {
        _patientRepository = patientRepository;
        _medicalCaseCrossModuleService = medicalCaseCrossModuleService;
    }

    public async Task<Result<PatientDetailDto>> Handle(
        TogglePatientStatusCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.Id, cancellationToken);
        if (patient == null)
            return Result<PatientDetailDto>.Failure(ErrorCode.PatientNotFound, "患者不存在");

        if (patient.Status == CommonStatus.Enabled)
        {
            var unfinishedCount = await _medicalCaseCrossModuleService.CountUnfinishedMedicalCasesAsync(request.Id, cancellationToken);
            if (unfinishedCount > 0)
            {
                return Result<PatientDetailDto>.Failure(
                    ErrorCode.PatientHasActiveCases,
                    $"该患者有 {unfinishedCount} 条进行中的医案，请先完成或取消后再禁用");
            }
        }

        patient.ChangeStatus(
            patient.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled,
            request.CurrentUserId);

        await _patientRepository.UpdateAsync(patient, cancellationToken);

        return Result<PatientDetailDto>.Success(PatientMapper.ToDetailDto(patient));
    }
}


