using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Identity.Application.Commands;

public record BatchDisableUsersCommand(
    List<Guid> Ids,
    Guid CurrentUserId = default,
    LYBT.Shared.Models.Enums.UserRole OperatorRole = LYBT.Shared.Models.Enums.UserRole.Admin
) : IRequest<Result<BatchOperationResultDto>>;
