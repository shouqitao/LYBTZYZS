using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.LocalWebAPI.Repositories;

public class HttpMedicalCaseRepository : IMedicalCaseRepository
{
    private readonly HttpClient _http;
    private readonly ILogger<HttpMedicalCaseRepository> _logger;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpMedicalCaseRepository(HttpClient http, ILogger<HttpMedicalCaseRepository> logger) { _http = http; _logger = logger; }

    public async Task<PagedResult<MedicalCaseListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
    {
        var response = await _http.GetAsync($"/api/medicalcases?keyword={keyword}&page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PagedResult<MedicalCaseListDto>>(json, Json) ?? new PagedResult<MedicalCaseListDto>();
    }

    public Task<PagedResult<MedicalCaseDetailDto>> SearchAsync(string? patientName = null, string? diagnosisKeyword = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
    { _logger.LogWarning("[REPO:LocalWebAPI] MedicalCase.SearchAsync - not supported"); return Task.FromResult(new PagedResult<MedicalCaseDetailDto>()); }

    public async Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id)
    {
        var response = await _http.GetAsync($"/api/medicalcases/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MedicalCaseDetailDto>(json, Json);
    }

    public Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query)
    { _logger.LogWarning("[REPO:LocalWebAPI] MedicalCase.QueryAsync - not supported"); return Task.FromResult(new PagedResult<MedicalCaseListDto>()); }

    public async Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseInputDto dto)
    {
        var json = JsonSerializer.Serialize(dto, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/medicalcases", content);
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MedicalCaseDetailDto>(resultJson, Json)!;
    }

    public async Task<MedicalCaseDetailDto> UpdateAsync(MedicalCaseInputDto dto)
    {
        var json = JsonSerializer.Serialize(dto, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync($"/api/medicalcases/{dto.Id}", content);
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MedicalCaseDetailDto>(resultJson, Json)!;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"/api/medicalcases/{id}");
        return response.IsSuccessStatusCode;
    }

    public Task<MedicalCaseDetailDto?> CloseCaseAsync(Guid medicalCaseId)
    { _logger.LogWarning("[REPO:LocalWebAPI] MedicalCase.CloseCaseAsync - not supported"); return Task.FromResult<MedicalCaseDetailDto?>(null); }

    public Task<MedicalCasePermissionDto?> GetPermissionsAsync(Guid medicalCaseId)
    { _logger.LogWarning("[REPO:LocalWebAPI] MedicalCase.GetPermissionsAsync - not supported"); return Task.FromResult<MedicalCasePermissionDto?>(null); }

    public Task<MedicalCaseDetailDto> SaveAsync(Guid medicalCaseId, MedicalCaseInputDto dto)
    { _logger.LogWarning("[REPO:LocalWebAPI] MedicalCase.SaveAsync - not supported"); return Task.FromResult<MedicalCaseDetailDto>(null!); }

    public Task<List<MedicalCaseDetailDto>> GetBatchDetailsAsync(List<Guid> ids)
    { _logger.LogWarning("[REPO:LocalWebAPI] MedicalCase.GetBatchDetailsAsync - not supported"); return Task.FromResult<List<MedicalCaseDetailDto>>([]); }

    public Task<MedicalCaseDetailDto?> SetPrescriptionFlagAsync(Guid id, SetPrescriptionFlagRequest request)
    { _logger.LogWarning("[REPO:LocalWebAPI] MedicalCase.SetPrescriptionFlagAsync - not supported"); return Task.FromResult<MedicalCaseDetailDto?>(null); }

    public Task<MedicalCaseDetailDto?> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request)
    { _logger.LogWarning("[REPO:LocalWebAPI] MedicalCase.UpdateStatusAsync - not supported"); return Task.FromResult<MedicalCaseDetailDto?>(null); }

    public Task<MedicalCaseDetailDto?> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request)
    { _logger.LogWarning("[REPO:LocalWebAPI] MedicalCase.CancelMedicalCaseAsync - not supported"); return Task.FromResult<MedicalCaseDetailDto?>(null); }

    public Task<MedicalCaseDetailDto?> SuspendAsync(Guid id, ConsultationInputDto? request)
    { _logger.LogWarning("[REPO:LocalWebAPI] MedicalCase.SuspendAsync - not supported"); return Task.FromResult<MedicalCaseDetailDto?>(null); }

    public Task<MedicalCaseDetailDto?> RecordPrintCompletedAsync(Guid medicalCaseId, PrintCompletedRequest request)
    { _logger.LogWarning("[REPO:LocalWebAPI] MedicalCase.RecordPrintCompletedAsync - not supported"); return Task.FromResult<MedicalCaseDetailDto?>(null); }

    public Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    { _logger.LogWarning("[REPO:LocalWebAPI] MedicalCase.BatchDeleteAsync - not supported"); return Task.FromResult<BatchOperationResultDto?>(null); }
}
