using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Identity.Interfaces;
using LYBT.Module.Identity.Application.Mappers;

namespace LYBT.Module.Identity.Application.Commands;

/// <summary>
/// 修改个人资料命令处理器。
/// </summary>
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
            return Result<UserDetailDto>.Failure(ErrorCode.UserNotFound, ErrorMessages.Get(ErrorCode.UserNotFound));

        if (string.IsNullOrWhiteSpace(request.Dto.RealName))
            return Result<UserDetailDto>.Failure(ErrorCode.InvalidRequest, "真实姓名不能为空");

        user.UpdateProfile(
            request.Dto.RealName,
            request.Dto.PhoneNumber,
            request.Dto.Email,
            user.Remark,
            request.CurrentUserId);

        await _userRepository.UpdateAsync(user, cancellationToken);
        return Result<UserDetailDto>.Success(IdentityMapper.ToDetailDto(user));
    }
}
