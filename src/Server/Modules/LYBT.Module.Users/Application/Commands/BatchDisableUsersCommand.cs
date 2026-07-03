using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Users.Application.Commands;

public record BatchDisableUsersCommand(
    List<Guid> Ids
) : IRequest<Result<BatchOperationResultDto>>;
