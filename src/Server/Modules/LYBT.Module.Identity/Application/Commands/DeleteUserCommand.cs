using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using MediatR;

namespace LYBT.Module.Identity.Application.Commands;

/// <summary>
/// 删除用户命令（软删除；UPDATEUSER-HIERARCHY-FIX: IsAdmin bool → OperatorRole 细粒度层级）。
/// </summary>
public record DeleteUserCommand(Guid Id, Guid CurrentUserId, UserRole OperatorRole)
    : IRequest<Result>;
