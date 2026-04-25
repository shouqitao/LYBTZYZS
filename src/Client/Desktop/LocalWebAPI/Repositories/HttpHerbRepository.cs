using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.LocalWebAPI.Repositories;

public class HttpHerbRepository : IHerbRepository
{
    private readonly HttpClient _http;
    private readonly ILogger<HttpHerbRepository> _logger;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpHerbRepository(HttpClient http, ILogger<HttpHerbRepository> logger) { _http = http; _logger = logger; }

    public async Task<PagedResult<HerbListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
    {
        var response = await _http.GetAsync($"/api/herbs?keyword={keyword}&page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PagedResult<HerbListDto>>(json, Json) ?? new PagedResult<HerbListDto>();
    }

    public async Task<HerbDetailDto?> GetByIdAsync(Guid id)
    {
        var response = await _http.GetAsync($"/api/herbs/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<HerbDetailDto>(json, Json);
    }

    public async Task<HerbDetailDto> CreateAsync(HerbInputDto dto)
    {
        var json = JsonSerializer.Serialize(dto, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/herbs", content);
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<HerbDetailDto>(resultJson, Json)!;
    }

    public async Task<HerbDetailDto> UpdateAsync(HerbInputDto dto)
    {
        var json = JsonSerializer.Serialize(dto, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync($"/api/herbs/{dto.Id}", content);
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<HerbDetailDto>(resultJson, Json)!;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"/api/herbs/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<HerbListDto>> SearchAsync(string keyword)
    {
        var response = await _http.GetAsync($"/api/herbs?keyword={keyword}&page=1&pageSize=100");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var paged = JsonSerializer.Deserialize<PagedResult<HerbListDto>>(json, Json);
        return paged?.Items ?? [];
    }

    public Task<HerbBatchImportResultDto?> BatchImportAsync(System.IO.Stream fileStream, string fileName)
    { _logger.LogWarning("[REPO:LocalWebAPI] Herb.BatchImportAsync - not supported"); return Task.FromResult<HerbBatchImportResultDto?>(null); }

    public Task<byte[]?> ExportTemplateAsync()
    { _logger.LogWarning("[REPO:LocalWebAPI] Herb.ExportTemplateAsync - not supported"); return Task.FromResult<byte[]?>(null); }

    public Task<byte[]?> ExportHerbsAsync(string? keyword = null)
    { _logger.LogWarning("[REPO:LocalWebAPI] Herb.ExportHerbsAsync - not supported"); return Task.FromResult<byte[]?>(null); }

    public Task<HerbDetailDto?> ToggleStatusAsync(Guid id)
    { _logger.LogWarning("[REPO:LocalWebAPI] Herb.ToggleStatusAsync - not supported"); return Task.FromResult<HerbDetailDto?>(null); }

    public Task<HerbDetailDto?> RestoreAsync(Guid id)
    { _logger.LogWarning("[REPO:LocalWebAPI] Herb.RestoreAsync - not supported"); return Task.FromResult<HerbDetailDto?>(null); }

    public Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    { _logger.LogWarning("[REPO:LocalWebAPI] Herb.BatchDeleteAsync - not supported"); return Task.FromResult<BatchOperationResultDto?>(null); }

    public Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
    { _logger.LogWarning("[REPO:LocalWebAPI] Herb.BatchEnableAsync - not supported"); return Task.FromResult<BatchOperationResultDto?>(null); }

    public Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
    { _logger.LogWarning("[REPO:LocalWebAPI] Herb.BatchDisableAsync - not supported"); return Task.FromResult<BatchOperationResultDto?>(null); }

    public async Task<(bool success, HerbDetailDto? data, string? error)> CreateWithResultAsync(HerbInputDto input)
    {
        try { var d = await CreateAsync(input); return (true, d, null); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool success, HerbDetailDto? data, string? error)> UpdateWithResultAsync(Guid id, HerbInputDto input)
    {
        try { var d = await UpdateAsync(input); return (true, d, null); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool success, string? error)> DeleteWithResultAsync(Guid id)
    {
        try { var d = await DeleteAsync(id); return (d, null); }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool success, HerbDetailDto? data, string? error)> GetByIdWithResultAsync(Guid id)
    {
        try { var d = await GetByIdAsync(id); return (d != null, d, null); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }
}
