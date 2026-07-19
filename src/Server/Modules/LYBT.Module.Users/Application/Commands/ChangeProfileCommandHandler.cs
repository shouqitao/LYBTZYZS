using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Application.Mappers;

namespace LYBT.Module.Users.Application.Commands;

public class ChangeProfileCommandHandler : IRequestHandler<ChangeProfileCommand, Result<UserDetailDto>>
{
    private readonly IUserRepository _userRepository;

    public ChangeProfileCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDetailDto>> Handle(
        ChangeProfileCommand request, CancellationToken cancellationToken)
    {
        if (request.Id != request.CurrentUserId)
            return Result<UserDetailDto>.Failure(ErrorCode.Forbidden, "只能修改自己的个人资料");

        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            return Result<UserDetailDto>.Failure(ErrorCode.UserNotFound, "用户不存在");

        user.UpdateProfile(
            request.Dto.RealName,
            request.Dto.PhoneNumber,
            request.Dto.Email,
            user.Remark,
            request.CurrentUserId);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<UserDetailDto>.Success(UserMapper.ToDetailDto(user));
    }
}


