using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Formulas.Application.Commands;

public record ToggleFormulaStatusCommand(
    Guid Id,
    Guid OperatorId
) : IRequest<Result<FormulaDetailDto>>;


