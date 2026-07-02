using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

public record GetMedicalCaseQuery(
    Guid Id
) : IRequest<Result<MedicalCaseDetailDto>>;


