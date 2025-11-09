using FluentValidation;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Shared.Validators.Users
{
    public class UserInputDtoValidator : AbstractValidator<UserInputDto>
    {
        public UserInputDtoValidator()
        {
            // 用户名：创建时必填（Id为null），更新时可选
            RuleFor(x => x.UserName)
                .NotEmpty()
                .When(x => x.Id == null || x.Id == Guid.Empty);
        }
    }
}
