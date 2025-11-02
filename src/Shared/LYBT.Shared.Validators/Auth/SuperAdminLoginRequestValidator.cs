using FluentValidation;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Shared.Validators.Auth
{
    /// <summary>
    /// 超级管理员登录请求验证器
    /// </summary>
    public class SuperAdminLoginRequestValidator : AbstractValidator<SuperAdminLoginRequest>
    {
        public SuperAdminLoginRequestValidator()
        {
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("密码不能为空");
        }
    }
}
