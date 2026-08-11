using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Identity.Interfaces;
using LYBT.Module.Identity.Application.Mappers;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Identity.Application.Commands;

/// <summary>
/// 更新用户命令处理器。
/// </summary>
public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<UserDetailDto>>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDetailDto>> Handle(
        UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            return Result<UserDetailDto>.Failure(ErrorCode.NotFound, ErrorMessages.Get(ErrorCode.UserNotFound));

        if (string.IsNullOrWhiteSpace(request.Input.RealName))
            return Result<UserDetailDto>.Failure(ErrorCode.InvalidRequest, "真实姓名不能为空");

        user.UpdateProfile(
            request.Input.RealName!,
            request.Input.PhoneNumber,
            request.Input.Email,
            request.Input.Remark,
            request.CurrentUserId,
            request.Input.RegistrationFee);

        // P2 (US-USER-005): 角色更新（原 Role 字段被忽略——需求「角色变更受层级约束」）
        if (request.Input.Role.HasValue && request.Input.Role.Value != user.Role)
        {
            if (user.IsSysAdmin)
                return Result<UserDetailDto>.Failure(ErrorCode.InvalidRequest, "系统管理员角色不可变更");

            var operatorUser = await _userRepository.GetByIdAsync(request.CurrentUserId, cancellationToken);
            var isSuperAdmin = operatorUser?.IsSysAdmin == true;
            var newRole = request.Input.Role.Value;
            if (!isSuperAdmin && (newRole == UserRole.Admin || newRole == UserRole.SuperAdmin))
                return Result<UserDetailDto>.Failure(ErrorCode.Unauthorized, "无权将用户提升为管理员角色");

            user.Role = newRole;
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        return Result<UserDetailDto>.Success(IdentityMapper.ToDetailDto(user));
    }
}
