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

    public async Task<PagedResult<MedicalCaseDetailDto>> SearchAsync(string? patientName = null, string? diagnosisKeyword = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
    {
        var url = $"/api/medicalcases/search?patientName={patientName}&diagnosisKeyword={diagnosisKeyword}&startDate={startDate:O}&endDate={endDate:O}&page={page}&pageSize={pageSize}";
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PagedResult<MedicalCaseDetailDto>>(json, Json) ?? new PagedResult<MedicalCaseDetailDto>();
    }

    public async Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id)
    {
        var response = await _http.GetAsync($"/api/medicalcases/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MedicalCaseDetailDto>(json, Json);
    }

    public async Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query)
    {
        var qs = $"queryType={query.QueryType}" +
                 $"&patientId={query.PatientId}" +
                 $"&doctorId={query.DoctorId}" +
                 $"&keyword={query.Keyword}" +
                 $"&pageIndex={query.PageIndex}" +
                 $"&pageSize={query.PageSize}" +
                 $"&includeAllDoctors={query.IncludeAllDoctors}" +
                 $"&limit={query.Limit}";
        var response = await _http.GetAsync($"/api/medicalcases/query?{qs}");
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PagedResult<MedicalCaseListDto>>(resultJson, Json) ?? new PagedResult<MedicalCaseListDto>();
    }

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

    public async Task<MedicalCaseDetailDto?> CloseCaseAsync(Guid medicalCaseId)
    {
        var response = await _http.PutAsync($"/api/medicalcases/{medicalCaseId}/close", null);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MedicalCaseDetailDto>(json, Json);
    }

    public async Task<MedicalCasePermissionDto?> GetPermissionsAsync(Guid medicalCaseId)
    {
        var response = await _http.GetAsync($"/api/medicalcases/{medicalCaseId}/permissions");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MedicalCasePermissionDto>(json, Json);
    }

    public async Task<MedicalCaseDetailDto> SaveAsync(Guid medicalCaseId, MedicalCaseInputDto dto)
    {
        var json = JsonSerializer.Serialize(dto, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync($"/api/medicalcases/{medicalCaseId}", content);
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MedicalCaseDetailDto>(resultJson, Json)!;
    }

    public async Task<List<MedicalCaseDetailDto>> GetBatchDetailsAsync(List<Guid> ids)
    {
        var json = JsonSerializer.Serialize(ids, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/medicalcases/batch-details", content);
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<MedicalCaseDetailDto>>(resultJson, Json) ?? [];
    }

    public async Task<MedicalCaseDetailDto?> SetPrescriptionFlagAsync(Guid id, SetPrescriptionFlagRequest request)
    {
        var json = JsonSerializer.Serialize(request, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync($"/api/medicalcases/{id}/prescription-flag", content);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MedicalCaseDetailDto>(resultJson, Json);
    }

    public async Task<MedicalCaseDetailDto?> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request)
    {
        var json = JsonSerializer.Serialize(request, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync($"/api/medicalcases/{id}/status", content);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MedicalCaseDetailDto>(resultJson, Json);
    }

    public async Task<MedicalCaseDetailDto?> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request)
    {
        var json = JsonSerializer.Serialize(request, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync($"/api/medicalcases/{id}/cancel", content);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (response.StatusCode == HttpStatusCode.NoContent) return null;
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MedicalCaseDetailDto>(resultJson, Json);
    }

    public async Task<MedicalCaseDetailDto?> SuspendAsync(Guid id, ConsultationInputDto? request)
    {
        var json = JsonSerializer.Serialize(request, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync($"/api/medicalcases/{id}/suspend", content);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MedicalCaseDetailDto>(resultJson, Json);
    }

    public async Task<MedicalCaseDetailDto?> RecordPrintCompletedAsync(Guid medicalCaseId, PrintCompletedRequest request)
    {
        var json = JsonSerializer.Serialize(request, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync($"/api/medicalcases/{medicalCaseId}/print-completed", content);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MedicalCaseDetailDto>(resultJson, Json);
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        var json = JsonSerializer.Serialize(ids, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/medicalcases/batch-delete", content);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BatchOperationResultDto>(resultJson, Json);
    }
}
