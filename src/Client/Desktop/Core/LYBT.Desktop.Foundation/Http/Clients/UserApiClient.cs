// ---------------------------------------------------------------------------
// UserApiClient — Refit adapter for IApiClientUsers
// ---------------------------------------------------------------------------
// Delegates remote calls to IUserApi (Refit-generated HTTP client).
// Local-only methods throw NotSupportedException in remote mode.
// ---------------------------------------------------------------------------

using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>
/// User management API client — wraps IUserApi (Refit) to implement IApiClientUsers.
/// </summary>
internal sealed class UserApiClient : IApiClientUsers
{
    private readonly IUserApi _api;

    /// <summary>
    /// Initializes a new instance of <see cref="UserApiClient"/>.
    /// </summary>
    /// <param name="api">Refit-generated user API client.</param>
    public UserApiClient(IUserApi api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<UserListDto>>> GetUsersAsync(
        int page = 1, int pageSize = 20, string? keyword = null)
        => _api.GetUsersAsync(page, pageSize, keyword);

    /// <inheritdoc />
    public Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid id)
        => _api.GetUserByIdAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<UserDetailDto>> CreateUserAsync(UserInputDto request)
        => _api.CreateUserAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<UserDetailDto>> UpdateUserAsync(Guid id, UserInputDto request)
        => _api.UpdateUserAsync(id, request);

    /// <inheritdoc />
    public Task<ApiResponse> DeleteUserAsync(Guid id)
        => _api.DeleteUserAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<UserDetailDto>> ChangeProfileAsync(Guid id, ChangeProfileDto request)
        => _api.ChangeProfileAsync(id, request);

    /// <inheritdoc />
    public Task<ApiResponse> ChangePasswordAsync(Guid id, ChangePasswordRequest request)
        => _api.ChangePasswordAsync(id, request);

    /// <inheritdoc />
    public Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequestDto request)
        => _api.ResetPasswordAsync(id, request);

    /// <inheritdoc />
    public Task<ApiResponse<UserBatchImportResultDto>> BatchImportAsync(UserBatchImportInputDto request)
        => _api.BatchImportAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<UserDetailDto>> ToggleStatusAsync(Guid id)
        => _api.ToggleStatusAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<UserDetailDto>> RestoreAsync(Guid id)
        => _api.RestoreAsync(id);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request)
        => _api.BatchDeleteAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request)
        => _api.BatchEnableAsync(request);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request)
        => _api.BatchDisableAsync(request);

    /// <inheritdoc />
    /// <remarks>Local-only method — not available in remote Refit mode.</remarks>
    public Task<UserDetailDto> GetCurrentUserAsync()
        => throw new NotSupportedException("GetCurrentUserAsync is a local-only method and is not available in remote mode.");
}
