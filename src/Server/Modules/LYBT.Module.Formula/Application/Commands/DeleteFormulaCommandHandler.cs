using MediatR;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Infrastructure.SharedKernel.Events;
using LYBT.Module.Formulas.Domain;
using LYBT.Module.Formulas.Domain.Events;
using LYBT.Module.Formulas.Interfaces;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 删除验方命令处理器（软删除）。
/// </summary>
public class DeleteFormulaCommandHandler : IRequestHandler<DeleteFormulaCommand, Result>
{
    private readonly IFormulaRepository _formulaRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    /// <summary>
    /// 初始化命令处理器。
    /// </summary>
    public DeleteFormulaCommandHandler(
        IFormulaRepository formulaRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _formulaRepository = formulaRepository;
        _eventDispatcher = eventDispatcher;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        DeleteFormulaCommand request, CancellationToken cancellationToken)
    {
        var formula = await _formulaRepository.GetByIdAsync(request.Id, cancellationToken);
        if (formula == null)
            return Result.Failure(ErrorCode.FormulaNotFound, "方剂不存在");

        formula.SoftDelete(request.CurrentUserId);

        await _formulaRepository.UpdateAsync(formula, cancellationToken);

        await _eventDispatcher.DispatchAsync(new[]
        {
            new FormulaDeletedEvent(formula.Id, formula.Name, request.CurrentUserId)
        }, cancellationToken);

        return Result.Success();
    }
}


