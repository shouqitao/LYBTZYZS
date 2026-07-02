using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Application.Commands;

public class BatchDeleteMedicalCasesCommandHandler(
    IMedicalCaseRepository repository,
    ILogger<BatchDeleteMedicalCasesCommandHandler> logger
) : IRequestHandler<BatchDeleteMedicalCasesCommand, Result<BatchOperationResultDto>>
{
    public async Task<Result<BatchOperationResultDto>> Handle(
        BatchDeleteMedicalCasesCommand request, CancellationToken cancellationToken)
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
                var medicalCase = await repository.GetByIdAsync(id, cancellationToken);
                if (medicalCase == null)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "医案不存在"
                    });
                    continue;
                }

                medicalCase.SoftDelete();
                await repository.UpdateAsync(medicalCase, cancellationToken);

                result.SuccessCount++;
                result.SuccessfulIds.Add(id);
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedIds.Add(id);
                result.FailedItems.Add(new BatchOperationFailureItem
                {
                    Id = id,
                    Reason = "删除操作失败"
                });
                logger.LogError(ex, "[CMD] BatchDelete → ItemFailed - MedicalCaseId={MedicalCaseId}", id);
            }
        }

        result.IsSuccess = result.SuccessCount > 0;
        result.Message = $"批量删除完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条";

        return Result<BatchOperationResultDto>.Success(result);
    }
}


