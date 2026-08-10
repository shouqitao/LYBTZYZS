using LYBT.Entities.Formulas;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量删除验方命令处理器（软删除）。
/// 骨架收敛至 <see cref="CatalogBatchOperationHandlerBase{TEntity,TCommand}"/>，本类保留验方特有钩子
/// （CatchExceptions=false 与原实现一致：验方批量删除不捕获异常）。
/// </summary>
public class BatchDeleteFormulasCommandHandler : CatalogBatchOperationHandlerBase<Formula, BatchDeleteFormulasCommand>
{
    public BatchDeleteFormulasCommandHandler(IFormulaRepository formulaRepository)
        : base(formulaRepository)
    {
    }

    protected override Guid ResolveOperatorId(BatchDeleteFormulasCommand request) => request.OperatorId;

    protected override Task ApplyOperationAsync(Formula formula, Guid operatorId, CancellationToken ct)
    {
        formula.SoftDelete(operatorId);
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => ErrorMessages.Get(ErrorCode.FormulaNotFound);
    protected override string OperationName => "删除";
    protected override bool CatchExceptions => false;

    protected override void FinalizeResult(BatchOperationResultDto result)
        => result.IsSuccess = result.FailureCount == 0;

    protected override string BuildMessage(int successCount, int failureCount)
        => failureCount > 0
            ? $"成功删除 {successCount} 个，失败 {failureCount} 个"
            : $"成功删除 {successCount} 个方剂";
}
