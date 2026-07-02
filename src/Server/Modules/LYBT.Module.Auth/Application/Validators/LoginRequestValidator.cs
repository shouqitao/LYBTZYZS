using FluentValidation;
using LYBT.Module.Auth.Application.Commands;

namespace LYBT.Module.Auth.Application.Validators;

/// <summary>
/// 登录命令验证器。
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginCommand>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Input.UserName)
            .NotEmpty().WithMessage("用户名不能为空")
            .MaximumLength(32).WithMessage("用户名长度不能超过32个字符");

        RuleFor(x => x.Input.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .MinimumLength(6).WithMessage("密码长度不能少于6个字符");
    }
}


