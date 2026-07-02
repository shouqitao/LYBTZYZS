using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

public record SaveMedicalCaseCommand(
    MedicalCaseInputDto Input,
    Guid OperatorId,
    bool IsAdmin
) : IRequest<Result<MedicalCaseDetailDto>>;


