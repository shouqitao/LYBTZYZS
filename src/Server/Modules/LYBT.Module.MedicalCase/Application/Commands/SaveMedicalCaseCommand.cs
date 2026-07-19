using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

public record SaveMedicalCaseCommand(
    MedicalCaseInputDto Input,
    Guid OperatorId,
    bool IsAdmin
) : IRequest<Result<MedicalCaseDetailDto>>;


