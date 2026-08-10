using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Caching;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量删除药材命令处理器（软删除，含缓存失效）。
/// 骨架收敛至 <see cref="CatalogBatchOperationHandlerBase{TEntity,TCommand}"/>，本类保留药材特有钩子。
/// </summary>
public class BatchDeleteHerbsCommandHandler : CatalogBatchOperationHandlerBase<Herb, BatchDeleteHerbsCommand>
{
    private readonly ICacheInvalidationService _cacheInvalidation;

    public BatchDeleteHerbsCommandHandler(
        IHerbRepository herbRepository,
        ICacheInvalidationService cacheInvalidation)
        : base(herbRepository)
    {
        _cacheInvalidation = cacheInvalidation;
    }

    protected override Guid ResolveOperatorId(BatchDeleteHerbsCommand request) => request.CurrentUserId;

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
