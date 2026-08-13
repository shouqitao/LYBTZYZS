using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using MediatR;

namespace LYBT.Module.Identity.Application.Commands;

/// <summary>
/// 更新用户命令（UPDATEUSER-HIERARCHY-FIX: 加 OperatorId/OperatorRole——原仅 CurrentUserId，
/// Handler 无法做层级校验——Admin 可更新 Admin 违反 USER-D05）。
/// </summary>
public record UpdateUserCommand(
    Guid Id,
    UserInputDto Input,
    Guid CurrentUserId,
    Guid OperatorId,
    UserRole OperatorRole
) : IRequest<Result<UserDetailDto>>;
