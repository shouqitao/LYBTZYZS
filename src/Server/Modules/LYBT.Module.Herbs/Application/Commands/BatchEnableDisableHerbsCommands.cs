using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Application.Commands;

public record BatchEnableHerbsCommand(
    List<Guid> Ids
) : IRequest<Result<BatchOperationResultDto>>;

public record BatchDisableHerbsCommand(
    List<Guid> Ids
) : IRequest<Result<BatchOperationResultDto>>;
