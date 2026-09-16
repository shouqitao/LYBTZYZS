// ---------------------------------------------------------------------------
// IdentityApiClient — Refit adapter for IApiClientIdentity
// ---------------------------------------------------------------------------
// 合并 AuthApiClient + UserApiClient（A-31-C3d）。
// 认证方法委托 IAuthApi，用户方法委托 IUserApi（均 Refit-generated HTTP client）。
// Local-only 方法（RestoreAsync/BatchEnableAsync/BatchDisableAsync/GetCurrentUserAsync）
// 在远程模式抛 NotSupportedException。
// Part of the IApiClient unified abstraction layer.
// ---------------------------------------------------------------------------

using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Foundation.Http.Clients;

/// <summary>
/// 认证与用户管理 API 客户端——包装 IAuthApi + IUserApi（Refit）以实现 <see cref="IApiClientIdentity"/>。
/// </summary>
internal sealed class IdentityApiClient : IApiClientIdentity
{
    private readonly IAuthApi _authApi;
    private readonly IUserApi _userApi;

    /// <summary>
    /// 初始化 <see cref="IdentityApiClient"/> 类的新实例。
    /// </summary>
    /// <param name="authApi">Refit-generated authentication API client.</param>
    /// <param name="userApi">Refit-generated user API client.</param>
    public IdentityApiClient(IAuthApi authApi, IUserApi userApi)
    {
        _authApi = authApi ?? throw new ArgumentNullException(nameof(authApi));
        _userApi = userApi ?? throw new ArgumentNullException(nameof(userApi));
    }

    /// <inheritdoc />
    public Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest loginRequest, CancellationToken ct = default)
        => _authApi.LoginAsync(loginRequest, ct);

    /// <inheritdoc />
    public Task<ApiResponse<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request, CancellationToken ct = default)
        => _authApi.LoginWithAutoTokenAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse> LogoutAsync(LogoutRequest logoutRequest, CancellationToken ct = default)
        => _authApi.LogoutAsync(logoutRequest, ct);

    /// <inheritdoc />
    public Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
        => _authApi.RefreshTokenAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<ValidateTokenResponse>> ValidateTokenAsync(CancellationToken ct = default)
        => _authApi.ValidateTokenAsync(ct);

    /// <inheritdoc />
    public Task<ApiResponse<HealthCheckResponse>> HealthCheckAsync(CancellationToken ct = default)
        => _authApi.HealthCheckAsync(ct);

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<SecurityAuditLogDto>>> GetSecurityAuditLogsAsync(
        int page = 1,
        int pageSize = 20,
        string? eventType = null,
        string? userName = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
        => _authApi.GetSecurityAuditLogsAsync(
            page,
            pageSize,
            eventType,
            userName,
            from?.ToString("O"),
            to?.ToString("O"),
            ct);

    /// <inheritdoc />
    public Task<ApiResponse<PagedResult<UserListDto>>> GetUsersAsync(
        int page = 1, int pageSize = 20, string? keyword = null,
        CancellationToken ct = default)
        => _userApi.GetUsersAsync(page, pageSize, keyword, ct);

    /// <inheritdoc />
    public Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid id, CancellationToken ct = default)
        => _userApi.GetUserByIdAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<UserDetailDto>> CreateUserAsync(UserInputDto request, CancellationToken ct = default)
        => _userApi.CreateUserAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<UserDetailDto>> UpdateUserAsync(Guid id, UserInputDto request, CancellationToken ct = default)
        => _userApi.UpdateUserAsync(id, request, ct);

    /// <inheritdoc />
    public Task<ApiResponse> DeleteUserAsync(Guid id, CancellationToken ct = default)
        => _userApi.DeleteUserAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<UserDetailDto>> ChangeProfileAsync(Guid id, ChangeProfileDto request, CancellationToken ct = default)
        => _userApi.ChangeProfileAsync(id, request, ct);

    /// <inheritdoc />
    public Task<ApiResponse> ChangePasswordAsync(Guid id, ChangePasswordRequest request, CancellationToken ct = default)
        => _userApi.ChangePasswordAsync(id, request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default)
        => _userApi.ResetPasswordAsync(id, request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<UserDetailDto>> ToggleStatusAsync(Guid id, CancellationToken ct = default)
        => _userApi.ToggleStatusAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => _userApi.BatchDeleteAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<UserDetailDto>> RestoreAsync(Guid id, CancellationToken ct = default)
        => _userApi.RestoreAsync(id, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => _userApi.BatchEnableAsync(request, ct);

    /// <inheritdoc />
    public Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync(BatchDeleteInputDto request, CancellationToken ct = default)
        => _userApi.BatchDisableAsync(request, ct);

    /// <inheritdoc />
    /// <remarks>Local-only method — not available in remote Refit mode.</remarks>
    public Task<UserDetailDto> GetCurrentUserAsync(CancellationToken ct = default)
        => throw new NotSupportedException("GetCurrentUserAsync is a local-only method and is not available in remote mode.");
}
