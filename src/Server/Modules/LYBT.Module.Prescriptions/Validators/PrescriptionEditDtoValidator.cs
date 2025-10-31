using FluentValidation;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Validators
{
    /// <summary>
    /// 处方编辑DTO验证器
    /// </summary>
    public class PrescriptionEditDtoValidator : AbstractValidator<PrescriptionEditDto>
    {
        public PrescriptionEditDtoValidator()
        {
            // 处方ID必填
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("处方ID不能为空");

            // 患者ID必填
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("患者ID不能为空");

            // 医生ID必填
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("医生ID不能为空");

            // 总价格范围验证
            RuleFor(x => x.TotalPrice)
                .GreaterThanOrEqualTo(0).WithMessage("总价格必须大于等于0");

            // 折扣范围验证
            RuleFor(x => x.Discount)
                .InclusiveBetween(0m, 1m).WithMessage("折扣必须在0-1之间");

            // 诊断长度限制（可选）
            RuleFor(x => x.Diagnosis)
                .MaximumLength(500).WithMessage("诊断长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Diagnosis));

            // 剂数范围验证
            RuleFor(x => x.DosageCount)
                .InclusiveBetween(1, 100).WithMessage("剂数必须在1-100之间");

            // 用药建议长度限制（可选）
            RuleFor(x => x.Advice)
                .MaximumLength(500).WithMessage("用药建议不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Advice));

            // 备注长度限制（可选）
            RuleFor(x => x.Remark)
                .MaximumLength(500).WithMessage("备注不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));

            // 验证处方项目（可选）
            When(x => x.Items != null && x.Items.Count > 0, () =>
            {
                RuleForEach(x => x.Items).SetValidator(new PrescriptionItemInputDtoValidator());
            });
        }
    }
}
