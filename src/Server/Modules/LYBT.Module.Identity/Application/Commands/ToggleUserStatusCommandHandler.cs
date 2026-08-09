using MediatR;
using LYBT.Module.Identity.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Identity.Application.Mappers;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Identity.Application.Commands;

public class ToggleUserStatusCommandHandler : IRequestHandler<ToggleUserStatusCommand, Result<UserDetailDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ISecurityAuditService _securityAuditService;

    public ToggleUserStatusCommandHandler(
        IUserRepository userRepository,
        IAuthSessionRepository authSessionRepository,
        ISecurityAuditService securityAuditService)
    {
        _userRepository = userRepository;
        _authSessionRepository = authSessionRepository;
        _securityAuditService = securityAuditService;
    }

    public async Task<Result<UserDetailDto>> Handle(
        ToggleUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            return Result<UserDetailDto>.Failure(ErrorCode.UserNotFound, ErrorMessages.Get(ErrorCode.UserNotFound));

        if (user.IsSysAdmin)
            return Result<UserDetailDto>.Failure(ErrorCode.CannotDeleteSysAdmin, "系统管理员账号不可被禁用");

        if (!request.IsAdmin)
            return Result<UserDetailDto>.Failure(ErrorCode.Unauthorized, "您没有权限切换该用户状态");

        var newStatus = user.Status == CommonStatus.Enabled
            ? CommonStatus.Disabled
            : CommonStatus.Enabled;

        user.ChangeStatus(newStatus, request.CurrentUserId);
        await _userRepository.UpdateAsync(user, cancellationToken);

        // 禁用用户时撤销其全部有效会话（Token 族旋转）
        if (newStatus == CommonStatus.Disabled)
        {
            await _authSessionRepository.RevokeAllUserSessionsAsync(user.Id, "用户已被禁用", cancellationToken);
        }

        await _securityAuditService.RecordEventAsync(new SecurityAuditEvent
        {
            UserId = user.Id,
            UserName = user.UserName,
            EventType = "UserStatusChanged",
            Details = newStatus == CommonStatus.Disabled ? "用户被禁用" : "用户被启用",
            IsSuccess = true
        }, cancellationToken);

        return Result<UserDetailDto>.Success(IdentityMapper.ToDetailDto(user));
    }
}
