using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Infrastructure.SharedKernel.Events;
using LYBT.Module.Formulas.Domain.Events;
using LYBT.Module.Formulas.Interfaces;

namespace LYBT.Module.Formulas.Application.Commands;

public class BatchDeleteFormulasCommandHandler
    : BatchOperationHandlerBase<LYBT.Entities.Formulas.Formula>,
      IRequestHandler<BatchDeleteFormulasCommand, Result<BatchOperationResultDto>>
{
    private readonly IFormulaRepository _formulaRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly List<string> _deletedNames = [];
    private Guid _operatorId;

    public BatchDeleteFormulasCommandHandler(
        IFormulaRepository formulaRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _formulaRepository = formulaRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Result<BatchOperationResultDto>> Handle(
        BatchDeleteFormulasCommand request, CancellationToken cancellationToken)
    {
        _operatorId = request.OperatorId;
        return await ExecuteBatchAsync(request.Ids, request.OperatorId, cancellationToken);
    }

    protected override Task<LYBT.Entities.Formulas.Formula?> GetByIdAsync(Guid id, CancellationToken ct)
        => _formulaRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(LYBT.Entities.Formulas.Formula formula, CancellationToken ct)
        => _formulaRepository.UpdateAsync(formula, ct);

    protected override Task ApplyOperationAsync(LYBT.Entities.Formulas.Formula formula, Guid operatorId, CancellationToken ct)
    {
        formula.SoftDelete(operatorId);
        _deletedNames.Add(formula.Name);
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => "方剂不存在";
    protected override string OperationName => "删除";
    protected override bool CatchExceptions => false;

    protected override async Task OnBatchCompletedAsync(BatchOperationResultDto result, CancellationToken ct)
    {
        if (_deletedNames.Count > 0)
        {
            await _eventDispatcher.DispatchAsync(_deletedNames.Select(name =>
                new FormulaDeletedEvent(Guid.Empty, name, _operatorId)
            ), ct);
        }
    }

    protected override void FinalizeResult(BatchOperationResultDto result)
        => result.IsSuccess = result.FailureCount == 0;

    protected override string BuildMessage(int successCount, int failureCount)
        => failureCount > 0
            ? $"成功删除 {successCount} 个，失败 {failureCount} 个"
            : $"成功删除 {successCount} 个方剂";
}
