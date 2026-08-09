// ---------------------------------------------------------------------------
// IdentityHttpApiClient — HttpClient adapter for IApiClientIdentity
// ---------------------------------------------------------------------------
// 合并 AuthHttpApiClient + UsersHttpApiClient（A-31-C3d）。
// LocalWebAPI mode implementation of IApiClientIdentity (split from HttpClientApiClient).
// 路由保持：认证 /api/v1/auth/* + 用户 /api/v1/users/*（Server IdentityController 双路由）。
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式认证与用户 API 客户端（拆分自 HttpClientApiClient）</summary>
internal sealed class IdentityHttpApiClient : HttpApiClientBase, IApiClientIdentity
{
    public IdentityHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

    // ========== 认证端点（/api/v1/auth/*） ==========

    public Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest loginRequest)
        => PostAndWrapAsync<LoginResponse>("/api/v1/auth/login", loginRequest);

    public Task<ApiResponse<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request)
        => PostAndWrapAsync<LoginResponse>("/api/v1/auth/auto-login", request);

    public async Task<ApiResponse> LogoutAsync(LogoutRequest logoutRequest)
    {
        await PostVoidAsync("/api/v1/auth/logout", logoutRequest);
        return WrapSuccess();
    }

    public Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        => PostAndWrapAsync<LoginResponse>("/api/v1/auth/refresh", request);

    public Task<ApiResponse<ValidateTokenResponse>> ValidateTokenAsync()
        => GetAndWrapAsync<ValidateTokenResponse>("/api/v1/auth/validate");

    public Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync()
        => GetAndWrapAsync<HealthCheckResponse>("/api/v1/health");

    // ========== 用户管理端点（/api/v1/users/*） ==========

    public async Task<ApiResponse<PagedResult<UserListDto>>> GetUsersAsync(
        int page, int pageSize, string? keyword)
    {
        var url = BuildPagedUrl("/api/v1/users", page, pageSize, ("keyword", keyword));
        return await GetPagedAndWrapAsync<UserListDto>(url);
    }

    public Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid id)
        => GetAndWrapAsync<UserDetailDto>($"/api/v1/users/{id}");

    public Task<ApiResponse<UserDetailDto>> CreateUserAsync(UserInputDto request)
        => PostAndWrapAsync<UserDetailDto>("/api/v1/users", request);

    public Task<ApiResponse<UserDetailDto>> UpdateUserAsync(Guid id, UserInputDto request)
        => PutAndWrapAsync<UserDetailDto>($"/api/v1/users/{id}", request);

    public Task<ApiResponse> DeleteUserAsync(Guid id)
        => DeleteVoidAsync($"/api/v1/users/{id}");

    public Task<ApiResponse<UserDetailDto>> ChangeProfileAsync(Guid id, ChangeProfileDto request)
        => PutAndWrapAsync<UserDetailDto>($"/api/v1/users/{id}/profile", request);

    public Task<ApiResponse> ChangePasswordAsync(Guid id, ChangePasswordRequest request)
        => PutVoidAsync($"/api/v1/users/{id}/change-password", request);

    public Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequest request)
        => PostAndWrapAsync<ResetPasswordResponseDto>($"/api/v1/users/{id}/reset-password", request);

    public Task<ApiResponse<UserDetailDto>> ToggleStatusAsync(Guid id)
        => PostAndWrapAsync<UserDetailDto>($"/api/v1/users/{id}/toggle-status");

    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/users/batch-delete", request);

    public Task<ApiResponse<UserDetailDto>> RestoreAsync(Guid id)
        => PostAndWrapAsync<UserDetailDto>($"/api/v1/users/{id}/restore");

    public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/users/batch-enable", request);

    public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request)
        => PostAndWrapAsync<BatchOperationResultDto>("/api/v1/users/batch-disable", request);

    public Task<UserDetailDto> GetCurrentUserAsync()
        => GetRawAsync<UserDetailDto>("/api/v1/users/current");
}
