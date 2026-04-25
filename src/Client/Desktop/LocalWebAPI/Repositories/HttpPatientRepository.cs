using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.LocalWebAPI.Repositories;

public class HttpPatientRepository : IPatientRepository
{
    private readonly HttpClient _http;
    private readonly ILogger<HttpPatientRepository> _logger;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpPatientRepository(HttpClient http, ILogger<HttpPatientRepository> logger) { _http = http; _logger = logger; }

    public async Task<PagedResult<PatientListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/patients?keyword={keyword}&page={page}&pageSize={pageSize}", ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<PagedResult<PatientListDto>>(json, Json) ?? new PagedResult<PatientListDto>();
    }

    public async Task<PatientDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/patients/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<PatientDetailDto>(json, Json);
    }

    public async Task<PatientDetailDto> CreateAsync(PatientInputDto patient, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(patient, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/patients", content, ct);
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<PatientDetailDto>(resultJson, Json)!;
    }

    public async Task<PatientDetailDto> UpdateAsync(PatientInputDto patient, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(patient, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync($"/api/patients/{patient.Id}", content, ct);
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<PatientDetailDto>(resultJson, Json)!;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"/api/patients/{id}", ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<PatientListDto>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/patients?keyword={keyword}&page=1&pageSize=100", ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        var paged = JsonSerializer.Deserialize<PagedResult<PatientListDto>>(json, Json);
        return paged?.Items ?? [];
    }

    public Task<PatientDetailDto?> GetByIdNumberAsync(string idNumber, CancellationToken ct = default)
    { _logger.LogWarning("[REPO:LocalWebAPI] Patient.GetByIdNumberAsync - not supported"); return Task.FromResult<PatientDetailDto?>(null); }

    public Task<PatientBatchImportResultDto?> BatchImportAsync(PatientBatchImportInputDto request, CancellationToken ct = default)
    { _logger.LogWarning("[REPO:LocalWebAPI] Patient.BatchImportAsync - not supported"); return Task.FromResult<PatientBatchImportResultDto?>(null); }

    public Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)
    { _logger.LogWarning("[REPO:LocalWebAPI] Patient.ExportTemplateAsync - not supported"); return Task.FromResult<byte[]?>(null); }

    public Task<byte[]?> ExportPatientsAsync(string? keyword = null, CancellationToken ct = default)
    { _logger.LogWarning("[REPO:LocalWebAPI] Patient.ExportPatientsAsync - not supported"); return Task.FromResult<byte[]?>(null); }

    public Task<PatientDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
    { _logger.LogWarning("[REPO:LocalWebAPI] Patient.RestoreAsync - not supported"); return Task.FromResult<PatientDetailDto?>(null); }

    public Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    { _logger.LogWarning("[REPO:LocalWebAPI] Patient.BatchDeleteAsync - not supported"); return Task.FromResult<BatchOperationResultDto?>(null); }
}
