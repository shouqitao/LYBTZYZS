using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Identity.Application.Commands;

public record BatchDeleteUsersCommand(
    List<Guid> Ids,
    Guid CurrentUserId,
    bool IsAdmin
) : IRequest<Result<BatchOperationResultDto>>;
