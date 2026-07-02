using FluentValidation;
using LYBT.Module.Patients.Application.Commands;

namespace LYBT.Module.Patients.Application.Validators;

/// <summary>
/// 更新患者命令验证器。
/// </summary>
public class UpdatePatientValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("患者ID不能为空");

        RuleFor(x => x.Input.Name)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(100).WithMessage("患者姓名长度不能超过100个字符");

        RuleFor(x => x.Input.PhoneNumber)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号码格式不正确")
            .When(x => !string.IsNullOrEmpty(x.Input.PhoneNumber));

        RuleFor(x => x.Input.IdNumber)
            .MaximumLength(18).WithMessage("身份证号长度不能超过18个字符")
            .When(x => !string.IsNullOrEmpty(x.Input.IdNumber));
    }
}


