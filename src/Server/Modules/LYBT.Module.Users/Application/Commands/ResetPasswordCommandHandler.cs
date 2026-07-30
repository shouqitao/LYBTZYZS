using MediatR;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Utilities.Security;
using Microsoft.AspNetCore.Identity;

namespace LYBT.Module.Users.Application.Commands;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<ResetPasswordResult>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<ResetPasswordResult>> Handle(
        ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user == null)
            return Result<ResetPasswordResult>.Failure(ErrorCode.UserNotFound, "用户不存在");

        var newPassword = PasswordHelper.GenerateSecurePassword();
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault();
            var message = error?.Description ?? "密码重置失败";
            return Result<ResetPasswordResult>.Failure(ErrorCode.InvalidPassword, message);
        }

        return Result<ResetPasswordResult>.Success(new ResetPasswordResult(newPassword));
    }
}
