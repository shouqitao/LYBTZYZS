using LYBT.Entities.Formulas;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量删除验方命令处理器（软删除）。
/// </summary>
public class BatchDeleteFormulasCommandHandler
    : BatchOperationHandlerBase<Formula>,
      IRequestHandler<BatchDeleteFormulasCommand, Result<BatchOperationResultDto>>
{
    private readonly IFormulaRepository _formulaRepository;

    public BatchDeleteFormulasCommandHandler(
        IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    public Task<Result<BatchOperationResultDto>> Handle(
        BatchDeleteFormulasCommand request, CancellationToken cancellationToken)
        => ExecuteBatchAsync(request.Ids, request.OperatorId, cancellationToken);

    protected override Task<Formula?> GetByIdAsync(Guid id, CancellationToken ct)
        => _formulaRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(Formula formula, CancellationToken ct)
        => _formulaRepository.UpdateAsync(formula, ct);

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
