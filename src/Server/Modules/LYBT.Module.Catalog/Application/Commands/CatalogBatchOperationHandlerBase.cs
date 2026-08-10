using LYBT.Entities.Common;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 目录批处理命令处理器泛型基类（A-31-C3b 收敛 BatchEnable/Disable/Delete 孪生 Handler）。
/// 收敛 仓储注入 + GetById/Update 委托 + ExecuteBatchAsync 调用的公共骨架，
/// 子类仅实现变更动作（ApplyOperationAsync）与差异钩子（消息/缓存/结果语义）。
/// </summary>
/// <typeparam name="TEntity">目录实体（Herb / Formula）</typeparam>
/// <typeparam name="TCommand">批量命令（实现 <see cref="IBatchIdsCommand"/>）</typeparam>
public abstract class CatalogBatchOperationHandlerBase<TEntity, TCommand>
    : BatchOperationHandlerBase<TEntity>,
      IRequestHandler<TCommand, Result<BatchOperationResultDto>>
    where TEntity : BaseEntity
    where TCommand : IBatchIdsCommand
{
    private readonly ICatalogRepository<TEntity> _repository;

    protected CatalogBatchOperationHandlerBase(ICatalogRepository<TEntity> repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<Result<BatchOperationResultDto>> Handle(TCommand request, CancellationToken cancellationToken)
        => ExecuteBatchAsync(request.Ids, ResolveOperatorId(request), cancellationToken);

    /// <summary>批量操作的操作者 ID（Delete 用请求中的操作者，Enable/Disable 用 Guid.Empty）。</summary>
    protected abstract Guid ResolveOperatorId(TCommand request);

    protected override Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct)
        => _repository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(TEntity entity, CancellationToken ct)
        => _repository.UpdateAsync(entity, ct);
}
