using FluentValidation;
using LYBT.Module.Identity.Application.Commands;

namespace LYBT.Module.Identity.Application.Validators;

/// <summary>
/// 修改个人资料命令验证器。
/// </summary>
public class ChangeProfileValidator : AbstractValidator<ChangeProfileCommand>
{
    public ChangeProfileValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("用户ID不能为空");

        RuleFor(x => x.Dto.RealName)
            .NotEmpty().WithMessage("真实姓名不能为空")
            .MaximumLength(50).WithMessage("真实姓名长度不能超过50个字符");

        RuleFor(x => x.Dto.PhoneNumber)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号码格式不正确")
            .When(x => !string.IsNullOrEmpty(x.Dto.PhoneNumber));

        RuleFor(x => x.Dto.Email)
            .EmailAddress().WithMessage("邮箱格式不正确")
            .When(x => !string.IsNullOrEmpty(x.Dto.Email));
    }
}
