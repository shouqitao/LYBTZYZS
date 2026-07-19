using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Users.Application.Mappers;
using LYBT.Module.Users.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Users.Application.Commands;

public class RestoreUserCommandHandler : IRequestHandler<RestoreUserCommand, Result<UserDetailDto>>
{
    private readonly IUserRepository _userRepository;

    public RestoreUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDetailDto>> Handle(
        RestoreUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdIncludingDeletedAsync(request.UserId, cancellationToken);
        if (user == null)
            return Result<UserDetailDto>.Failure(ErrorCode.UserNotFound, "用户不存在");

        if (!user.IsDeleted)
            return Result<UserDetailDto>.Failure(ErrorCode.InvalidRequest, "该用户未被删除，无需恢复");

        user.IsDeleted = false;
        user.Status = CommonStatus.Enabled;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        user.UpdatedBy = request.OperatorId;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<UserDetailDto>.Success(UserMapper.ToDetailDto(user));
    }
}
