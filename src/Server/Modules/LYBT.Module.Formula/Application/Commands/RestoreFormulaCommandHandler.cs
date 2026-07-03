using MediatR;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.SharedKernel.Common;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Application.Mappers;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 恢复已删除验方命令处理器。
/// </summary>
public class RestoreFormulaCommandHandler : IRequestHandler<RestoreFormulaCommand, Result<FormulaDetailDto>>
{
    private readonly IFormulaRepository _formulaRepository;

    public RestoreFormulaCommandHandler(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    public async Task<Result<FormulaDetailDto>> Handle(
        RestoreFormulaCommand request, CancellationToken cancellationToken)
    {
        var formula = await _formulaRepository.GetByIdIncludingDeletedAsync(request.FormulaId, cancellationToken);
        if (formula == null)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNotFound, "验方不存在");

        if (!formula.IsDeleted)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNotFound, "验方未被删除，无需恢复");

        var nameExists = await _formulaRepository.ExistsByNameAsync(formula.Name, formula.Id, cancellationToken);
        if (nameExists)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNameExists, $"验方名称「{formula.Name}」已存在，无法恢复");

        formula.Restore(request.OperatorId);

        await _formulaRepository.UpdateAsync(formula, cancellationToken);

        return Result<FormulaDetailDto>.Success(FormulaDtoMapper.ToDetailDto(formula));
    }
}
