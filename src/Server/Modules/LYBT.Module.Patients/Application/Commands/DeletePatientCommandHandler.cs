using MediatR;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Patients.Interfaces;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 删除患者命令处理器（软删除）。
/// </summary>
public class DeletePatientCommandHandler : IRequestHandler<DeletePatientCommand, Result>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMedicalCaseCrossModuleService _medicalCaseCrossModuleService;

    public DeletePatientCommandHandler(
        IPatientRepository patientRepository,
        IMedicalCaseCrossModuleService medicalCaseCrossModuleService)
    {
        _patientRepository = patientRepository;
        _medicalCaseCrossModuleService = medicalCaseCrossModuleService;
    }

    public async Task<Result> Handle(
        DeletePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.Id, cancellationToken);
        if (patient == null)
            return Result.Failure(ErrorCode.PatientNotFound, ErrorMessages.Get(ErrorCode.PatientNotFound));

        // P1-9（2026-08-14）: 所有权检查移入 Handler（原 Controller CheckOwnershipAsync）
        if (request.OperatorRole is not (UserRole.Admin or UserRole.SuperAdmin)
            && patient.CreatedBy != request.CurrentUserId)
            return Result.Failure(ErrorCode.Forbidden, "您没有权限操作此患者，只能操作自己创建的数据");

        // 被医案引用的患者不可删除（与批量删除逻辑一致）
        var refCount = await _medicalCaseCrossModuleService.CountMedicalCasesAsync(request.Id, cancellationToken);
        if (refCount > 0)
            return Result.Failure(ErrorCode.PatientHasActiveCases, $"患者有 {refCount} 条医案记录，无法删除");

        patient.SoftDelete(request.CurrentUserId);

        await _patientRepository.UpdateAsync(patient, cancellationToken);

        return Result.Success();
    }
}


