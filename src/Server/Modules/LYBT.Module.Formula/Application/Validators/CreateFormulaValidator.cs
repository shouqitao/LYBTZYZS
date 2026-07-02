using FluentValidation;
using LYBT.Module.Formulas.Application.Commands;

namespace LYBT.Module.Formulas.Application.Validators;

/// <summary>
/// 创建验方命令验证器。
/// </summary>
public class CreateFormulaValidator : AbstractValidator<CreateFormulaCommand>
{
    /// <summary>
    /// 初始化验证规则。
    /// </summary>
    public CreateFormulaValidator()
    {
        RuleFor(x => x.Input.Name)
            .NotEmpty().WithMessage("验方名称不能为空")
            .MaximumLength(200).WithMessage("验方名称长度不能超过200个字符");

        RuleFor(x => x.Input.Effect)
            .MaximumLength(500).WithMessage("功用长度不能超过500个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.Effect));

        RuleFor(x => x.Input.Indications)
            .MaximumLength(1000).WithMessage("主治长度不能超过1000个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.Indications));

        RuleFor(x => x.Input.Usage)
            .MaximumLength(500).WithMessage("用法长度不能超过500个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.Usage));

        RuleFor(x => x.Input.Remark)
            .MaximumLength(500).WithMessage("备注长度不能超过500个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.Remark));

        RuleFor(x => x.Input.Property)
            .MaximumLength(300).WithMessage("性味归经长度不能超过300个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.Property));

        RuleFor(x => x.Input.Category)
            .MaximumLength(50).WithMessage("分类长度不能超过50个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.Category));
    }
}


