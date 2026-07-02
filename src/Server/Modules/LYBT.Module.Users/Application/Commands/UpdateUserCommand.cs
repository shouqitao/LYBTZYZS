using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Users.Application.Commands;

/// <summary>
/// 更新用户命令。
/// </summary>
public record UpdateUserCommand(
    Guid Id,
    UserInputDto Input,
    Guid CurrentUserId,
    bool IsAdmin
) : IRequest<Result<UserDetailDto>>;


