using FluentValidation;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Module.Formula.Validators
{
    /// <summary>
    /// 验方创建DTO验证器
    /// </summary>
    public class FormulaInputDtoValidator : AbstractValidator<FormulaInputDto>
    {
        public FormulaInputDtoValidator()
        {
            // 验方名称必填且不超过100字符
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("验方名称不能为空")
                .MaximumLength(100).WithMessage("验方名称不能超过100个字符");

            // 功效描述长度限制（可选）
            RuleFor(x => x.Effect)
                .MaximumLength(200).WithMessage("功效描述不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.Effect));

            // 验方描述长度限制（可选）
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("验方描述不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.Description));

            // 用法长度限制（可选）
            RuleFor(x => x.Usage)
                .MaximumLength(200).WithMessage("用法描述不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.Usage));

            // 性味归经长度限制（可选）
            RuleFor(x => x.Property)
                .MaximumLength(200).WithMessage("性味归经不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.Property));

            // 验方分类长度限制（可选）
            RuleFor(x => x.Category)
                .MaximumLength(100).WithMessage("验方分类不能超过100个字符")
                .When(x => !string.IsNullOrEmpty(x.Category));

            // 用药指导长度限制（可选）
            RuleFor(x => x.Instructions)
                .MaximumLength(500).WithMessage("用药指导不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Instructions));

            // 主治症状长度限制（可选）
            RuleFor(x => x.Indications)
                .MaximumLength(500).WithMessage("主治症状不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Indications));

            // 禁忌症长度限制（可选）
            RuleFor(x => x.Contraindications)
                .MaximumLength(500).WithMessage("禁忌症不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Contraindications));

            // 制备方法长度限制（可选）
            RuleFor(x => x.Preparation)
                .MaximumLength(200).WithMessage("制备方法不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.Preparation));

            // 备注长度限制（可选）
            RuleFor(x => x.Remark)
                .MaximumLength(500).WithMessage("备注不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));

            // 中药材组成必填且至少一味
            RuleFor(x => x.Herbs)
                .NotEmpty().WithMessage("必须包含至少一味中药材")
                .Must(herbs => herbs != null && herbs.Count > 0)
                .WithMessage("必须包含至少一味中药材");

            // 验证每个药材项
            RuleForEach(x => x.Herbs).SetValidator(new FormulaHerbItemInputDtoValidator());
        }
    }

    /// <summary>
    /// 验方药材项创建DTO验证器
    /// </summary>
    public class FormulaHerbItemInputDtoValidator : AbstractValidator<FormulaHerbItemInputDto>
    {
        public FormulaHerbItemInputDtoValidator()
        {
            // 药材ID必填
            RuleFor(x => x.HerbId)
                .NotEmpty().WithMessage("中药材ID不能为空");

            // 用量必填且范围0.1-1000
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("用量必须大于0")
                .LessThanOrEqualTo(1000).WithMessage("用量不能超过1000");

            // 炮制方法长度限制（可选）
            RuleFor(x => x.Preparation)
                .MaximumLength(50).WithMessage("炮制方法不能超过50个字符")
                .When(x => !string.IsNullOrEmpty(x.Preparation));

            // 用法长度限制（可选）
            RuleFor(x => x.Usage)
                .MaximumLength(100).WithMessage("用法不能超过100个字符")
                .When(x => !string.IsNullOrEmpty(x.Usage));
        }
    }
}
