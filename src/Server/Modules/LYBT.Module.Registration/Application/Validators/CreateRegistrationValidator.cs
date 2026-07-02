using FluentValidation;
using LYBT.Module.Registration.Application.Commands;

namespace LYBT.Module.Registration.Application.Validators;

/// <summary>
/// 创建挂号请求验证器。
/// </summary>
public sealed class CreateRegistrationValidator : AbstractValidator<CreateRegistrationCommand>
{
    public CreateRegistrationValidator()
    {
        RuleFor(x => x.Input.PatientId)
            .NotEmpty().WithMessage("患者不能为空");

        RuleFor(x => x.Input.PatientName)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(100).WithMessage("患者姓名长度不能超过100个字符");

        RuleFor(x => x.Input.DoctorId)
            .NotEmpty().WithMessage("医生不能为空");

        RuleFor(x => x.Input.DoctorName)
            .NotEmpty().WithMessage("医生姓名不能为空")
            .MaximumLength(100).WithMessage("医生姓名长度不能超过100个字符");

        RuleFor(x => x.Input.RegistrationFee)
            .GreaterThanOrEqualTo(0).WithMessage("挂号费不能为负数");

        RuleFor(x => x.Input.Remark)
            .MaximumLength(500).WithMessage("备注长度不能超过500个字符");
    }
}


