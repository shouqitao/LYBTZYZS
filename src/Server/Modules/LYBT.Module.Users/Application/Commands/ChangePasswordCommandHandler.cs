using MediatR;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Users.Interfaces;

namespace LYBT.Module.Users.Application.Commands;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;

    public ChangePasswordCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result> Handle(
        ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.Id != request.CurrentUserId)
            return Result.Failure(ErrorCode.Forbidden, "只能修改自己的密码");

        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            return Result.Failure(ErrorCode.UserNotFound, "用户不存在");

        var verifyResult = LYBT.Shared.Utilities.Security.PasswordHelper.VerifyPassword(
            request.OldPassword, user.PasswordHash ?? string.Empty);
        if (!verifyResult.IsSuccess)
            return Result.Failure(ErrorCode.InvalidPassword, "原密码错误");

        var newHash = LYBT.Shared.Utilities.Security.PasswordHelper.HashPassword(request.NewPassword);
        user.PasswordHash = newHash;
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success();
    }
}


