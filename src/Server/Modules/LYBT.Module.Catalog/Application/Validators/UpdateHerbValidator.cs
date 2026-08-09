using FluentValidation;
using LYBT.Module.Catalog.Application.Commands;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Catalog.Application.Validators;

/// <summary>
/// 更新药材命令验证器。
/// </summary>
public class UpdateHerbValidator : AbstractValidator<UpdateEntityCommand<HerbInputDto, HerbDetailDto>>
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
