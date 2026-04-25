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

    public Task<FormulaDetailDto> CloneFormulaAsync(Guid formulaId)
    { _logger.LogWarning("[REPO:LocalWebAPI] Formula.CloneFormulaAsync - not supported"); return Task.FromResult<FormulaDetailDto>(null!); }

    public Task<FormulaDetailDto?> ToggleStatusAsync(Guid id)
    { _logger.LogWarning("[REPO:LocalWebAPI] Formula.ToggleStatusAsync - not supported"); return Task.FromResult<FormulaDetailDto?>(null); }

    public Task<FormulaDetailDto?> RestoreAsync(Guid id)
    { _logger.LogWarning("[REPO:LocalWebAPI] Formula.RestoreAsync - not supported"); return Task.FromResult<FormulaDetailDto?>(null); }

    public Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    { _logger.LogWarning("[REPO:LocalWebAPI] Formula.BatchDeleteAsync - not supported"); return Task.FromResult<BatchOperationResultDto?>(null); }

    public Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
    { _logger.LogWarning("[REPO:LocalWebAPI] Formula.BatchEnableAsync - not supported"); return Task.FromResult<BatchOperationResultDto?>(null); }

    public Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
    { _logger.LogWarning("[REPO:LocalWebAPI] Formula.BatchDisableAsync - not supported"); return Task.FromResult<BatchOperationResultDto?>(null); }

    public Task<FormulaBatchImportResultDto?> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default)
    { _logger.LogWarning("[REPO:LocalWebAPI] Formula.BatchImportAsync - not supported"); return Task.FromResult<FormulaBatchImportResultDto?>(null); }

    public Task<byte[]?> ExportFormulasAsync(string? category = null, CancellationToken ct = default)
    { _logger.LogWarning("[REPO:LocalWebAPI] Formula.ExportFormulasAsync - not supported"); return Task.FromResult<byte[]?>(null); }

    public Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)
    { _logger.LogWarning("[REPO:LocalWebAPI] Formula.ExportTemplateAsync - not supported"); return Task.FromResult<byte[]?>(null); }
}
