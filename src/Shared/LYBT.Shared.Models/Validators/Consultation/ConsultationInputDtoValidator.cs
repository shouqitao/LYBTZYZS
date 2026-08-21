using FluentValidation;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Primitives.Validation;

namespace LYBT.Shared.Models.Validators.Consultation;

/// <summary>
/// 诊疗输入DTO验证器 — P0-8: 补 CreateFromInputDtoAsync 手动校验（原直接映射绕过 ValidationBehavior）
/// </summary>
public class ConsultationInputDtoValidator : AbstractValidator<ConsultationInputDto>
{
    public ConsultationInputDtoValidator()
    {
        RuleFor(x => x.PresentIllness)
            .MaximumLength(ValidationConstants.FourDiagnosisMaxLength)
            .WithMessage($"现病史长度不能超过{{MaxLength}}个字符")
            .When(x => !string.IsNullOrEmpty(x.PresentIllness));

        RuleFor(x => x.TongueDiagnosis)
            .MaximumLength(ValidationConstants.DiagnosisMaxLength)
            .WithMessage($"舌诊长度不能超过{{MaxLength}}个字符")
            .When(x => !string.IsNullOrEmpty(x.TongueDiagnosis));

        RuleFor(x => x.PulseDiagnosis)
            .MaximumLength(ValidationConstants.DiagnosisMaxLength)
            .WithMessage($"脉诊长度不能超过{{MaxLength}}个字符")
            .When(x => !string.IsNullOrEmpty(x.PulseDiagnosis));

        RuleFor(x => x.TcmDiagnosis)
            .MaximumLength(ValidationConstants.DiagnosisMaxLength)
            .WithMessage($"中医诊断长度不能超过{{MaxLength}}个字符")
            .When(x => !string.IsNullOrEmpty(x.TcmDiagnosis));
    }
}
