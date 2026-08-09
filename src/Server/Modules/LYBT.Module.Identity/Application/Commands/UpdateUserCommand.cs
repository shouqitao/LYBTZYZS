using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Identity.Application.Commands;

/// <summary>
/// 更新用户命令。
/// </summary>
public record UpdateUserCommand(
    Guid Id,
    UserInputDto Input,
    Guid CurrentUserId
) : IRequest<Result<UserDetailDto>>;
