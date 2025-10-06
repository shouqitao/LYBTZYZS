using FluentValidation;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Module.MedicalCase.Validators
{
    /// <summary>
    /// 医疗案例创建DTO验证器
    /// </summary>
    public class MedicalCaseCreateDtoValidator : AbstractValidator<MedicalCaseCreateDto>
    {
        public MedicalCaseCreateDtoValidator()
        {
            // 患者ID必填
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("患者ID不能为空");

            // 医生ID必填
            RuleFor(x => x.DoctorId)
                .NotEmpty().WithMessage("医生ID不能为空");

            // 案例编号长度限制（可选）
            RuleFor(x => x.CaseNumber)
                .MaximumLength(50).WithMessage("案例编号长度不能超过50个字符")
                .When(x => !string.IsNullOrEmpty(x.CaseNumber));

            // 主诉长度限制（可选）
            RuleFor(x => x.ChiefComplaint)
                .MaximumLength(1000).WithMessage("主诉长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.ChiefComplaint));

            // 现病史长度限制（可选）
            RuleFor(x => x.PresentIllnessHistory)
                .MaximumLength(2000).WithMessage("现病史长度不能超过2000个字符")
                .When(x => !string.IsNullOrEmpty(x.PresentIllnessHistory));

            // 既往史长度限制（可选）
            RuleFor(x => x.PastMedicalHistory)
                .MaximumLength(1000).WithMessage("既往史长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.PastMedicalHistory));

            // 诊断摘要长度限制（可选）
            RuleFor(x => x.DiagnosisSummary)
                .MaximumLength(200).WithMessage("诊断摘要长度不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.DiagnosisSummary));

            // 备注长度限制（可选）
            RuleFor(x => x.Remark)
                .MaximumLength(500).WithMessage("备注长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));
        }
    }
}
