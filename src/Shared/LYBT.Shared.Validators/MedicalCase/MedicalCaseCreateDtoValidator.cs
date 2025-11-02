using FluentValidation;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Shared.Validators.MedicalCase
{
    /// <summary>
    /// 病案创建DTO验证器
    /// </summary>
    public class MedicalCaseCreateDtoValidator : AbstractValidator<MedicalCaseCreateDto>
    {
        public MedicalCaseCreateDtoValidator()
        {
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("患者ID不能为空");

            RuleFor(x => x.DoctorId)
                .NotEmpty().WithMessage("医生ID不能为空");

            RuleFor(x => x.ChiefComplaint)
                .MaximumLength(1000).WithMessage("主诉长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.ChiefComplaint));

            RuleFor(x => x.PresentIllnessHistory)
                .MaximumLength(2000).WithMessage("现病史长度不能超过2000个字符")
                .When(x => !string.IsNullOrEmpty(x.PresentIllnessHistory));
        }
    }
}
