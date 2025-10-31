using FluentValidation;
using LYBT.Shared.Models.Contracts.Auth;

namespace LYBT.Module.Auth.Validators
{
    /// <summary>
    /// 修改密码请求验证器
    /// Epic #1731: 补全Auth模块Validators
    /// </summary>
    public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordRequestValidator()
        {
            RuleFor(x => x.OldPassword)
                .NotEmpty().WithMessage("旧密码不能为空");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("新密码不能为空")
                .Length(6, 50).WithMessage("新密码长度必须在6-50个字符之间");

            RuleFor(x => x.NewPassword)
                .NotEqual(x => x.OldPassword).WithMessage("新密码不能与旧密码相同")
                .When(x => !string.IsNullOrEmpty(x.OldPassword) && !string.IsNullOrEmpty(x.NewPassword));
        }
    }
}
