using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.LocalWebAPI.Repositories;

public class HttpUserRepository : IUserRepository
{
    private readonly HttpClient _http;
    private readonly ILogger<HttpUserRepository> _logger;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null };

    public HttpUserRepository(HttpClient http, ILogger<HttpUserRepository> logger) { _http = http; _logger = logger; }

    public async Task<PagedResult<UserListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
    {
        var response = await _http.GetAsync($"/api/users?keyword={keyword}&page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PagedResult<UserListDto>>(json, Json) ?? new PagedResult<UserListDto>();
    }

    public async Task<UserDetailDto?> GetByIdAsync(Guid id)
    {
        var response = await _http.GetAsync($"/api/users/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UserDetailDto>(json, Json);
    }

    public async Task<UserDetailDto> CreateAsync(UserInputDto user)
    {
        var json = JsonSerializer.Serialize(user, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("/api/users", content);
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UserDetailDto>(resultJson, Json)!;
    }

    public async Task<UserDetailDto> UpdateAsync(UserInputDto user)
    {
        var json = JsonSerializer.Serialize(user, Json);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync($"/api/users/{user.Id}", content);
        response.EnsureSuccessStatusCode();
        var resultJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UserDetailDto>(resultJson, Json)!;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"/api/users/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<UserDetailDto> GetByUsernameAsync(string username)
    {
        var response = await _http.GetAsync($"/api/users?keyword={username}&page=1&pageSize=100");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var paged = JsonSerializer.Deserialize<PagedResult<UserListDto>>(json, Json);
        var user = paged?.Items.FirstOrDefault(u => u.Username == username);
        if (user == null) throw new KeyNotFoundException($"User not found: {username}");
        var detailResponse = await _http.GetAsync($"/api/users/{user.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detailJson = await detailResponse.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UserDetailDto>(detailJson, Json)!;
    }

    public async Task<List<UserListDto>> SearchAsync(string keyword)
    {
        var response = await _http.GetAsync($"/api/users?keyword={keyword}&page=1&pageSize=100");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var paged = JsonSerializer.Deserialize<PagedResult<UserListDto>>(json, Json);
        return paged?.Items ?? [];
    }

    public async Task<List<UserListDto>> GetDoctorsAsync()
    {
        var all = await SearchAsync("");
        return all.Where(u => u.Role == "Clinical" || u.Role == "Doctor").ToList();
    }

    public Task<UserDetailDto> ChangeProfileAsync(Guid userId, ChangeProfileDto dto)
    { _logger.LogWarning("[REPO:LocalWebAPI] User.ChangeProfileAsync - not supported"); return Task.FromResult<UserDetailDto>(null!); }

    public Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    { _logger.LogWarning("[REPO:LocalWebAPI] User.ChangePasswordAsync - not supported"); return Task.FromResult(new ServiceResult()); }

    public Task<ServiceResult<ResetPasswordResponseDto>> ResetPasswordAsync(Guid userId, ResetPasswordRequestDto request)
    { _logger.LogWarning("[REPO:LocalWebAPI] User.ResetPasswordAsync - not supported"); return Task.FromResult<ServiceResult<ResetPasswordResponseDto>>(null!); }

    public Task<UserBatchImportResultDto?> BatchImportAsync(UserBatchImportInputDto request)
    { _logger.LogWarning("[REPO:LocalWebAPI] User.BatchImportAsync - not supported"); return Task.FromResult<UserBatchImportResultDto?>(null); }

    public Task<UserDetailDto?> ToggleStatusAsync(Guid id)
    { _logger.LogWarning("[REPO:LocalWebAPI] User.ToggleStatusAsync - not supported"); return Task.FromResult<UserDetailDto?>(null); }

    public Task<UserDetailDto?> RestoreAsync(Guid id)
    { _logger.LogWarning("[REPO:LocalWebAPI] User.RestoreAsync - not supported"); return Task.FromResult<UserDetailDto?>(null); }

    public Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    { _logger.LogWarning("[REPO:LocalWebAPI] User.BatchDeleteAsync - not supported"); return Task.FromResult<BatchOperationResultDto?>(null); }

    public Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
    { _logger.LogWarning("[REPO:LocalWebAPI] User.BatchEnableAsync - not supported"); return Task.FromResult<BatchOperationResultDto?>(null); }

    public Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
    { _logger.LogWarning("[REPO:LocalWebAPI] User.BatchDisableAsync - not supported"); return Task.FromResult<BatchOperationResultDto?>(null); }
}
