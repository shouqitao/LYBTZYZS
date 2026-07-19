using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Infrastructure.SharedKernel.Events;
using LYBT.Module.Formulas.Domain;
using LYBT.Module.Formulas.Domain.Events;
using LYBT.Module.Formulas.Interfaces;

namespace LYBT.Module.Formulas.Application.Commands;

public class BatchDeleteFormulasCommandHandler
    : IRequestHandler<BatchDeleteFormulasCommand, Result<BatchOperationResultDto>>
{
    private readonly IFormulaRepository _formulaRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

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
        var successCount = 0;
        var failCount = 0;
        var failedItems = new List<BatchOperationFailureItem>();
        var deletedNames = new List<string>();

        foreach (var id in request.Ids)
        {
            var formula = await _formulaRepository.GetByIdAsync(id, cancellationToken);
            if (formula == null)
            {
                failCount++;
                failedItems.Add(new BatchOperationFailureItem { Id = id, Reason = "方剂不存在" });
                continue;
            }

            formula.SoftDelete(request.OperatorId);
            await _formulaRepository.UpdateAsync(formula, cancellationToken);
            deletedNames.Add(formula.Name);
            successCount++;
        }

        if (deletedNames.Count > 0)
        {
            await _eventDispatcher.DispatchAsync(deletedNames.Select(name =>
                new FormulaDeletedEvent(Guid.Empty, name, request.OperatorId)
            ), cancellationToken);
        }

        var message = failCount > 0
            ? $"成功删除 {successCount} 个，失败 {failCount} 个"
            : $"成功删除 {successCount} 个方剂";

        return Result<BatchOperationResultDto>.Success(new BatchOperationResultDto
        {
            IsSuccess = failCount == 0,
            SuccessCount = successCount,
            FailureCount = failCount,
            TotalCount = request.Ids.Count,
            Message = message,
            FailedItems = failedItems
        });
    }
}


