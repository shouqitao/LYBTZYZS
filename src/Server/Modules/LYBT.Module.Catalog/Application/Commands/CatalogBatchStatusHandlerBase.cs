using LYBT.Entities.Common;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 目录批量状态切换 Handler 基类（BatchEnable/BatchDisable 共用）。
/// 收敛 目标状态 + 操作名 + 实体不存在消息 的注入，子类仅提供状态变更动作与实体名。
/// </summary>
/// <typeparam name="TEntity">目录实体（Herb / Formula）</typeparam>
/// <typeparam name="TCommand">批量状态命令</typeparam>
public abstract class CatalogBatchStatusHandlerBase<TEntity, TCommand>
    : CatalogBatchOperationHandlerBase<TEntity, TCommand>
    where TEntity : BaseEntity
    where TCommand : IBatchIdsCommand
{
    private readonly CommonStatus _targetStatus;
    private readonly string _operationName;
    private readonly string _entityNotFoundMessage;

    protected CatalogBatchStatusHandlerBase(
        ICatalogRepository<TEntity> repository,
        CommonStatus targetStatus,
        string operationName,
        string entityNotFoundMessage)
        : base(repository)
    {
        _targetStatus = targetStatus;
        _operationName = operationName;
        _entityNotFoundMessage = entityNotFoundMessage;
    }

    protected override Guid ResolveOperatorId(TCommand request) => Guid.Empty;

    protected override string EntityNotFoundMessage => _entityNotFoundMessage;

    protected override string OperationName => _operationName;

    protected override Task ApplyOperationAsync(TEntity entity, Guid operatorId, CancellationToken ct)
    {
        ApplyStatusChange(entity);
        return Task.CompletedTask;
    }

    /// <summary>执行状态变更（子类调用实体 ChangeStatus）。</summary>
    protected abstract void ApplyStatusChange(TEntity entity);

    /// <summary>目标状态（启用/禁用）。</summary>
    protected CommonStatus TargetStatus => _targetStatus;
}
