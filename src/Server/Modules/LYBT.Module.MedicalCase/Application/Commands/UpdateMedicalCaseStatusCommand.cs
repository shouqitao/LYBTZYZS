using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

public record UpdateMedicalCaseStatusCommand(
    Guid Id,
    MedicalCaseStatus Status,
    Guid OperatorId,
    bool IsAdmin
) : IRequest<Result<MedicalCaseDetailDto>>;


