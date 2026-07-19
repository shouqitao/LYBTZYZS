using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Application.Mappers;

namespace LYBT.Module.Users.Application.Commands;

public class ToggleUserStatusCommandHandler : IRequestHandler<ToggleUserStatusCommand, Result<UserDetailDto>>
{
    private readonly IUserRepository _userRepository;

    public ToggleUserStatusCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDetailDto>> Handle(
        ToggleUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            return Result<UserDetailDto>.Failure(ErrorCode.UserNotFound, "用户不存在");

        if (user.IsSysAdmin)
            return Result<UserDetailDto>.Failure(ErrorCode.CannotDeleteSysAdmin, "系统管理员账号不可被禁用");

        if (!request.IsAdmin)
            return Result<UserDetailDto>.Failure(ErrorCode.Unauthorized, "您没有权限切换该用户状态");

        var newStatus = user.Status == Shared.Models.Enums.CommonStatus.Enabled
            ? Shared.Models.Enums.CommonStatus.Disabled
            : Shared.Models.Enums.CommonStatus.Enabled;

        user.ChangeStatus(newStatus, request.CurrentUserId);
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<UserDetailDto>.Success(UserMapper.ToDetailDto(user));
    }
}


