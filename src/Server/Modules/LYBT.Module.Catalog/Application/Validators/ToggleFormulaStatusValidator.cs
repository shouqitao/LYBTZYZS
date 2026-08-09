using FluentValidation;
using LYBT.Module.Catalog.Application.Commands;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Module.Catalog.Application.Validators;

/// <summary>
/// 切换验方状态命令验证器。
/// </summary>
public class ToggleFormulaStatusValidator : AbstractValidator<ToggleEntityStatusCommand<Formula, FormulaDetailDto>>
{
    public ToggleFormulaStatusValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("验方ID不能为空");
    }
}
