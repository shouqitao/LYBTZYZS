using MediatR;
using LYBT.Module.Identity.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Entities.Users;
using Microsoft.AspNetCore.Identity;

namespace LYBT.Module.Identity.Application.Commands;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ISecurityAuditService _securityAuditService;

    public ChangePasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IAuthSessionRepository authSessionRepository,
        ISecurityAuditService securityAuditService)
    {
        _userManager = userManager;
        _authSessionRepository = authSessionRepository;
        _securityAuditService = securityAuditService;
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

        await _authSessionRepository.RevokeAllUserSessionsAsync(user.Id, "密码已修改", cancellationToken);
        await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
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
