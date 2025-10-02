using FluentValidation;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Module.Consultation.Validators
{
    /// <summary>
    /// 诊疗创建DTO验证器 - 简化版，只保留必要验证
    /// </summary>
    public class ConsultationCreateDtoValidator : AbstractValidator<ConsultationCreateDto>
    {
        public ConsultationCreateDtoValidator()
        {
            // 只验证患者ID必填，其他字段允许为空（四诊信息可以逐步完善）
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("患者ID不能为空");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("医生ID不能为空");

            // 字符长度限制保留，但不强制必填
            RuleFor(x => x.ChiefComplaint)
                .MaximumLength(500).WithMessage("主诉长度不能超过500个字符")
                .When(x => !string.IsNullOrEmpty(x.ChiefComplaint));

            RuleFor(x => x.Diagnosis)
                .MaximumLength(1000).WithMessage("诊断长度不能超过1000个字符")
                .When(x => !string.IsNullOrEmpty(x.Diagnosis));
        }
    }
}
