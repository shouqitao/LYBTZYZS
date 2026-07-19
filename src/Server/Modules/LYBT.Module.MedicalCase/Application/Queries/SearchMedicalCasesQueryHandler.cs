using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.MedicalCases.Application.Queries;

public class SearchMedicalCasesQueryHandler(
    IMedicalCaseRepository repository,
    MedicalCaseMapper mapper
) : IRequestHandler<SearchMedicalCasesQuery, Result<PagedResult<MedicalCaseDetailDto>>>
{
    public async Task<Result<PagedResult<MedicalCaseDetailDto>>> Handle(
        SearchMedicalCasesQuery request, CancellationToken cancellationToken)
    {
        var pagedResult = await repository.QueryPagedAsync(
            request.PatientName, request.StartDate, request.EndDate,
            request.DiagnosisKeyword, request.Page, request.PageSize, cancellationToken);

        var dtos = pagedResult.Items.Select(mapper.MapToMedicalCaseDetailDto).ToList();
        return Result<PagedResult<MedicalCaseDetailDto>>.Success(
            new PagedResult<MedicalCaseDetailDto>(dtos, pagedResult.TotalCount, request.Page, request.PageSize));
    }
}


