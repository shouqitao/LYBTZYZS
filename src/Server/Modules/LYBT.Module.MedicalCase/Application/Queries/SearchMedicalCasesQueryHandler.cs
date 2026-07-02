using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.MedicalCases.Application.Queries;

public class SearchMedicalCasesQueryHandler(
    IMedicalCaseRepository repository,
    MedicalCaseMapper mapper
) : IRequestHandler<SearchMedicalCasesQuery, Result<PagedResult<MedicalCaseDetailDto>>>
{
    public async Task<Result<PagedResult<MedicalCaseDetailDto>>> Handle(
        SearchMedicalCasesQuery request, CancellationToken cancellationToken)
    {
        var entities = await repository.QueryAsync(
            request.PatientName, request.StartDate, request.EndDate,
            request.DiagnosisKeyword, cancellationToken);

        var ordered = entities.OrderByDescending(e => e.CreatedAt).ToList();
        var totalCount = ordered.Count;
        var paged = ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var dtos = paged.Select(mapper.MapToMedicalCaseDetailDto).ToList();
        return Result<PagedResult<MedicalCaseDetailDto>>.Success(
            new PagedResult<MedicalCaseDetailDto>(dtos, totalCount, request.Page, request.PageSize));
    }
}


