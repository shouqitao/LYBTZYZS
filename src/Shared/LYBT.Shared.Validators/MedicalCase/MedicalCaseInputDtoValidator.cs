using FluentValidation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Primitives.Validation;

namespace LYBT.Shared.Validators.MedicalCase
{
    /// <summary>
    /// 病案输入DTO验证器
    /// </summary>
    /// <remarks>
    /// OpenSpec: unify-medicalcase-input-dto, simplify-medicalcase-dataflow
    ///
    /// 设计理念：
    /// 1. 仅验证创建/更新医案的核心字段
    /// 2. 必填字段：PatientId, UserId（原DoctorId已重命名）
    /// 3. 可选字段：Remark（仅在有值时验证长度）
    /// 4. 诊断字段验证在ConsultationInputDtoValidator中
    /// 5. VisitDate已删除，使用CreatedAt代替（系统自动生成）
    ///
    /// 相关OpenSpec:
    /// - unify-medicalcase-input-dto: 简化InputDto及其验证器
    /// - simplify-medicalcase-dataflow: DoctorId→UserId, VisitDate删除
    /// </remarks>
    public class MedicalCaseInputDtoValidator : AbstractValidator<MedicalCaseInputDto>
    {
        public MedicalCaseInputDtoValidator()
        {
            // ========== 必填字段验证 ==========

            // 患者ID：始终必填
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("患者ID不能为空");

            // 用户ID（原医生ID）：始终必填
            // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("用户ID不能为空");

            // VisitDate已删除，使用CreatedAt代替（系统自动生成）
            // OpenSpec: simplify-medicalcase-dataflow - VisitDate删除

            // ========== 可选字段验证（有值时验证长度） ==========

            // 备注：可选，有值时验证长度
            RuleFor(x => x.Remark)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"备注长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));
        }
    }
}
