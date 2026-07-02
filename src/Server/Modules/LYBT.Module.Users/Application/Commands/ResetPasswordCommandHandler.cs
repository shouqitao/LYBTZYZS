using MediatR;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using LYBT.Module.Users.Interfaces;

namespace LYBT.Module.Users.Application.Commands;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<ResetPasswordResult>>
{
    private readonly IUserRepository _userRepository;

    public ResetPasswordCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<ResetPasswordResult>> Handle(
        ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            return Result<ResetPasswordResult>.Failure(ErrorCode.UserNotFound, "用户不存在");

        var newPassword = LYBT.Shared.Utilities.Security.PasswordHelper.GenerateSecurePassword();
        var hashedPassword = LYBT.Shared.Utilities.Security.PasswordHelper.HashPassword(newPassword);

        user.PasswordHash = hashedPassword;
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<ResetPasswordResult>.Success(new ResetPasswordResult(newPassword));
    }
}


