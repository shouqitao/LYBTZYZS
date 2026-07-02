using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

public record CompleteMedicalCaseCommand(
    Guid Id,
    Guid OperatorId,
    bool IsAdmin
) : IRequest<Result<bool>>;


