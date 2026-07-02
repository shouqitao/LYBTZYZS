using FluentValidation;
using LYBT.Module.Users.Application.Commands;

namespace LYBT.Module.Users.Application.Validators;

/// <summary>
/// 创建用户命令验证器。
/// </summary>
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Input.UserName)
            .NotEmpty().WithMessage("用户名不能为空")
            .Length(3, 32).WithMessage("用户名长度必须在3-32个字符之间")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("用户名只能包含字母、数字和下划线");

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


