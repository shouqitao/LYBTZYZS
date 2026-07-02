using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Commands;

public record CreateMedicalCaseCommand(
    MedicalCaseInputDto Input,
    Guid CurrentUserId
) : IRequest<Result<MedicalCaseDetailDto>>;


