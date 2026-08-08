using FluentValidation;
using LYBT.Module.Formulas.Application.Commands;

namespace LYBT.Module.Formulas.Application.Validators;

/// <summary>
/// 恢复验方命令验证器。
/// </summary>
public class RestoreFormulaValidator : AbstractValidator<RestoreFormulaCommand>
{
    public RestoreFormulaValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("验方ID不能为空");
    }
}
