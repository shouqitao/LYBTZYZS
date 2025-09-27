using FluentValidation;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Validators
{
    public class UserCreateDtoValidator : AbstractValidator<UserCreateDto>
    {
        public UserCreateDtoValidator()
        {
            RuleFor(x => x.Username).NotEmpty();
        }
    }
}
