using MediatR;
using LYBT.Infrastructure.Caching;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.SharedKernel.Common;
using LYBT.Module.Herbs.Domain;
using LYBT.Module.Herbs.Interfaces;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 批量删除药材命令处理器（软删除）。
/// </summary>
public class BatchDeleteHerbsCommandHandler : IRequestHandler<BatchDeleteHerbsCommand, Result<BatchOperationResultDto>>
{
    private readonly IHerbRepository _herbRepository;
    private readonly ICacheInvalidationService _cacheInvalidation;

    public BatchDeleteHerbsCommandHandler(
        IHerbRepository herbRepository,
        ICacheInvalidationService cacheInvalidation)
    {
        _herbRepository = herbRepository;
        _cacheInvalidation = cacheInvalidation;
    }

    public async Task<Result<BatchOperationResultDto>> Handle(
        BatchDeleteHerbsCommand request, CancellationToken cancellationToken)
    {
        var result = new BatchOperationResultDto
        {
            TotalCount = request.Ids.Count,
            SuccessCount = 0,
            FailureCount = 0
        };

        foreach (var id in request.Ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var herb = await _herbRepository.GetByIdAsync(id, cancellationToken);
                if (herb == null)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "药材不存在"
                    });
                    continue;
                }

                herb.SoftDelete(request.CurrentUserId);
                await _herbRepository.UpdateAsync(herb, cancellationToken);

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

        if (result.SuccessCount > 0)
        {
            await _cacheInvalidation.InvalidateAsync("herbs");
        }

        return Result<BatchOperationResultDto>.Success(result);
    }
}


