using LYBT.Entities.Formulas;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量禁用药方命令处理器（A-31-C3b 统一收敛至 BatchOperationHandlerBase）。
/// </summary>
public class BatchDisableFormulasCommandHandler
    : BatchOperationHandlerBase<Formula>,
      IRequestHandler<BatchDisableFormulasCommand, Result<BatchOperationResultDto>>
{
    private readonly IFormulaRepository _formulaRepository;

    public BatchDisableFormulasCommandHandler(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    public Task<Result<BatchOperationResultDto>> Handle(
        BatchDisableFormulasCommand request, CancellationToken cancellationToken)
        => ExecuteBatchAsync(request.Ids, Guid.Empty, cancellationToken);

    protected override Task<Formula?> GetByIdAsync(Guid id, CancellationToken ct)
        => _formulaRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(Formula formula, CancellationToken ct)
        => _formulaRepository.UpdateAsync(formula, ct);

    protected override Task ApplyOperationAsync(Formula formula, Guid operatorId, CancellationToken ct)
    {
        formula.ChangeStatus(CommonStatus.Disabled, Guid.Empty);
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => "验方不存在";
    protected override string OperationName => "禁用";
    protected override string? GetEntityName(Formula formula) => formula.Name;
}
