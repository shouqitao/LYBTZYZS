using LYBT.Entities.Herbs;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Infrastructure.Caching;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量删除药材命令处理器（软删除）。
/// </summary>
public class BatchDeleteHerbsCommandHandler
    : BatchOperationHandlerBase<Herb>,
      IRequestHandler<BatchDeleteHerbsCommand, Result<BatchOperationResultDto>>
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

    public Task<Result<BatchOperationResultDto>> Handle(
        BatchDeleteHerbsCommand request, CancellationToken cancellationToken)
        => ExecuteBatchAsync(request.Ids, request.CurrentUserId, cancellationToken);

    protected override Task<Herb?> GetByIdAsync(Guid id, CancellationToken ct)
        => _herbRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(Herb herb, CancellationToken ct)
        => _herbRepository.UpdateAsync(herb, ct);

    protected override Task ApplyOperationAsync(Herb herb, Guid operatorId, CancellationToken ct)
    {
        herb.SoftDelete(operatorId);
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => ErrorMessages.Get(ErrorCode.HerbNotFound);
    protected override string OperationName => "删除";
    protected override bool TrackIds => true;

    protected override async Task OnBatchCompletedAsync(BatchOperationResultDto result, CancellationToken ct)
    {
        if (result.SuccessCount > 0)
            await _cacheInvalidation.InvalidateAsync("herbs");
    }

    protected override void FinalizeResult(BatchOperationResultDto result)
        => result.IsSuccess = result.SuccessCount > 0;

    protected override string BuildMessage(int successCount, int failureCount)
        => $"批量删除完成：成功 {successCount} 条，失败 {failureCount} 条";
}
