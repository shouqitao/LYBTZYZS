using FluentValidation;
using LYBT.Module.Herbs.Application.Commands;

namespace LYBT.Module.Herbs.Application.Validators;

/// <summary>
/// 更新药材命令验证器。
/// </summary>
public class UpdateHerbValidator : AbstractValidator<UpdateHerbCommand>
{
    public UpdateHerbValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("药材ID不能为空");

        RuleFor(x => x.Input.Name)
            .NotEmpty().WithMessage("药材名称不能为空")
            .MaximumLength(100).WithMessage("药材名称长度不能超过100个字符");

        RuleFor(x => x.Input.Price)
            .GreaterThanOrEqualTo(0).WithMessage("单价不能为负数");

        RuleFor(x => x.Input.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("成本价不能为负数")
            .When(x => x.Input.CostPrice.HasValue);
    }
}
