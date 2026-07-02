using FluentValidation;
using LYBT.Module.Users.Application.Commands;

namespace LYBT.Module.Users.Application.Validators;

/// <summary>
/// 更新用户命令验证器。
/// </summary>
public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("用户ID不能为空");

        RuleFor(x => x.Input.RealName)
            .NotEmpty().WithMessage("真实姓名不能为空")
            .MaximumLength(50).WithMessage("真实姓名长度不能超过50个字符");

        RuleFor(x => x.Input.PhoneNumber)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号码格式不正确")
            .When(x => !string.IsNullOrEmpty(x.Input.PhoneNumber));

        RuleFor(x => x.Input.Email)
            .EmailAddress().WithMessage("邮箱格式不正确")
            .When(x => !string.IsNullOrEmpty(x.Input.Email));
    }
}


