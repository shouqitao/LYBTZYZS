using FluentValidation;
using LYBT.Module.Formulas.Application.Commands;

namespace LYBT.Module.Formulas.Application.Validators;

/// <summary>
/// 更新验方命令验证器。
/// </summary>
public class UpdateFormulaValidator : AbstractValidator<UpdateFormulaCommand>
{
    public UpdateFormulaValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("验方ID不能为空");

        RuleFor(x => x.Input.Name)
            .NotEmpty().WithMessage("验方名称不能为空")
            .MaximumLength(200).WithMessage("验方名称长度不能超过200个字符");

        RuleFor(x => x.Input.Effect)
            .MaximumLength(500).WithMessage("功用长度不能超过500个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.Effect));

        RuleFor(x => x.Input.Indications)
            .MaximumLength(1000).WithMessage("主治长度不能超过1000个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.Indications));
    }
}
