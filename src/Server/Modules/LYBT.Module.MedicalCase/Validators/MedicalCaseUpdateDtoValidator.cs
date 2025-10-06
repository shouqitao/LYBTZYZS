using FluentValidation;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Module.MedicalCase.Validators
{
    /// <summary>
    /// 医疗案例更新DTO验证器
    /// </summary>
    public class MedicalCaseUpdateDtoValidator : AbstractValidator<MedicalCaseUpdateDto>
    {
        public MedicalCaseUpdateDtoValidator()
        {
            // 医疗案例ID必填
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("医疗案例ID不能为空");

            // 患者ID必填
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("患者ID不能为空");

            // 医生ID必填
            RuleFor(x => x.DoctorId)
                .NotEmpty().WithMessage("医生ID不能为空");

            // 诊断摘要长度限制（可选）
            RuleFor(x => x.DiagnosisSummary)
                .MaximumLength(200).WithMessage("诊断摘要长度不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.DiagnosisSummary));

            // 主诉长度限制（可选）
            RuleFor(x => x.ChiefComplaint)
                .MaximumLength(1000).WithMessage("主诉长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.ChiefComplaint));

            // 现病史长度限制（可选）
            RuleFor(x => x.PresentIllness)
                .MaximumLength(2000).WithMessage("现病史长度不能超过2000个字符")
                .When(x => !string.IsNullOrEmpty(x.PresentIllness));

            // 既往史长度限制（可选）
            RuleFor(x => x.PastHistory)
                .MaximumLength(1000).WithMessage("既往史长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.PastHistory));

            // 诊断结果长度限制（可选）
            RuleFor(x => x.DiagnosisResult)
                .MaximumLength(1000).WithMessage("诊断结果长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.DiagnosisResult));

            // 治疗方案长度限制（可选）
            RuleFor(x => x.TreatmentPlan)
                .MaximumLength(1000).WithMessage("治疗方案长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.TreatmentPlan));

            // 体格检查长度限制（可选）
            RuleFor(x => x.PhysicalExamination)
                .MaximumLength(1000).WithMessage("体格检查长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.PhysicalExamination));

            // 辅助检查长度限制（可选）
            RuleFor(x => x.AuxiliaryExamination)
                .MaximumLength(1000).WithMessage("辅助检查长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.AuxiliaryExamination));

            // 处方信息长度限制（可选）
            RuleFor(x => x.PrescriptionInfo)
                .MaximumLength(1000).WithMessage("处方信息长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.PrescriptionInfo));

            // 随访计划长度限制（可选）
            RuleFor(x => x.FollowUpPlan)
                .MaximumLength(1000).WithMessage("随访计划长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.FollowUpPlan));

            // 备注长度限制（可选）
            RuleFor(x => x.Remark)
                .MaximumLength(500).WithMessage("备注长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));
        }
    }
}
