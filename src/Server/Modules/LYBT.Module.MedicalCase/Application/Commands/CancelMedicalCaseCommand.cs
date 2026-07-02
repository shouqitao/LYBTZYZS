using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

public record CancelMedicalCaseCommand(
    Guid Id,
    Guid OperatorId,
    bool IsAdmin,
    string? Reason = null
) : IRequest<Result<bool>>;


