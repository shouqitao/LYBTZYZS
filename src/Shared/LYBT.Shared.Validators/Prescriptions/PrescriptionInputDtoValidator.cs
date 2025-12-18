using FluentValidation;
using LYBT.Shared.Models.Contracts.Prescriptions;

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
            // MedicalCaseId验证 - 创建时必填
            RuleFor(x => x.MedicalCaseId)
                .NotEmpty().WithMessage("医疗案例ID不能为空")
                .When(x => !x.Id.HasValue);

            // 诊断验证
            RuleFor(x => x.Diagnosis)
                .MaximumLength(500).WithMessage("诊断长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Diagnosis));

            // 主治验证
            RuleFor(x => x.Indication)
                .MaximumLength(500).WithMessage("主治长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Indication));

            // 医嘱验证
            RuleFor(x => x.Advice)
                .MaximumLength(500).WithMessage("医嘱长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Advice));

            // 备注验证
            RuleFor(x => x.Remark)
                .MaximumLength(500).WithMessage("备注长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));

            // 折扣验证
            RuleFor(x => x.Discount)
                .InclusiveBetween(0, 1).WithMessage("折扣必须在0到1之间");

            // 剂数验证
            RuleFor(x => x.DosageCount)
                .GreaterThan(0).WithMessage("剂数必须大于0")
                .LessThanOrEqualTo(100).WithMessage("剂数不能超过100");

            // 处方项目验证
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
                .LessThanOrEqualTo(1000).WithMessage("用量不能超过1000克");

            RuleFor(x => x.Usage)
                .MaximumLength(200).WithMessage("用法长度不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.Usage));

            RuleFor(x => x.Remark)
                .MaximumLength(500).WithMessage("备注长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));
        }
    }
}
