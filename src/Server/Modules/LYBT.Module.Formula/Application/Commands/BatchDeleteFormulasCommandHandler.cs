using LYBT.Entities.Formulas;
using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Module.Formulas.Interfaces;

namespace LYBT.Module.Formulas.Application.Commands;

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

    public async Task<Result<BatchOperationResultDto>> Handle(
        BatchDeleteFormulasCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteBatchAsync(request.Ids, request.OperatorId, cancellationToken);
    }

    protected override Task<Formula?> GetByIdAsync(Guid id, CancellationToken ct)
        => _formulaRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(Formula formula, CancellationToken ct)
        => _formulaRepository.UpdateAsync(formula, ct);

    protected override Task ApplyOperationAsync(Formula formula, Guid operatorId, CancellationToken ct)
    {
        formula.SoftDelete(operatorId);
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => "方剂不存在";
    protected override string OperationName => "删除";
    protected override bool CatchExceptions => false;

    protected override void FinalizeResult(BatchOperationResultDto result)
        => result.IsSuccess = result.FailureCount == 0;

    protected override string BuildMessage(int successCount, int failureCount)
        => failureCount > 0
            ? $"成功删除 {successCount} 个，失败 {failureCount} 个"
            : $"成功删除 {successCount} 个方剂";
}
