using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Users.Application.Commands;

public record BatchDeleteUsersCommand(
    List<Guid> Ids,
    Guid CurrentUserId,
    bool IsAdmin
) : IRequest<Result<BatchOperationResultDto>>;


