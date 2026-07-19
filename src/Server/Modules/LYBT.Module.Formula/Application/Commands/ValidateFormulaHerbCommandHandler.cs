using MediatR;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Formulas.Interfaces;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 验证验方药材命令处理器。
/// </summary>
public class ValidateFormulaHerbCommandHandler(
    IFormulaRepository formulaRepository,
    IHerbCrossModuleService herbCrossModuleService) : IRequestHandler<ValidateFormulaHerbCommand, Result>
{
    private readonly IFormulaRepository _formulaRepository = formulaRepository;
    private readonly IHerbCrossModuleService _herbCrossModuleService = herbCrossModuleService;

    public async Task<Result> Handle(
        ValidateFormulaHerbCommand request, CancellationToken cancellationToken)
    {
        var formula = await _formulaRepository.GetByIdAsync(request.FormulaId, cancellationToken);
        if (formula == null)
            return Result.Failure(ErrorCode.FormulaNotFound, "验方不存在");

        var herbItem = formula.Herbs.FirstOrDefault(h => h.Id == request.HerbItemId);
        if (herbItem == null)
            return Result.Failure(ErrorCode.FormulaValidationFailed, "药材项不存在");

        if (herbItem.IsValidated)
            return Result.Failure(ErrorCode.FormulaValidationFailed, "该药材已校验，无需重复操作");

        var selectedHerb = await _herbCrossModuleService.GetHerbBasicInfoAsync(request.SelectedHerbId, cancellationToken);
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
