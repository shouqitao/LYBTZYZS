using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

public record QueryMedicalCasesCommand(
    MedicalCaseQueryDto Query
) : IRequest<Result<PagedResult<MedicalCaseListDto>>>;


