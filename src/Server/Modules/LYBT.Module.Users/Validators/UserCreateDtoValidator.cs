using FluentValidation;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Validators
{
    public class UserInputDtoValidator : AbstractValidator<UserInputDto>
    {
        public UserInputDtoValidator()
        {
            RuleFor(x => x.UserName).NotEmpty();
        }
    }
}
