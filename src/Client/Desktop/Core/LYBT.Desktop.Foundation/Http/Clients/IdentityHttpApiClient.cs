// ---------------------------------------------------------------------------
// IdentityHttpApiClient — HttpClient adapter for IApiClientIdentity
// ---------------------------------------------------------------------------
// 合并 AuthHttpApiClient + UsersHttpApiClient（A-31-C3d）。
// LocalWebAPI mode implementation of IApiClientIdentity (split from HttpClientApiClient).
// 路由保持：认证 /api/v1/auth/* + 用户 /api/v1/users/*（Server IdentityController 双路由）。
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using LYBT.Desktop.Contracts.ApiClient;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式认证与用户 API 客户端（拆分自 HttpClientApiClient）</summary>
internal sealed class IdentityHttpApiClient : HttpApiClientBase, IApiClientIdentity
{
    public IdentityHttpApiClient(IHttpClientFactory httpClientFactory, ILogger logger) : base(httpClientFactory, logger) { }

    // ========== 认证端点（/api/v1/auth/*） ==========

    public Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest loginRequest, CancellationToken ct = default)
        => PostAndWrapAsync<LoginResponse>("/api/v1/auth/login", loginRequest, ct);

    public Task<ApiResponse<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request, CancellationToken ct = default)
        => PostAndWrapAsync<LoginResponse>("/api/v1/auth/auto-login", request, ct);

    public async Task<ApiResponse> LogoutAsync(LogoutRequest logoutRequest, CancellationToken ct = default)
    {
        await PostVoidAsync("/api/v1/auth/logout", logoutRequest, ct);
        return WrapSuccess();
    }

    public Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
        => PostAndWrapAsync<LoginResponse>("/api/v1/auth/refresh", request, ct);

    public Task<ApiResponse<ValidateTokenResponse>> ValidateTokenAsync(CancellationToken ct = default)
        => GetAndWrapAsync<ValidateTokenResponse>("/api/v1/auth/validate", ct);

    public Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync(CancellationToken ct = default)
        => GetAndWrapAsync<HealthCheckResponse>("/api/v1/health", ct);

    // ========== 用户管理端点（/api/v1/users/*） ==========

    public async Task<ApiResponse<PagedResult<UserListDto>>> GetUsersAsync(
        int page, int pageSize, string? keyword,
        CancellationToken ct = default)
    {
        var url = BuildPagedUrl("/api/v1/users", page, pageSize, ("keyword", keyword));
        return await GetPagedAndWrapAsync<UserListDto>(url, ct);
    }

    public Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid id, CancellationToken ct = default)
        => GetAndWrapAsync<UserDetailDto>($"/api/v1/users/{id}", ct);

    public Task<ApiResponse<UserDetailDto>> CreateUserAsync(UserInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<UserDetailDto>("/api/v1/users", request, ct);

    public Task<ApiResponse<UserDetailDto>> UpdateUserAsync(Guid id, UserInputDto request, CancellationToken ct = default)
        => PutAndWrapAsync<UserDetailDto>($"/api/v1/users/{id}", request, ct);

    public Task<ApiResponse> DeleteUserAsync(Guid id, CancellationToken ct = default)
        => DeleteVoidAsync($"/api/v1/users/{id}", ct);

    public Task<ApiResponse<UserDetailDto>> ChangeProfileAsync(Guid id, ChangeProfileDto request, CancellationToken ct = default)
        => PutAndWrapAsync<UserDetailDto>($"/api/v1/users/{id}/profile", request, ct);

    public Task<ApiResponse> ChangePasswordAsync(Guid id, ChangePasswordRequest request, CancellationToken ct = default)
        => PutVoidAsync($"/api/v1/users/{id}/change-password", request, ct);

    public Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default)
        => PostAndWrapAsync<ResetPasswordResponseDto>($"/api/v1/users/{id}/reset-password", request, ct);

    public Task<ApiResponse<UserDetailDto>> ToggleStatusAsync(Guid id, CancellationToken ct = default)
        => PostAndWrapAsync<UserDetailDto>($"/api/v1/users/{id}/toggle-status", ct: ct);

    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/users/batch-delete", request, ct);

    public Task<ApiResponse<UserDetailDto>> RestoreAsync(Guid id, CancellationToken ct = default)
        => PostAndWrapAsync<UserDetailDto>($"/api/v1/users/{id}/restore", ct: ct);

    public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/users/batch-enable", request, ct);

    public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/users/batch-disable", request, ct);

    public Task<UserDetailDto> GetCurrentUserAsync(CancellationToken ct = default)
        => GetRawAsync<UserDetailDto>("/api/v1/users/current", ct);

    public async Task<ApiResponse<PagedResult<SecurityAuditLogDto>>> GetSecurityAuditLogsAsync(
        int page = 1,
        int pageSize = 20,
        string? eventType = null,
        string? userName = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var url = BuildPagedUrl("/api/v1/security-audit", page, pageSize,
            ("eventType", eventType),
            ("userName", userName),
            ("from", from?.ToString("O")),
            ("to", to?.ToString("O")));
        return await GetPagedAndWrapAsync<SecurityAuditLogDto>(url, ct);
    }
}
