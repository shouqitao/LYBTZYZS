using FluentValidation;
using LYBT.Module.Herbs.Application.Commands;

namespace LYBT.Module.Herbs.Application.Validators;

/// <summary>
/// 创建药材命令验证器。
/// </summary>
public class CreateHerbValidator : AbstractValidator<CreateHerbCommand>
{
    public CreateHerbValidator()
    {
        RuleFor(x => x.Input.Name)
            .NotEmpty().WithMessage("药材名称不能为空")
            .MaximumLength(100).WithMessage("药材名称长度不能超过100个字符");

        RuleFor(x => x.Input.Unit)
            .NotEmpty().WithMessage("单位不能为空")
            .MaximumLength(20).WithMessage("单位长度不能超过20个字符");

        RuleFor(x => x.Input.Price)
            .GreaterThanOrEqualTo(0).WithMessage("单价不能为负数");

        RuleFor(x => x.Input.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("成本价不能为负数")
            .When(x => x.Input.CostPrice.HasValue);

        RuleFor(x => x.Input.PinYinCode)
            .MaximumLength(50).WithMessage("拼音码长度不能超过50个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.PinYinCode));

        RuleFor(x => x.Input.Category)
            .MaximumLength(50).WithMessage("分类长度不能超过50个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.Category));

        RuleFor(x => x.Input.Properties)
            .MaximumLength(100).WithMessage("性味长度不能超过100个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.Properties));

        RuleFor(x => x.Input.Origin)
            .MaximumLength(100).WithMessage("产地长度不能超过100个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.Origin));

        RuleFor(x => x.Input.Spec)
            .MaximumLength(100).WithMessage("规格长度不能超过100个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.Spec));

        RuleFor(x => x.Input.Effect)
            .MaximumLength(500).WithMessage("功效说明长度不能超过500个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.Effect));

        RuleFor(x => x.Input.Usage)
            .MaximumLength(500).WithMessage("用法用量长度不能超过500个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.Usage));

        RuleFor(x => x.Input.Remark)
            .MaximumLength(500).WithMessage("备注长度不能超过500个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.Remark));
    }
}


