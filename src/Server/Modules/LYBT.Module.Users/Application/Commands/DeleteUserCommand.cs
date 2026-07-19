using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Users.Application.Commands;

/// <summary>
/// 删除用户命令（软删除）。
/// </summary>
public record DeleteUserCommand(
    Guid Id,
    Guid CurrentUserId,
    bool IsAdmin
) : IRequest<Result>;


