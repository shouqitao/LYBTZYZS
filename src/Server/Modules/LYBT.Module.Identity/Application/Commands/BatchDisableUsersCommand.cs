using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Identity.Application.Commands;

public record BatchDisableUsersCommand(
    List<Guid> Ids
) : IRequest<Result<BatchOperationResultDto>>;
