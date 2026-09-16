// ---------------------------------------------------------------------------
// IUserManagementApiClient — 用户管理 API 窄接口（T5.2 ISP：从 IApiClientIdentity 拆出）
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// 用户管理 API 子接口（T5.2 从 IApiClientIdentity 拆出，仅用户 CRUD/批量端点）。
/// </summary>
public interface IUserManagementApiClient : IEntityApiSegment<UserListDto, UserDetailDto, UserInputDto>
{
    /// <summary>分页获取用户列表。</summary>
    Task<ApiResponse<PagedResult<UserListDto>>> GetUsersAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default);

    /// <summary>按 ID 获取用户详情。</summary>
    Task<ApiResponse<UserDetailDto>> GetUserByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>创建新用户。</summary>
    Task<ApiResponse<UserDetailDto>> CreateUserAsync(UserInputDto request, CancellationToken ct = default);

    /// <summary>更新现有用户。</summary>
    Task<ApiResponse<UserDetailDto>> UpdateUserAsync(Guid id, UserInputDto request, CancellationToken ct = default);

    /// <summary>删除用户（软删除）。</summary>
    Task<ApiResponse> DeleteUserAsync(Guid id, CancellationToken ct = default);

    /// <summary>修改用户个人资料。</summary>
    Task<ApiResponse<UserDetailDto>> ChangeProfileAsync(Guid id, ChangeProfileDto request, CancellationToken ct = default);

    /// <summary>修改用户密码。</summary>
    Task<ApiResponse> ChangePasswordAsync(Guid id, ChangePasswordRequest request, CancellationToken ct = default);

    /// <summary>管理员重置用户密码。</summary>
    Task<ApiResponse<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken ct = default);

    /// <summary>切换用户状态（启用/禁用）。</summary>
    Task<ApiResponse<UserDetailDto>> ToggleStatusAsync(Guid id, CancellationToken ct = default);
}
