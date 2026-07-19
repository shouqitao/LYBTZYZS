using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

public record SuspendMedicalCaseCommand(
    Guid Id,
    Guid OperatorId,
    bool IsAdmin
) : IRequest<Result<bool>>;


