using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Infrastructure.BatchOperations;

/// <summary>
/// 批处理操作 Handler 基类（模板方法模式）。
/// 统一 循环→GetById→前置校验→变更→Update→累计 BatchOperationResultDto 的批处理骨架，
/// 子类仅实现实体访问、变更动作与差异钩子。
/// </summary>
public abstract class BatchOperationHandlerBase<TEntity>
    where TEntity : class
{
    // ===== 抽象成员：子类必须实现 =====

    /// <summary>按 ID 获取实体（不存在返回 null）。</summary>
    protected abstract Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>持久化实体变更。</summary>
    protected abstract Task UpdateAsync(TEntity entity, CancellationToken ct);

    /// <summary>执行单条变更（软删除 / 状态切换 / 字段赋值）。</summary>
    protected abstract Task ApplyOperationAsync(TEntity entity, Guid operatorId, CancellationToken ct);

    // ===== 可覆写成员：默认值满足多数场景，差异点按需覆写 =====

    /// <summary>实体不存在时的失败原因文案。</summary>
    protected virtual string EntityNotFoundMessage => "实体不存在";

    /// <summary>操作名（用于默认消息模板）。</summary>
    protected virtual string OperationName => "操作";

    /// <summary>实体显示名（用于失败项 Name 字段），默认 null。</summary>
    protected virtual string? GetEntityName(TEntity entity) => null;

    /// <summary>前置校验钩子：返回 null 表示通过，非 null 为失败原因（该条计入失败，不执行变更）。</summary>
    protected virtual Task<string?> ValidateAsync(TEntity entity, Guid id, Guid operatorId, CancellationToken ct)
        => Task.FromResult<string?>(null);

    /// <summary>是否记录 SuccessfulIds / FailedIds（部分模块需要）。</summary>
    protected virtual bool TrackIds => false;

    /// <summary>是否捕获异常（Formula 模块无 try-catch，需覆写为 false 保持行为等价）。</summary>
    protected virtual bool CatchExceptions => true;

    /// <summary>捕获的异常类型（Users BatchDelete 仅捕获 InvalidOperationException）。</summary>
    protected virtual Type CaughtExceptionType => typeof(Exception);

    /// <summary>批量完成后的钩子（缓存失效 / 领域事件派发）。</summary>
    protected virtual Task OnBatchCompletedAsync(BatchOperationResultDto result, CancellationToken ct)
        => Task.CompletedTask;

    /// <summary>结果收尾钩子（IsSuccess 语义），默认不调整（保持 true）。</summary>
    protected virtual void FinalizeResult(BatchOperationResultDto result) { }

    /// <summary>成功/失败计数的消息文案模板。</summary>
    protected virtual string BuildMessage(int successCount, int failureCount)
        => $"批量{OperationName}完成: 成功{successCount}个, 失败{failureCount}个";

    /// <summary>
    /// 批量执行模板方法：逐条 GetById → 前置校验 → 变更 → Update → 累计结果。
    /// </summary>
    protected async Task<Result<BatchOperationResultDto>> ExecuteBatchAsync(
        List<Guid> ids, Guid operatorId, CancellationToken ct)
    {
        var result = new BatchOperationResultDto { TotalCount = ids.Count };

        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            TEntity? entity = null;
            try
            {
                entity = await GetByIdAsync(id, ct);
                if (entity == null)
                {
                    RecordFailure(result, id, null, EntityNotFoundMessage);
                    continue;
                }

                var validationError = await ValidateAsync(entity, id, operatorId, ct);
                if (validationError != null)
                {
                    RecordFailure(result, id, GetEntityName(entity), validationError);
                    continue;
                }

                await ApplyOperationAsync(entity, operatorId, ct);
                await UpdateAsync(entity, ct);
                result.SuccessCount++;
                if (TrackIds) result.SuccessfulIds.Add(id);
            }
            catch (Exception ex) when (CatchExceptions && CaughtExceptionType.IsInstanceOfType(ex))
            {
                var reason = ex is AppException appEx && appEx.TypedErrorCode.HasValue
                    ? $"[{appEx.TypedErrorCode.Value.ToFormattedString()}] {ex.Message}"
                    : ex.Message;
                RecordFailure(result, id, entity is null ? null : GetEntityName(entity), reason);
            }
        }

        await OnBatchCompletedAsync(result, ct);
        FinalizeResult(result);
        result.Message = BuildMessage(result.SuccessCount, result.FailureCount);
        return Result<BatchOperationResultDto>.Success(result);
    }

    private void RecordFailure(BatchOperationResultDto result, Guid id, string? name, string reason)
    {
        result.FailureCount++;
        if (TrackIds) result.FailedIds.Add(id);
        result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = name, Reason = reason });
    }
}
