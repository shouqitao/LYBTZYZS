using MediatR;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Entities.Users;
using Microsoft.AspNetCore.Identity;

namespace LYBT.Module.Users.Application.Commands;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthCrossModuleService _authCrossModule;

    public ChangePasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IAuthCrossModuleService authCrossModule)
    {
        _userManager = userManager;
        _authCrossModule = authCrossModule;
    }

    public async Task<Result> Handle(
        ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.Id != request.CurrentUserId)
            return Result.Failure(ErrorCode.Forbidden, "只能修改自己的密码");

        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user == null)
            return Result.Failure(ErrorCode.UserNotFound, ErrorMessages.Get(ErrorCode.UserNotFound));

        var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault();
            var message = error?.Description ?? "密码修改失败";
            return Result.Failure(ErrorCode.InvalidPassword, message);
        }

        await _authCrossModule.RevokeAllUserSessionsAsync(user.Id, "密码已修改", cancellationToken);
        await _authCrossModule.RecordSecurityAuditAsync(new SecurityAuditEvent
        {
            UserId = user.Id,
            UserName = user.UserName,
            EventType = "PasswordChanged",
            Details = "用户修改了自己的密码",
            IsSuccess = true
        }, cancellationToken);

        return Result.Success();
    }
}
