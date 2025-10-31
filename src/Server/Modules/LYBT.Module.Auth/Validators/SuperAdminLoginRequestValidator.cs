using FluentValidation;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Module.Auth.Validators
{
    /// <summary>
    /// 超级管理员登录请求验证器
    /// Epic #1731: 补全Auth模块Validators
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
