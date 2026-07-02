using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

public record SetPrescriptionFlagCommand(
    Guid Id,
    bool NeedsPrescription,
    Guid OperatorId,
    bool IsAdmin
) : IRequest<Result<MedicalCaseDetailDto>>;


