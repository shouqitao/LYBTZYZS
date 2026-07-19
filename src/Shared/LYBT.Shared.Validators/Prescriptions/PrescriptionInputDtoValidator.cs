using FluentValidation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Primitives.Validation;

namespace LYBT.Shared.Validators.Prescriptions
{
    /// <summary>
    /// 处方输入DTO验证器 - 统一创建/更新验证
    /// OpenSpec: refactor-dto-simplification - 合并Create/Edit/Update验证器
    /// </summary>
    public class PrescriptionInputDtoValidator : AbstractValidator<PrescriptionInputDto>
    {
        public PrescriptionInputDtoValidator()
        {
            RuleFor(x => x.MedicalCaseId)
                .NotEmpty().WithMessage("医疗案例ID不能为空")
                .When(x => !x.Id.HasValue);

            RuleFor(x => x.ReferencedFormulas)
                .MaximumLength(ValidationConstants.UsageMaxLength).WithMessage("引用验方长度不能超过{MaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.ReferencedFormulas));

            RuleFor(x => x.Advice)
                .MaximumLength(ValidationConstants.UsageMaxLength).WithMessage("医嘱长度不能超过{MaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Advice));

            RuleFor(x => x.Remark)
                .MaximumLength(ValidationConstants.UsageMaxLength).WithMessage("备注长度不能超过{MaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));

            RuleFor(x => x.Discount)
                .InclusiveBetween(0, 1).WithMessage("折扣必须在0到1之间");

            RuleFor(x => x.DosageCount)
                .GreaterThan(ValidationConstants.DosageCountMinValue - 1).WithMessage("剂数必须大于0")
                .LessThanOrEqualTo(ValidationConstants.DosageCountMaxValue).WithMessage("剂数不能超过{ComparisonValue}");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("处方明细不能为空")
                .Must(items => items != null && items.Any()).WithMessage("处方必须包含至少一项药材");

            RuleForEach(x => x.Items)
                .SetValidator(new PrescriptionItemInputDtoValidator())
                .When(x => x.Items != null);
        }
    }

    /// <summary>
    /// 处方项目输入DTO验证器
    /// </summary>
    public class PrescriptionItemInputDtoValidator : AbstractValidator<PrescriptionItemInputDto>
    {
        public PrescriptionItemInputDtoValidator()
        {
            RuleFor(x => x.HerbId)
                .NotEmpty().WithMessage("药材ID不能为空");

            RuleFor(x => x.Dosage)
                .GreaterThan(0).WithMessage("用量必须大于0")
                .LessThanOrEqualTo((int)ValidationConstants.HerbDosageMaxValue).WithMessage("用量不能超过{ComparisonValue}克");

            RuleFor(x => x.Usage)
                .MaximumLength(ValidationConstants.AddressMaxLength).WithMessage("用法长度不能超过{MaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Usage));

            RuleFor(x => x.Remark)
                .MaximumLength(ValidationConstants.UsageMaxLength).WithMessage("备注长度不能超过{MaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));
        }
    }
}
