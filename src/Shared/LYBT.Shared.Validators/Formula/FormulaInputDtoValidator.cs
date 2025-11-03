using FluentValidation;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Shared.Validators.Formula
{
    /// <summary>
    /// 方剂创建DTO验证器
    /// </summary>
    public class FormulaInputDtoValidator : AbstractValidator<FormulaInputDto>
    {
        public FormulaInputDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("方剂名称不能为空")
                .MaximumLength(100).WithMessage("方剂名称长度不能超过100个字符");

            RuleFor(x => x.Effect)
                .MaximumLength(200).WithMessage("功效长度不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.Effect));

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("描述长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Usage)
                .MaximumLength(500).WithMessage("用法长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Usage));

            RuleFor(x => x.Indications)
                .MaximumLength(500).WithMessage("主治长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Indications));

            RuleFor(x => x.Remark)
                .MaximumLength(1000).WithMessage("备注长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));

            RuleFor(x => x.Herbs)
                .NotEmpty().WithMessage("方剂必须包含至少一味药材")
                .When(x => x.Herbs != null);

            RuleForEach(x => x.Herbs)
                .SetValidator(new FormulaHerbItemInputDtoValidator())
                .When(x => x.Herbs != null && x.Herbs.Any());
        }
    }

    /// <summary>
    /// 方剂药材项DTO验证器
    /// </summary>
    public class FormulaHerbItemInputDtoValidator : AbstractValidator<FormulaHerbItemInputDto>
    {
        public FormulaHerbItemInputDtoValidator()
        {
            RuleFor(x => x.HerbId)
                .NotEmpty().WithMessage("药材ID不能为空");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("用量必须大于0")
                .LessThanOrEqualTo(1000).WithMessage("用量不能超过1000克");

            RuleFor(x => x.Usage)
                .MaximumLength(200).WithMessage("用法长度不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.Usage));
        }
    }
}
