using MediatR;
using LYBT.Module.Identity.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Identity.Application.Commands;

/// <summary>
/// 删除用户命令处理器（软删除）。
/// </summary>
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ISecurityAuditService _securityAuditService;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        IAuthSessionRepository authSessionRepository,
        ISecurityAuditService securityAuditService)
    {
        _userRepository = userRepository;
        _authSessionRepository = authSessionRepository;
        _securityAuditService = securityAuditService;
    }

    public async Task<Result> Handle(
        DeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (!request.IsAdmin)
            return Result.Failure(ErrorCode.Unauthorized, "无权删除用户");

        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            return Result.Failure(ErrorCode.NotFound, ErrorMessages.Get(ErrorCode.UserNotFound));

        if (user.IsSysAdmin)
            return Result.Failure(ErrorCode.CannotDeleteSysAdmin, "系统管理员账号不可被删除");

        user.SoftDelete(request.CurrentUserId);

        await _userRepository.UpdateAsync(user, cancellationToken);

        await _authSessionRepository.RevokeAllUserSessionsAsync(user.Id, "用户已删除", cancellationToken);
        await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
        {
            UserId = user.Id,
            UserName = user.UserName,
            EventType = "UserDeleted",
            Details = $"用户 {user.RealName} 已被删除，操作人ID={request.CurrentUserId}",
            IsSuccess = true
        }, cancellationToken);

        return Result.Success();
    }
}
