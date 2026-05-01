using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.LocalWebAPI.Repositories;

public class HttpFormulaRepository : IFormulaRepository
{
    private readonly HttpClient _http;
    private readonly ILogger<HttpFormulaRepository> _logger;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpFormulaRepository(HttpClient http, ILogger<HttpFormulaRepository> logger) { _http = http; _logger = logger; }

    public async Task<PagedResult<FormulaListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
    {
        var response = await _http.GetAsync($"/api/formulas?keyword={keyword}&page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PagedResult<FormulaListDto>>(json, Json) ?? new PagedResult<FormulaListDto>();
    }

    public async Task<FormulaDetailDto?> GetByIdAsync(Guid id)
    {
        var response = await _http.GetAsync($"/api/formulas/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FormulaDetailDto>(json, Json);
    }

    public async Task<FormulaDetailDto> CreateAsync(FormulaInputDto dto)
    {
        var json = JsonSerializer.Serialize(dto, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/formulas", content);
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FormulaDetailDto>(resultJson, Json)!;
    }

    public async Task<FormulaDetailDto> UpdateAsync(FormulaInputDto dto)
    {
        var json = JsonSerializer.Serialize(dto, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync($"/api/formulas/{dto.Id}", content);
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FormulaDetailDto>(resultJson, Json)!;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"/api/formulas/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<FormulaListDto>> SearchAsync(string keyword)
    {
        var response = await _http.GetAsync($"/api/formulas?keyword={keyword}&page=1&pageSize=100");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var paged = JsonSerializer.Deserialize<PagedResult<FormulaListDto>>(json, Json);
        return paged?.Items ?? [];
    }

    public async Task<FormulaDetailDto> CloneFormulaAsync(Guid formulaId)
    {
        var response = await _http.PostAsync($"/api/formulas/{formulaId}/clone", null);
        if (response.StatusCode == HttpStatusCode.NotFound) return null!;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FormulaDetailDto>(json, Json)!;
    }

    public async Task<FormulaDetailDto?> ToggleStatusAsync(Guid id)
    {
        var response = await _http.PostAsync($"/api/formulas/{id}/toggle-status", null);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FormulaDetailDto>(json, Json);
    }

    public async Task<FormulaDetailDto?> RestoreAsync(Guid id)
    {
        var response = await _http.PostAsync($"/api/formulas/{id}/restore", null);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<FormulaDetailDto>(json, Json);
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        var body = JsonSerializer.Serialize(new { ids }, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/formulas/batch-delete", content);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BatchOperationResultDto>(json, Json);
    }

    public async Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
    {
        var body = JsonSerializer.Serialize(new { ids }, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/formulas/batch-enable", content);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BatchOperationResultDto>(json, Json);
    }

    public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
    {
        var body = JsonSerializer.Serialize(new { ids }, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/formulas/batch-disable", content);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BatchOperationResultDto>(json, Json);
    }

    public async Task<FormulaBatchImportResultDto?> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(request, Json);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/formulas/import", content, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<FormulaBatchImportResultDto>(json, Json);
    }

    public async Task<byte[]?> ExportFormulasAsync(string? category = null, CancellationToken ct = default)
    {
        var url = category is null ? "/api/formulas/export" : $"/api/formulas/export?category={Uri.EscapeDataString(category)}";
        var response = await _http.GetAsync(url, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("/api/formulas/import-template", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
