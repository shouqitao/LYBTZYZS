using FluentValidation;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Shared.Models.Validators.Users;

/// <summary>
/// 用户输入DTO验证器（DTO 级——审计补全：原整体缺失，仅命令级 CreateUserValidator 存在）。
/// 规则与 UserInputDto DataAnnotations 及 CreateUserValidator（命令级）对齐：
/// - 创建场景（Id==null）：UserName/RealName 必填
/// - 更新场景（Id!=null）：UserName 不可改（不校验必填）；RealName 有值则校验长度
/// - 可选字段（Password/PhoneNumber/Email/PinYinCode/Role/Remark）：有值时校验
/// 保留用户名业务检查在命令级 CreateUserValidator（UserReservedNameHelper）——DTO 级不做业务规则。
/// </summary>
public class UserInputDtoValidator : AbstractValidator<UserInputDto>
{
    public UserInputDtoValidator()
    {
        // ========== 用户名（创建时必填，更新时不可改） ==========

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("用户名不能为空")
            .Length(3, 32).WithMessage("用户名长度必须在3-32个字符之间")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("用户名只能包含字母、数字和下划线")
            .When(x => x.Id == null); // 创建场景

        RuleFor(x => x.UserName)
            .Length(3, 32).WithMessage("用户名长度必须在3-32个字符之间")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("用户名只能包含字母、数字和下划线")
            .When(x => x.Id != null && !string.IsNullOrEmpty(x.UserName)); // 更新场景有值则校验格式

        // ========== 密码（创建时可选，更新时禁止） ==========

        RuleFor(x => x.Password)
            .Length(6, 128).WithMessage("密码长度必须在6-128个字符之间")
            .When(x => !string.IsNullOrEmpty(x.Password));

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("两次输入的密码不一致")
            .When(x => !string.IsNullOrEmpty(x.Password));

        // ========== 真实姓名（创建时必填，更新时可选） ==========

        RuleFor(x => x.RealName)
            .NotEmpty().WithMessage("真实姓名不能为空")
            .MaximumLength(50).WithMessage("真实姓名长度不能超过50个字符")
            .When(x => x.Id == null); // 创建场景

        RuleFor(x => x.RealName)
            .MaximumLength(50).WithMessage("真实姓名长度不能超过50个字符")
            .When(x => x.Id != null && !string.IsNullOrEmpty(x.RealName)); // 更新场景有值则校验长度

        // ========== 可选字段（有值时校验） ==========

        RuleFor(x => x.PinYinCode)
            .MaximumLength(50).WithMessage("拼音码长度不能超过50个字符")
            .When(x => !string.IsNullOrEmpty(x.PinYinCode));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号码格式不正确")
            .MaximumLength(20).WithMessage("手机号码长度不能超过20个字符")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("邮箱格式不正确")
            .MaximumLength(100).WithMessage("邮箱长度不能超过100个字符")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("用户角色值无效")
            .When(x => x.Role.HasValue);

        RuleFor(x => x.Remark)
            .MaximumLength(500).WithMessage("备注长度不能超过500个字符")
            .When(x => !string.IsNullOrEmpty(x.Remark));
    }
}
