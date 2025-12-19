using FluentValidation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Validators.Common;

namespace LYBT.Shared.Validators.MedicalCase
{
    /// <summary>
    /// 病案输入DTO验证器
    /// </summary>
    /// <remarks>
    /// OpenSpec: unify-medicalcase-input-dto
    ///
    /// 设计理念：
    /// 1. 仅验证创建/更新医案的核心字段
    /// 2. 必填字段：PatientId, DoctorId, VisitDate
    /// 3. 可选字段：Remark（仅在有值时验证长度）
    /// 4. 诊断字段验证在ConsultationInputDtoValidator中
    ///
    /// 相关OpenSpec:
    /// - unify-medicalcase-input-dto: 简化InputDto及其验证器
    /// </remarks>
    public class MedicalCaseInputDtoValidator : AbstractValidator<MedicalCaseInputDto>
    {
        public MedicalCaseInputDtoValidator()
        {
            // ========== 必填字段验证 ==========

            // 患者ID：始终必填
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("患者ID不能为空");

            // 医生ID：始终必填
            RuleFor(x => x.DoctorId)
                .NotEmpty().WithMessage("医生ID不能为空");

            // 就诊日期：始终必填
            RuleFor(x => x.VisitDate)
                .NotEmpty().WithMessage("就诊日期不能为空")
                .Must(x => x.Date <= DateTime.Today).WithMessage("就诊日期不能晚于今天");

            // ========== 可选字段验证（有值时验证长度） ==========

            // 备注：可选，有值时验证长度
            RuleFor(x => x.Remark)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"备注长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));
        }
    }
}
