using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;

namespace LYBT.LocalWebAPI.Repositories;

public class HttpRegistrationRepository : IRegistrationRepository
{
    private readonly HttpClient _http;
    private readonly ILogger<HttpRegistrationRepository> _logger;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpRegistrationRepository(HttpClient http, ILogger<HttpRegistrationRepository> logger) { _http = http; _logger = logger; }

    public async Task<RegistrationDetailDto> CreateAsync(RegistrationInputDto input)
    {
        var json = JsonSerializer.Serialize(input, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/registrations", content);
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<RegistrationDetailDto>(resultJson, Json)!;
    }

    public async Task<RegistrationDetailDto?> GetByIdAsync(Guid id)
    {
        var response = await _http.GetAsync($"/api/registrations/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<RegistrationDetailDto>(json, Json);
    }

    public async Task<PagedResult<RegistrationListDto>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        var response = await _http.GetAsync($"/api/registrations?keyword={keyword}&page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PagedResult<RegistrationListDto>>(json, Json) ?? new PagedResult<RegistrationListDto>();
    }

    public Task<List<RegistrationListDto>> GetWaitingQueueAsync(Guid? doctorId = null)
    { _logger.LogWarning("[REPO:LocalWebAPI] Registration.GetWaitingQueueAsync - not supported"); return Task.FromResult<List<RegistrationListDto>>([]); }

    public Task<Guid?> StartVisitAsync(Guid id)
    { _logger.LogWarning("[REPO:LocalWebAPI] Registration.StartVisitAsync - not supported"); return Task.FromResult<Guid?>(null); }

    public async Task<bool> CancelAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"/api/registrations/{id}");
        return response.IsSuccessStatusCode;
    }
}
