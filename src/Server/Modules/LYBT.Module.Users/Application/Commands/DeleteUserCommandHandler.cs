using MediatR;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using LYBT.SharedKernel.Events;
using LYBT.Entities.Users;
using LYBT.Module.Users.Domain.Events;
using LYBT.Module.Users.Interfaces;

namespace LYBT.Module.Users.Application.Commands;

/// <summary>
/// 删除用户命令处理器（软删除）。
/// </summary>
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _userRepository = userRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Result> Handle(
        DeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (!request.IsAdmin)
            return Result.Failure(ErrorCode.Unauthorized, "无权删除用户");

        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            return Result.Failure(ErrorCode.NotFound, "用户不存在");

        if (user.IsSysAdmin)
            return Result.Failure(ErrorCode.CannotDeleteSysAdmin, "系统管理员账号不可被删除");

        user.SoftDelete(request.CurrentUserId);

        await _userRepository.UpdateAsync(user, cancellationToken);

        await _eventDispatcher.DispatchAsync(new[]
        {
            new UserDeletedEvent(user.Id, user.UserName, user.RealName, request.CurrentUserId)
        }, cancellationToken);

        return Result.Success();
    }
}


