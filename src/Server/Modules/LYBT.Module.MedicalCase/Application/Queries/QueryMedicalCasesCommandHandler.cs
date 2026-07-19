using MediatR;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Module.MedicalCases.Application.Queries;

public class QueryMedicalCasesCommandHandler(
    IMedicalCaseRepository repository,
    MedicalCaseMapper mapper
) : IRequestHandler<QueryMedicalCasesCommand, Result<PagedResult<MedicalCaseListDto>>>
{
    public async Task<Result<PagedResult<MedicalCaseListDto>>> Handle(
        QueryMedicalCasesCommand request, CancellationToken cancellationToken)
    {
        var query = request.Query;

        return query.QueryType switch
        {
            LYBT.Shared.Models.Enums.MedicalCaseQueryType.ByPatient => await HandleByPatientAsync(query, cancellationToken),
            LYBT.Shared.Models.Enums.MedicalCaseQueryType.Pending => await HandlePendingAsync(query, cancellationToken),
            LYBT.Shared.Models.Enums.MedicalCaseQueryType.Unfinished => await HandleUnfinishedAsync(query, cancellationToken),
            LYBT.Shared.Models.Enums.MedicalCaseQueryType.Recent => await HandleRecentAsync(query, cancellationToken),
            _ => await HandleDefaultAsync(query, cancellationToken)
        };
    }

    private async Task<Result<PagedResult<MedicalCaseListDto>>> HandleByPatientAsync(
        LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseQueryDto query, CancellationToken ct)
    {
        if (!query.PatientId.HasValue)
            return Result<PagedResult<MedicalCaseListDto>>.Success(new PagedResult<MedicalCaseListDto>());

        var pagedResult = await repository.GetByPatientIdPagedAsync(
            query.PatientId.Value, query.PageIndex, query.PageSize, ct);

        var dtos = pagedResult.Items.Select(mapper.ToListDto).ToList();
        return Result<PagedResult<MedicalCaseListDto>>.Success(
            new PagedResult<MedicalCaseListDto>(dtos, pagedResult.TotalCount, query.PageIndex, query.PageSize));
    }

    private async Task<Result<PagedResult<MedicalCaseListDto>>> HandlePendingAsync(
        LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseQueryDto query, CancellationToken ct)
    {
        List<LYBT.Shared.Models.Contracts.MedicalCase.PendingMedicalCaseDto> pendingCases;
        if (query.IncludeAllDoctors || !query.DoctorId.HasValue)
            pendingCases = await repository.GetAllPendingCasesAsync(ct);
        else
            pendingCases = await repository.GetPendingCasesAsync(query.DoctorId.Value, null, ct);

        var dtos = pendingCases
            .Where(p => p.MedicalCaseId.HasValue)
            .Select(p => new LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseListDto
            {
                Id = p.MedicalCaseId!.Value,
                PatientId = p.PatientId,
                PatientName = p.PatientName,
                CaseStatus = LYBT.Shared.Models.Enums.MedicalCaseStatus.Active,
                CreatedAt = p.CreatedAt
            }).ToList();

        return Result<PagedResult<MedicalCaseListDto>>.Success(
            new PagedResult<MedicalCaseListDto>(dtos, dtos.Count, 1, dtos.Count));
    }

    private async Task<Result<PagedResult<MedicalCaseListDto>>> HandleUnfinishedAsync(
        LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseQueryDto query, CancellationToken ct)
    {
        if (!query.PatientId.HasValue)
            return Result<PagedResult<MedicalCaseListDto>>.Success(new PagedResult<MedicalCaseListDto>());

        var doctorId = query.IncludeAllDoctors ? Guid.Empty : (query.DoctorId ?? Guid.Empty);
        var unfinished = await repository.GetUnfinishedCaseByPatientIdAsync(query.PatientId.Value, doctorId, ct);

        if (unfinished == null)
            return Result<PagedResult<MedicalCaseListDto>>.Success(new PagedResult<MedicalCaseListDto>());

        var dto = mapper.ToListDto(unfinished);
        return Result<PagedResult<MedicalCaseListDto>>.Success(
            new PagedResult<MedicalCaseListDto>(new List<LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseListDto> { dto }, 1, 1, 1));
    }

    private async Task<Result<PagedResult<MedicalCaseListDto>>> HandleRecentAsync(
        LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseQueryDto query, CancellationToken ct)
    {
        if (!query.PatientId.HasValue)
            return Result<PagedResult<MedicalCaseListDto>>.Success(new PagedResult<MedicalCaseListDto>());

        var count = query.Limit ?? 5;
        var pagedResult = await repository.GetByPatientIdPagedAsync(
            query.PatientId.Value, 1, count, ct);

        var dtos = pagedResult.Items.Select(mapper.ToListDto).ToList();
        return Result<PagedResult<MedicalCaseListDto>>.Success(
            new PagedResult<MedicalCaseListDto>(dtos, dtos.Count, 1, dtos.Count));
    }

    private async Task<Result<PagedResult<MedicalCaseListDto>>> HandleDefaultAsync(
        LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseQueryDto query, CancellationToken ct)
    {
        var result = await repository.GetPagedWithDetailsAsync(
            query.PageIndex, query.PageSize,
            null, query.PatientId, query.DoctorId,
            query.IncludeAllDoctors, query.Keyword, ct);

        var dtos = result.Items.Select(mapper.ToListDto).ToList();
        for (int i = 0; i < result.Items.Count && i < dtos.Count; i++)
        {
            var entity = result.Items[i];
            dtos[i].HasConsultation = entity.Consultation != null && !entity.Consultation.IsDeleted;
            dtos[i].HasPrescription = entity.Prescription != null && !entity.Prescription.IsDeleted;
        }

        return Result<PagedResult<MedicalCaseListDto>>.Success(
            new PagedResult<MedicalCaseListDto>(dtos, result.TotalCount, query.PageIndex, query.PageSize));
    }
}


