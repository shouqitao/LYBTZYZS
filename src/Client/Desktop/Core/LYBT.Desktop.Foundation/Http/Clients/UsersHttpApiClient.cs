// ---------------------------------------------------------------------------
// UsersHttpApiClient — HttpClient adapter for IApiClientUsers
// ---------------------------------------------------------------------------
// LocalWebAPI mode implementation of IApiClientUsers (split from HttpClientApiClient).
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>本地模式用户 API 客户端（拆分自 HttpClientApiClient）</summary>
internal sealed class UsersHttpApiClient : HttpApiClientBase, IApiClientUsers
{
    public UsersHttpApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory) { }

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
