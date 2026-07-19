using FluentValidation;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Primitives.Validation;

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
                .MaximumLength(ValidationConstants.NameMaxLength).WithMessage("方剂名称长度不能超过{MaxLength}个字符");

            RuleFor(x => x.Effect)
                .MaximumLength(ValidationConstants.UsageMaxLength).WithMessage("功效长度不能超过{MaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Effect));

            RuleFor(x => x.Description)
                .MaximumLength(ValidationConstants.RemarkMaxLength).WithMessage("描述长度不能超过{MaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Usage)
                .MaximumLength(ValidationConstants.UsageMaxLength).WithMessage("用法长度不能超过{MaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Usage));

            RuleFor(x => x.Indications)
                .MaximumLength(ValidationConstants.UsageMaxLength).WithMessage("主治长度不能超过{MaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Indications));

            RuleFor(x => x.Remark)
                .MaximumLength(ValidationConstants.RemarkMaxLength).WithMessage("备注长度不能超过{MaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));

            RuleFor(x => x.Herbs)
                .NotNull().WithMessage("药材列表不能为空")
                .NotEmpty().WithMessage("方剂必须包含至少一味药材");

            RuleForEach(x => x.Herbs)
                .SetValidator(new FormulaHerbItemInputDtoValidator())
                .When(x => x.Herbs != null && x.Herbs.Any());
        }
    }

    /// <summary>
    /// 方剂药材项DTO验证器
    /// Issue #2014: 添加HerbName/Unit/ProcessingMethod验证，移除HerbId必填约束（支持延迟绑定）
    /// </summary>
    public class FormulaHerbItemInputDtoValidator : AbstractValidator<FormulaHerbItemInputDto>
    {
        public FormulaHerbItemInputDtoValidator()
        {
            RuleFor(x => x.HerbName)
                .NotEmpty().WithMessage("药材名称不能为空")
                .MaximumLength(ValidationConstants.NameMaxLength).WithMessage("药材名称长度不能超过{MaxLength}个字符");

            RuleFor(x => x.Dosage)
                .GreaterThan(0).WithMessage("用量必须大于0")
                .LessThanOrEqualTo((int)ValidationConstants.HerbDosageMaxValue).WithMessage("用量不能超过{ComparisonValue}克");

            RuleFor(x => x.Unit)
                .NotEmpty().WithMessage("单位不能为空")
                .MaximumLength(10).WithMessage("单位长度不能超过10个字符");

            RuleFor(x => x.ProcessingMethod)
                .MaximumLength(ValidationConstants.NameMaxLength).WithMessage("加工方法长度不能超过{MaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.ProcessingMethod));

            RuleFor(x => x.Usage)
                .MaximumLength(ValidationConstants.AddressMaxLength).WithMessage("用法长度不能超过{MaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Usage));
        }
    }
}
