using FluentValidation;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Shared.Validators.Auth
{
    /// <summary>
    /// 登录请求验证器
    /// </summary>
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("用户名不能为空")
                .MaximumLength(32).WithMessage("用户名长度不能超过32个字符");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("密码不能为空")
                .MinimumLength(6).WithMessage("密码长度不能少于6个字符");
        }
    }
}
