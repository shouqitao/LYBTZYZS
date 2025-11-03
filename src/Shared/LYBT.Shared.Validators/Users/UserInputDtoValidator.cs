using FluentValidation;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Shared.Validators.Users
{
    public class UserInputDtoValidator : AbstractValidator<UserInputDto>
    {
        public UserInputDtoValidator()
        {
            RuleFor(x => x.UserName).NotEmpty();
        }
    }
}
