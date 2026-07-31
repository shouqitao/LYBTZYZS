using MediatR;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Formulas.Application.Commands;

public record BatchEnableFormulasCommand(
    List<Guid> Ids
) : IRequest<Result<BatchOperationResultDto>>;

public record BatchDisableFormulasCommand(
    List<Guid> Ids
) : IRequest<Result<BatchOperationResultDto>>;
