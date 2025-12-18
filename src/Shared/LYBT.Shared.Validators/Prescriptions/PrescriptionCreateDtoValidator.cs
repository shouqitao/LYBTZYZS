using FluentValidation;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Shared.Validators.Prescriptions
{
    /// <summary>
    /// 处方创建DTO验证器
    /// </summary>
    public class PrescriptionCreateDtoValidator : AbstractValidator<PrescriptionCreateDto>
    {
        public PrescriptionCreateDtoValidator()
        {
            // OpenSpec: optimize-entity-data-flow - PatientId/DoctorId验证已移除
            // 这些字段通过MedicalCase聚合根获取，无需在处方层验证

            RuleFor(x => x.Diagnosis)
                .MaximumLength(500).WithMessage("诊断长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Diagnosis));

            RuleFor(x => x.Remark)
                .MaximumLength(1000).WithMessage("备注长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));

            RuleFor(x => x.Quantity)
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

    /// <summary>
    /// 处方明细DTO验证器
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
