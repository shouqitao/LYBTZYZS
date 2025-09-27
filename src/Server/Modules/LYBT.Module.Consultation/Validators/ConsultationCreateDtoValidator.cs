using FluentValidation;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Module.Consultation.Validators
{
    /// <summary>
    /// 诊疗创建DTO验证器
    /// </summary>
    public class ConsultationCreateDtoValidator : AbstractValidator<ConsultationCreateDto>
    {
        public ConsultationCreateDtoValidator()
        {
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("患者ID不能为空");

            RuleFor(x => x.ChiefComplaint)
                .NotEmpty().WithMessage("主诉不能为空")
                .MaximumLength(500).WithMessage("主诉长度不能超过500个字符");

            RuleFor(x => x.Diagnosis)
                .NotEmpty().WithMessage("诊断不能为空")
                .MaximumLength(1000).WithMessage("诊断长度不能超过1000个字符");
        }
    }
}