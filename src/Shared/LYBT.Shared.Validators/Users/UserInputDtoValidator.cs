using FluentValidation;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.Validation;

namespace LYBT.Shared.Validators.Users
{
    public class UserInputDtoValidator : AbstractValidator<UserInputDto>
    {
        public UserInputDtoValidator()
        {
            // 用户名：创建时必填（Id为null），更新时可选
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("用户名不能为空")
                .When(x => x.Id == null || x.Id == Guid.Empty);

            // 真实姓名：创建时必填
            RuleFor(x => x.RealName)
                .NotEmpty().WithMessage("真实姓名不能为空")
                .MaximumLength(ValidationConstants.NameMaxLength)
                .WithMessage($"真实姓名长度不能超过{ValidationConstants.NameMaxLength}个字符")
                .When(x => x.Id == null || x.Id == Guid.Empty);

            // 角色：创建时必填且有效
            RuleFor(x => x.Role)
                .NotNull().WithMessage("用户角色不能为空")
                .IsInEnum().WithMessage("用户角色无效")
                .When(x => x.Id == null || x.Id == Guid.Empty);

            // 密码：创建时如果提供则验证长度
            RuleFor(x => x.Password)
                .MinimumLength(8).WithMessage("密码长度不能少于8个字符")
                .MaximumLength(ValidationConstants.PasswordMaxLength)
                .WithMessage($"密码长度不能超过{ValidationConstants.PasswordMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Password));

            // 确认密码：如果提供密码则必须匹配
            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("两次输入的密码不一致")
                .When(x => !string.IsNullOrEmpty(x.Password));

            // 邮箱：可选，但填写时必须有效
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage(ValidationConstants.EmailFormatErrorMessage)
                .MaximumLength(100).WithMessage("邮箱长度不能超过100个字符")
                .When(x => !string.IsNullOrEmpty(x.Email));

            // 手机号：可选，但填写时必须有效（中国手机号格式）
            RuleFor(x => x.PhoneNumber)
                .Matches(ValidationConstants.PhoneRegex)
                .WithMessage(ValidationConstants.PhoneFormatErrorMessage)
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            // 备注：可选，限制长度
            RuleFor(x => x.Remark)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage($"备注长度不能超过{ValidationConstants.RemarkMaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Remark));
        }
    }
}
