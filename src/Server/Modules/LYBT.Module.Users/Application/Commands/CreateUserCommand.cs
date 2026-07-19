using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Users.Application.Commands;

/// <summary>
/// 创建用户命令。
/// </summary>
public record CreateUserCommand(
    UserInputDto Input,
    Guid CurrentUserId,
    bool IsAdmin
) : IRequest<Result<UserDetailDto>>;


