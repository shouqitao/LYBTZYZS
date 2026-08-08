using MediatR;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Formulas.Interfaces;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 删除验方命令处理器（软删除）。
/// </summary>
public class DeleteFormulaCommandHandler : IRequestHandler<DeleteFormulaCommand, Result>
{
    private readonly IFormulaRepository _formulaRepository;

    /// <summary>
    /// 初始化命令处理器。
    /// </summary>
    public DeleteFormulaCommandHandler(
        IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
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

        return Result.Success();
    }
}


