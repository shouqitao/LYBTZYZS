using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 验证验方药材命令处理器。
/// </summary>
public class ValidateFormulaHerbCommandHandler(
    IFormulaRepository formulaRepository,
    IHerbRepository herbRepository) : IRequestHandler<ValidateFormulaHerbCommand, Result>
{
    private readonly IFormulaRepository _formulaRepository = formulaRepository;
    private readonly IHerbRepository _herbRepository = herbRepository;

    public async Task<Result> Handle(
        ValidateFormulaHerbCommand request, CancellationToken cancellationToken)
    {
        var formula = await _formulaRepository.GetByIdAsync(request.FormulaId, cancellationToken);
        if (formula == null)
            return Result.Failure(ErrorCode.FormulaNotFound, "验方不存在");

        var herbItem = formula.Herbs.FirstOrDefault(h => h.Id == request.HerbItemId);
        if (herbItem == null)
            return Result.Failure(ErrorCode.FormulaValidationFailed, ErrorMessages.Get(ErrorCode.FormulaHerbItemNotFound));

        if (herbItem.IsValidated)
            return Result.Failure(ErrorCode.FormulaValidationFailed, ErrorMessages.Get(ErrorCode.FormulaHerbItemAlreadyValidated));

        // T3.1: 改用模块内仓储直查（不再绕跨模块接口）
        var selectedHerb = await _herbRepository.GetByIdAsync(request.SelectedHerbId, cancellationToken);
        if (selectedHerb == null)
            return Result.Failure(ErrorCode.HerbNotFound, "所选药材不存在");

        herbItem.BindHerb(request.SelectedHerbId, selectedHerb.Name);

        if (formula.Herbs.All(h => h.IsValidated))
        {
            formula.Validate();
        }

        await _formulaRepository.UpdateAsync(formula, cancellationToken);

        return Result.Success();
    }
}
