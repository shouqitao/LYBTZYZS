using FluentValidation;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Shared.Validators.MedicalCase
{
    /// <summary>
    /// 病案更新DTO验证器
    /// </summary>
    public class MedicalCaseUpdateDtoValidator : AbstractValidator<MedicalCaseUpdateDto>
    {
        public MedicalCaseUpdateDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("病案ID不能为空");

            RuleFor(x => x.ChiefComplaint)
                .MaximumLength(1000).WithMessage("主诉长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.ChiefComplaint));

            RuleFor(x => x.PresentIllness)
                .MaximumLength(2000).WithMessage("现病史长度不能超过2000个字符")
                .When(x => !string.IsNullOrEmpty(x.PresentIllness));

            RuleFor(x => x.PastHistory)
                .MaximumLength(2000).WithMessage("既往史长度不能超过2000个字符")
                .When(x => !string.IsNullOrEmpty(x.PastHistory));

            RuleFor(x => x.DiagnosisSummary)
                .MaximumLength(1000).WithMessage("诊断摘要长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.DiagnosisSummary));

            RuleFor(x => x.DiagnosisResult)
                .MaximumLength(1000).WithMessage("诊断结果长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.DiagnosisResult));

            RuleFor(x => x.TreatmentPlan)
                .MaximumLength(2000).WithMessage("治疗方案长度不能超过2000个字符")
                .When(x => !string.IsNullOrEmpty(x.TreatmentPlan));

            RuleFor(x => x.PhysicalExamination)
                .MaximumLength(2000).WithMessage("体格检查长度不能超过2000个字符")
                .When(x => !string.IsNullOrEmpty(x.PhysicalExamination));

            RuleFor(x => x.AuxiliaryExamination)
                .MaximumLength(2000).WithMessage("辅助检查长度不能超过2000个字符")
                .When(x => !string.IsNullOrEmpty(x.AuxiliaryExamination));

            RuleFor(x => x.PrescriptionInfo)
                .MaximumLength(1000).WithMessage("处方信息长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.PrescriptionInfo));

            RuleFor(x => x.FollowUpPlan)
                .MaximumLength(1000).WithMessage("随访计划长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.FollowUpPlan));
        }
    }
}
