using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using MediatR;

namespace LYBT.Module.Identity.Application.Commands;

/// <summary>切换用户状态命令（UPDATEUSER-HIERARCHY-FIX: IsAdmin bool → OperatorRole）</summary>
public record ToggleUserStatusCommand(Guid Id, Guid CurrentUserId, UserRole OperatorRole)
    : IRequest<Result<UserDetailDto>>;
