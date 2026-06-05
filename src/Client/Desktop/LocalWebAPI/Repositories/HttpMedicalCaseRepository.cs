using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.LocalWebAPI.Repositories;

public class HttpMedicalCaseRepository : IMedicalCaseRepository
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<HttpMedicalCaseRepository> _logger;

    public HttpMedicalCaseRepository(IApiClient apiClient, ILogger<HttpMedicalCaseRepository> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<PagedResult<MedicalCaseListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
    {
        var response = await _apiClient.MedicalCases.GetMedicalCasesAsync(page, pageSize, keyword);
        if (response.Data == null)
            return new PagedResult<MedicalCaseListDto>();
        return new PagedResult<MedicalCaseListDto>
        {
            Items = response.Data.Items.ToList(),
            TotalCount = response.Data.TotalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<MedicalCaseDetailDto>> SearchAsync(string? patientName = null, string? diagnosisKeyword = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
    {
        var response = await _apiClient.MedicalCases.SearchMedicalCasesAsync(
            patientName, diagnosisKeyword, startDate, endDate, page, pageSize);
        if (response.Data == null)
            return new PagedResult<MedicalCaseDetailDto>();
        return response.Data;
    }

    public async Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id)
    {
        var response = await _apiClient.MedicalCases.GetMedicalCaseByIdAsync(id);
        return response.Data;
    }

    public async Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query)
    {
        var response = await _apiClient.MedicalCases.QueryMedicalCasesAsync(
            queryType: query.QueryType,
            patientId: query.PatientId,
            doctorId: query.DoctorId,
            keyword: query.Keyword,
            pageIndex: query.PageIndex,
            pageSize: query.PageSize,
            includeAllDoctors: query.IncludeAllDoctors,
            limit: query.Limit);
        if (response.Data == null)
            return new PagedResult<MedicalCaseListDto>();
        return response.Data;
    }

    public async Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseInputDto dto)
    {
        var response = await _apiClient.MedicalCases.CreateMedicalCaseAsync(dto);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Create medical case failed");
        return response.Data;
    }

    public async Task<MedicalCaseDetailDto> UpdateAsync(MedicalCaseInputDto dto)
    {
        var response = await _apiClient.MedicalCases.SaveAsync(dto.Id!.Value, dto);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Update medical case failed");
        return response.Data;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var response = await _apiClient.MedicalCases.DeleteMedicalCaseAsync(id);
        return response.Success;
    }

    public async Task<MedicalCaseDetailDto?> CloseCaseAsync(Guid medicalCaseId)
    {
        var response = await _apiClient.MedicalCases.CloseCaseAsync(medicalCaseId);
        return response.Data;
    }

    public async Task<MedicalCasePermissionDto?> GetPermissionsAsync(Guid medicalCaseId)
    {
        var response = await _apiClient.MedicalCases.GetPermissionsAsync(medicalCaseId);
        return response.Data;
    }

    public async Task<MedicalCaseDetailDto> SaveAsync(Guid medicalCaseId, MedicalCaseInputDto dto)
    {
        var response = await _apiClient.MedicalCases.SaveAsync(medicalCaseId, dto);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Aggregate save failed");
        return response.Data;
    }

    public async Task<List<MedicalCaseDetailDto>> GetBatchDetailsAsync(List<Guid> ids)
    {
        var response = await _apiClient.MedicalCases.GetBatchDetailsAsync(new BatchDetailQueryDto { Ids = ids });
        if (response.Success && response.Data != null)
            return response.Data;
        return [];
    }

    public async Task<MedicalCaseDetailDto?> SetPrescriptionFlagAsync(Guid id, SetPrescriptionFlagRequest request)
    {
        var response = await _apiClient.MedicalCases.SetPrescriptionFlagAsync(id, request);
        return response.Data;
    }

    public async Task<MedicalCaseDetailDto?> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request)
    {
        var response = await _apiClient.MedicalCases.UpdateStatusAsync(id, request);
        return response.Data;
    }

    public async Task<MedicalCaseDetailDto?> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request)
    {
        var response = await _apiClient.MedicalCases.CancelMedicalCaseAsync(id, request);
        return response.Success ? null : null;
    }

    public async Task<MedicalCaseDetailDto?> SuspendAsync(Guid id, ConsultationInputDto? request)
    {
        var response = await _apiClient.MedicalCases.SuspendAsync(id, request);
        return response.Data;
    }

    public async Task<MedicalCaseDetailDto?> RecordPrintCompletedAsync(Guid medicalCaseId, PrintCompletedRequest request)
    {
        var response = await _apiClient.MedicalCases.RecordPrintCompletedAsync(medicalCaseId, request);
        return response.Data;
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        var response = await _apiClient.MedicalCases.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
        return response.Data;
    }
}
