using FluentValidation;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Validators
{
    /// <summary>
    /// 中药更新DTO验证器
    /// </summary>
    public class HerbUpdateDtoValidator : AbstractValidator<HerbUpdateDto>
    {
        public HerbUpdateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("药材名称不能为空")
                .MaximumLength(100).WithMessage("药材名称长度不能超过100个字符");

            // Code字段不存在于HerbUpdateDto中，已注释
            // RuleFor(x => x.Code)
            //     .NotEmpty().WithMessage("药材编码不能为空")
            //     .MaximumLength(50).WithMessage("药材编码长度不能超过50个字符");

            // HerbUpdateDto中没有Price字段，注释此规则
            // RuleFor(x => x.Price)
            //     .GreaterThan(0).WithMessage("单价必须大于0");

            // Stock字段不存在于HerbUpdateDto中，已注释  
            // RuleFor(x => x.Stock)
            //     .GreaterThanOrEqualTo(0).WithMessage("库存不能为负数");
        }
    }
}