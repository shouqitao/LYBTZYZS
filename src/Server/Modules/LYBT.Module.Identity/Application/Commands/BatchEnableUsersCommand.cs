using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Identity.Application.Commands;

public record BatchEnableUsersCommand(
    List<Guid> Ids
) : IRequest<Result<BatchOperationResultDto>>;
