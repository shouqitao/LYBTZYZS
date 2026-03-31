using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Interfaces
{
    public interface IUserPasswordService
    {
        Task<Result<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequestDto request, CancellationToken cancellationToken = default);

        Task<Result<UserDetailDto>> ValidatePasswordAsync(string userName, string password, CancellationToken cancellationToken = default);

        Task<Result> ChangePasswordAsync(Guid id, string oldPassword, string newPassword, CancellationToken cancellationToken = default);
    }
}
