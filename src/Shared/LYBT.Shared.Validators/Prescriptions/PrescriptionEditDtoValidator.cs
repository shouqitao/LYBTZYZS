using FluentValidation;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Shared.Validators.Prescriptions
{
    /// <summary>
    /// 处方编辑DTO验证器
    /// </summary>
    public class PrescriptionEditDtoValidator : AbstractValidator<PrescriptionEditDto>
    {
        public PrescriptionEditDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("处方ID不能为空");

            // OpenSpec: optimize-entity-data-flow - PatientId/UserId验证已移除
            // 这些字段通过MedicalCase聚合根获取，无需在处方层验证

            RuleFor(x => x.Diagnosis)
                .MaximumLength(500).WithMessage("诊断长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Diagnosis));

            RuleFor(x => x.Advice)
                .MaximumLength(500).WithMessage("医嘱长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Advice));

            RuleFor(x => x.Remark)
                .MaximumLength(1000).WithMessage("备注长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));

            RuleFor(x => x.Discount)
                .InclusiveBetween(0, 1).WithMessage("折扣必须在0到1之间");

            RuleFor(x => x.DosageCount)
                .GreaterThan(0).WithMessage("剂数必须大于0")
                .LessThanOrEqualTo(100).WithMessage("剂数不能超过100");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("处方明细不能为空")
                .Must(items => items != null && items.Any()).WithMessage("处方必须包含至少一项药材")
                .When(x => x.Items != null);

            RuleForEach(x => x.Items)
                .SetValidator(new PrescriptionItemInputDtoValidator())
                .When(x => x.Items != null);
        }
    }
}
