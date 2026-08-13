using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using MediatR;

namespace LYBT.Module.Identity.Application.Commands;

/// <summary>批量删除用户命令（UPDATEUSER-HIERARCHY-FIX: IsAdmin bool → OperatorRole）</summary>
public record BatchDeleteUsersCommand(List<Guid> Ids, Guid CurrentUserId, UserRole OperatorRole)
    : IRequest<Result<BatchOperationResultDto>>;
