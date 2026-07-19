using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Formulas.Application.Commands;

public record BatchDeleteFormulasCommand(
    List<Guid> Ids,
    Guid OperatorId
) : IRequest<Result<BatchOperationResultDto>>;


