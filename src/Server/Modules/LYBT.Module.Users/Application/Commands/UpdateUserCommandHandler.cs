using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Application.Mappers;

namespace LYBT.Module.Users.Application.Commands;

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
            return Result<UserDetailDto>.Failure(ErrorCode.NotFound, "用户不存在");

        user.UpdateProfile(
            request.Input.RealName!,
            request.Input.PhoneNumber,
            request.Input.Email,
            request.Input.Remark,
            request.CurrentUserId);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<UserDetailDto>.Success(UserMapper.ToDetailDto(user));
    }
}


