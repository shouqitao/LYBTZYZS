using FluentValidation;
using LYBT.Module.Formulas.Application.Commands;

namespace LYBT.Module.Formulas.Application.Validators;

/// <summary>
/// 切换验方状态命令验证器。
/// </summary>
public class ToggleFormulaStatusValidator : AbstractValidator<ToggleFormulaStatusCommand>
{
    public ToggleFormulaStatusValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("验方ID不能为空");
    }
}
