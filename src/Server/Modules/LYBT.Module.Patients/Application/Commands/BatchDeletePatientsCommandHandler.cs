using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 批量删除患者命令处理器。
/// </summary>
public class BatchDeletePatientsCommandHandler
    : BatchOperationHandlerBase<Patient>,
      IRequestHandler<BatchDeletePatientsCommand, Result<BatchOperationResultDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMedicalCaseCrossModuleService _medicalCaseCrossModuleService;

    public BatchDeletePatientsCommandHandler(
        IPatientRepository patientRepository,
        IMedicalCaseCrossModuleService medicalCaseCrossModuleService)
    {
        _patientRepository = patientRepository;
        _medicalCaseCrossModuleService = medicalCaseCrossModuleService;
    }

    public Task<Result<BatchOperationResultDto>> Handle(
        BatchDeletePatientsCommand request, CancellationToken cancellationToken)
        => ExecuteBatchAsync(request.Ids, request.CurrentUserId, cancellationToken);

    protected override Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct)
        => _patientRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(Patient patient, CancellationToken ct)
        => _patientRepository.UpdateAsync(patient, ct);

    protected override Task ApplyOperationAsync(Patient patient, Guid operatorId, CancellationToken ct)
    {
        patient.SoftDelete(operatorId);
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => ErrorMessages.Get(ErrorCode.PatientNotFound);
    protected override string OperationName => "删除";
    protected override bool TrackIds => true;

    protected override async Task<string?> ValidateAsync(
        Patient patient, Guid id, Guid operatorId, CancellationToken ct)
    {
        var refCount = await _medicalCaseCrossModuleService.CountMedicalCasesAsync(id, ct);
        return refCount > 0 ? $"患者有 {refCount} 条医案记录，无法删除" : null;
    }

    protected override void FinalizeResult(BatchOperationResultDto result)
        => result.IsSuccess = result.SuccessCount > 0;

    protected override string BuildMessage(int successCount, int failureCount)
        => $"批量删除完成：成功 {successCount} 条，失败 {failureCount} 条";
}
