using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.MedicalCases.Application.Queries;

public record SearchMedicalCasesQuery(
    string? PatientName = null,
    string? DiagnosisKeyword = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PagedResult<MedicalCaseDetailDto>>>;


