using FluentValidation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Validators.Common;

namespace LYBT.Shared.Validators.MedicalCase
{
    /// <summary>
    /// 病案输入DTO验证器
    /// </summary>
    /// <remarks>
    /// Epic #1961: FluentValidation统一设计重构
    ///
    /// 设计理念：
    /// 1. 统一验证创建和更新场景（通过Id字段区分）
    /// 2. 必填字段：PatientId, DoctorId, VisitDate
    /// 3. 可选字段：仅在有值时验证长度
    /// 4. 使用ValidationConstants统一管理验证规则
    ///
    /// 参考标准：
    /// - UserInputDtoValidator（条件验证模式）
    ///
    /// 相关文档：
    /// - 设计文档：docs/explanation/fluentvalidation-unified-design.md
    /// - 任务文档：docs/tasks/fluentvalidation-unified-tasks.md
    /// - GitHub Epic：Issue #1961
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

            // 主诉：可选，有值时验证长度
            RuleFor(x => x.ChiefComplaint)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"主诉长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.ChiefComplaint));

            // 现病史：可选，有值时验证长度
            RuleFor(x => x.PresentIllnessHistory)
                .MaximumLength(ValidationConstants.LongRemarkMaxLength)
                .WithMessage($"现病史长度不能超过{ValidationConstants.LongRemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.PresentIllnessHistory));

            // 既往史：可选，有值时验证长度
            RuleFor(x => x.PastMedicalHistory)
                .MaximumLength(ValidationConstants.LongRemarkMaxLength)
                .WithMessage($"既往史长度不能超过{ValidationConstants.LongRemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.PastMedicalHistory));

            // 过敏史：可选，有值时验证长度
            RuleFor(x => x.AllergyHistory)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"过敏史长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.AllergyHistory));

            // 望诊：可选，有值时验证长度
            RuleFor(x => x.Inspection)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"望诊长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Inspection));

            // 闻诊：可选，有值时验证长度
            RuleFor(x => x.Auscultation)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"闻诊长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Auscultation));

            // 问诊：可选，有值时验证长度
            RuleFor(x => x.Inquiry)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"问诊长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Inquiry));

            // 切诊：可选，有值时验证长度
            RuleFor(x => x.Palpation)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"切诊长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Palpation));

            // 中医诊断：可选，有值时验证长度
            RuleFor(x => x.TCMDiagnosis)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"中医诊断长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.TCMDiagnosis));

            // 西医诊断：可选，有值时验证长度
            RuleFor(x => x.WesternDiagnosis)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"西医诊断长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.WesternDiagnosis));

            // 治则治法：可选，有值时验证长度
            RuleFor(x => x.TreatmentPrinciple)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"治则治法长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.TreatmentPrinciple));

            // 备注：可选，有值时验证长度
            RuleFor(x => x.Remark)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"备注长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));

            // ========== 创建/更新场景区分 ==========
            // 注意：当前MedicalCase的创建和更新验证规则完全相同，暂不需要条件验证
            // 如果未来需要，可添加：
            // RuleFor(x => x.SomeField)
            //     .NotEmpty()
            //     .When(x => x.Id == null || x.Id == Guid.Empty);
        }
    }
}
