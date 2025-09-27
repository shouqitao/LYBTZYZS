using FluentValidation;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Validators
{
    public class UserUpdateDtoValidator : AbstractValidator<UserUpdateDto>
    {
        public UserUpdateDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
