using FluentValidation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Validators.Common;
using LYBT.Shared.Validators.Consultation;
using LYBT.Shared.Validators.Prescriptions;

namespace LYBT.Shared.Validators.MedicalCase
{
    /// <summary>
    /// 医案聚合根输入DTO验证器
    /// OpenSpec: refactor-medicalcase-aggregate-crud (PERSIST-001, PERSIST-002)
    /// </summary>
    /// <remarks>
    /// 验证规则：
    /// 1. Id：必填（聚合根标识）
    /// 2. Consultation：可选，有值时使用嵌套验证器
    /// 3. Prescription：可选，有值时使用嵌套验证器
    /// 4. 当NeedsPrescription=true时，处方项目不能为空
    /// </remarks>
    public class MedicalCaseAggregateInputDtoValidator : AbstractValidator<MedicalCaseAggregateInputDto>
    {
        public MedicalCaseAggregateInputDtoValidator()
        {
            // ========== 必填字段验证 ==========

            // 医案ID：必填
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("医案ID不能为空");

            // ========== 可选字段验证 ==========

            // 备注：可选，有值时验证长度
            RuleFor(x => x.Remark)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"备注长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));

            // 编辑原因：可选，有值时验证长度
            RuleFor(x => x.EditReason)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"编辑原因长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.EditReason));

            // ========== 嵌套对象验证 ==========

            // 诊断信息：有值时使用嵌套验证器
            RuleFor(x => x.Consultation)
                .SetValidator(new ConsultationInputDtoValidator()!)
                .When(x => x.Consultation != null);

            // 处方信息：有值时使用嵌套验证器
            RuleFor(x => x.Prescription)
                .SetValidator(new PrescriptionAggregateInputDtoValidator()!)
                .When(x => x.Prescription != null);
        }
    }
}
