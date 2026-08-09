using LYBT.Entities.Formulas;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量启用药方命令处理器（A-31-C3b 统一收敛至 BatchOperationHandlerBase）。
/// </summary>
public class BatchEnableFormulasCommandHandler
    : BatchOperationHandlerBase<Formula>,
      IRequestHandler<BatchEnableFormulasCommand, Result<BatchOperationResultDto>>
{
    private readonly IFormulaRepository _formulaRepository;

    public BatchEnableFormulasCommandHandler(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    public Task<Result<BatchOperationResultDto>> Handle(
        BatchEnableFormulasCommand request, CancellationToken cancellationToken)
        => ExecuteBatchAsync(request.Ids, Guid.Empty, cancellationToken);

    protected override Task<Formula?> GetByIdAsync(Guid id, CancellationToken ct)
        => _formulaRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(Formula formula, CancellationToken ct)
        => _formulaRepository.UpdateAsync(formula, ct);

    protected override Task ApplyOperationAsync(Formula formula, Guid operatorId, CancellationToken ct)
    {
        formula.ChangeStatus(CommonStatus.Enabled, Guid.Empty);
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => "验方不存在";
    protected override string OperationName => "启用";
    protected override string? GetEntityName(Formula formula) => formula.Name;
}
