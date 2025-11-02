using FluentValidation;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Shared.Validators.Consultation
{
    /// <summary>
    /// 诊疗创建DTO验证器
    /// </summary>
    public class ConsultationInputDtoValidator : AbstractValidator<ConsultationInputDto>
    {
        public ConsultationInputDtoValidator()
        {
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("患者ID不能为空");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("用户ID不能为空");

            RuleFor(x => x.ChiefComplaint)
                .MaximumLength(500).WithMessage("主诉长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.ChiefComplaint));
        }
    }
}
