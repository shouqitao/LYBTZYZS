using FluentValidation;
using LYBT.Shared.Models.Constants;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Module.Consultation.Validators
{
    /// <summary>
    /// 诊疗更新DTO验证器
    /// </summary>
    public class ConsultationUpdateDtoValidator : AbstractValidator<ConsultationUpdateDto>
    {
        public ConsultationUpdateDtoValidator()
        {
            // 诊疗ID必填
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("诊疗ID不能为空");

            // 主诉长度限制（可选）
            RuleFor(x => x.ChiefComplaint)
                .MaximumLength(ValidationConstants.DiagnosisMaxLength)
                .WithMessage($"主诉长度不能超过{ValidationConstants.DiagnosisMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.ChiefComplaint));

            // 现病史长度限制（可选）
            RuleFor(x => x.PresentIllness)
                .MaximumLength(ValidationConstants.LongRemarkMaxLength)
                .WithMessage($"现病史长度不能超过{ValidationConstants.LongRemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.PresentIllness));

            // 望诊结果长度限制（可选）
            RuleFor(x => x.Inspection)
                .MaximumLength(ValidationConstants.DiagnosisMaxLength)
                .WithMessage($"望诊结果长度不能超过{ValidationConstants.DiagnosisMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Inspection));

            // 闻诊结果长度限制（可选）
            RuleFor(x => x.AuscultationOlfaction)
                .MaximumLength(ValidationConstants.DiagnosisMaxLength)
                .WithMessage($"闻诊结果长度不能超过{ValidationConstants.DiagnosisMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.AuscultationOlfaction));

            // 问诊结果长度限制（可选）
            RuleFor(x => x.Inquiry)
                .MaximumLength(ValidationConstants.DiagnosisMaxLength)
                .WithMessage($"问诊结果长度不能超过{ValidationConstants.DiagnosisMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Inquiry));

            // 切诊结果长度限制（可选）
            RuleFor(x => x.Palpation)
                .MaximumLength(ValidationConstants.DiagnosisMaxLength)
                .WithMessage($"切诊结果长度不能超过{ValidationConstants.DiagnosisMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Palpation));

            // 中医诊断长度限制（可选）
            RuleFor(x => x.TCMDiagnosis)
                .MaximumLength(ValidationConstants.DiagnosisMaxLength)
                .WithMessage($"中医诊断长度不能超过{ValidationConstants.DiagnosisMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.TCMDiagnosis));

            // 诊断结果长度限制（可选）
            RuleFor(x => x.Diagnosis)
                .MaximumLength(ValidationConstants.DiagnosisMaxLength)
                .WithMessage($"诊断结果长度不能超过{ValidationConstants.DiagnosisMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Diagnosis));

            // 治疗原则长度限制（可选）
            RuleFor(x => x.TreatmentPrinciple)
                .MaximumLength(ValidationConstants.DiagnosisMaxLength)
                .WithMessage($"治疗原则长度不能超过{ValidationConstants.DiagnosisMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.TreatmentPrinciple));

            // 医嘱长度限制（可选）
            RuleFor(x => x.MedicalAdvice)
                .MaximumLength(ValidationConstants.LongRemarkMaxLength)
                .WithMessage($"医嘱长度不能超过{ValidationConstants.LongRemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.MedicalAdvice));

            // 备注长度限制（可选）
            RuleFor(x => x.Remark)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"备注长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));
        }
    }
}
