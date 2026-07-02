using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.SharedKernel.Common;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Patients.Domain;
using LYBT.Module.Patients.Interfaces;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 批量删除患者命令处理器。
/// </summary>
public class BatchDeletePatientsCommandHandler : IRequestHandler<BatchDeletePatientsCommand, Result<BatchOperationResultDto>>
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

    public async Task<Result<BatchOperationResultDto>> Handle(
        BatchDeletePatientsCommand request, CancellationToken cancellationToken)
    {
        var result = new BatchOperationResultDto
        {
            TotalCount = request.Ids.Count,
            SuccessCount = 0,
            FailureCount = 0
        };

        foreach (var id in request.Ids)
        {
            try
            {
                var patient = await _patientRepository.GetByIdAsync(id, cancellationToken);
                if (patient == null)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "患者不存在"
                    });
                    continue;
                }

                var refCount = await _medicalCaseCrossModuleService.CountMedicalCasesAsync(id, cancellationToken);
                if (refCount > 0)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = $"患者有 {refCount} 条医案记录，无法删除"
                    });
                    continue;
                }

                patient.SoftDelete(request.CurrentUserId);
                await _patientRepository.UpdateAsync(patient, cancellationToken);

                result.SuccessCount++;
                result.SuccessfulIds.Add(id);
            }
            catch
            {
                result.FailureCount++;
                result.FailedIds.Add(id);
                result.FailedItems.Add(new BatchOperationFailureItem
                {
                    Id = id,
                    Reason = "删除操作失败"
                });
            }
        }

        result.IsSuccess = result.SuccessCount > 0;
        result.Message = $"批量删除完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条";

        return Result<BatchOperationResultDto>.Success(result);
    }
}


