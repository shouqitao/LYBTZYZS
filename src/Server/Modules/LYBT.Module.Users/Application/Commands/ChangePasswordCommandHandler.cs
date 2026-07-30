using MediatR;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Entities.Users;
using Microsoft.AspNetCore.Identity;

namespace LYBT.Module.Users.Application.Commands;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ChangePasswordCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result> Handle(
        ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.Id != request.CurrentUserId)
            return Result.Failure(ErrorCode.Forbidden, "只能修改自己的密码");

        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user == null)
            return Result.Failure(ErrorCode.UserNotFound, "用户不存在");

        var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault();
            var message = error?.Description ?? "密码修改失败";
            return Result.Failure(ErrorCode.InvalidPassword, message);
        }

        return Result.Success();
    }
}
