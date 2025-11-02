using FluentValidation;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Shared.Validators.Herbs
{
    /// <summary>
    /// 药材创建DTO验证器
    /// </summary>
    public class HerbInputDtoValidator : AbstractValidator<HerbInputDto>
    {
        public HerbInputDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("药材名称不能为空")
                .MaximumLength(100).WithMessage("药材名称长度不能超过100个字符");

            // Code字段已移除 - 改为自动生成
            // RuleFor(x => x.Code)
            //     .NotEmpty().WithMessage("药材编码不能为空")
            //     .MaximumLength(50).WithMessage("药材编码长度不能超过50个字符");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("单价必须大于0");

            // Stock字段已移除 - 库存管理不在MVP范围
            // RuleFor(x => x.Stock)
            //     .GreaterThanOrEqualTo(0).WithMessage("库存不能为负数")
            //     .When(x => x.Stock.HasValue);

            RuleFor(x => x.Remark)
                .MaximumLength(500).WithMessage("备注长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));
        }
    }
}
