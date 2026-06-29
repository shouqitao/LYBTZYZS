using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Users.Interfaces;

/// <summary>
/// 用户管理服务接口
/// 供 WebAPI 和 LocalWebAPI 共享，减少 Controller 层业务逻辑
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 分页查询用户列表
    /// </summary>
    Task<PagedResult<UserListDto>> GetPagedUsersAsync(
        int page, int pageSize, string? keyword,
        UserRole? role, CommonStatus? status,
        CancellationToken ct = default);

    /// <summary>
    /// 根据 ID 获取用户详情
    /// </summary>
    Task<UserDetailDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    Task<UserDetailDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// 创建用户
    /// </summary>
    Task<(bool Success, UserDetailDto? User, string? Error)> CreateUserAsync(
        UserInputDto dto, Guid currentUserId, bool isAdmin, CancellationToken ct = default);

    /// <summary>
    /// 更新用户
    /// </summary>
    Task<(bool Success, UserDetailDto? User, string? Error)> UpdateUserAsync(
        Guid id, UserInputDto dto, Guid currentUserId, bool isAdmin, CancellationToken ct = default);

    /// <summary>
    /// 删除用户
    /// </summary>
    Task<(bool Success, string? Error)> DeleteUserAsync(
        Guid id, Guid currentUserId, bool isAdmin, CancellationToken ct = default);

    /// <summary>
    /// 重置用户密码
    /// </summary>
    Task<(bool Success, string? TemporaryPassword, string? Error)> ResetPasswordAsync(
        Guid id, CancellationToken ct = default);

    /// <summary>
    /// 修改个人资料
    /// </summary>
    Task<(bool Success, UserDetailDto? User, string? Error)> ChangeProfileAsync(
        Guid id, ChangeProfileDto dto, Guid currentUserId, CancellationToken ct = default);

    /// <summary>
    /// 修改密码
    /// </summary>
    Task<(bool Success, string? Error)> ChangePasswordAsync(
        Guid id, string oldPassword, string newPassword, Guid currentUserId, CancellationToken ct = default);

    /// <summary>
    /// 切换用户状态
    /// </summary>
    Task<(bool Success, UserDetailDto? User, string? Error)> ToggleUserStatusAsync(
        Guid id, Guid currentUserId, bool isAdmin, CancellationToken ct = default);

    /// <summary>
    /// 批量删除用户
    /// </summary>
    Task<BatchOperationResultDto> BatchDeleteUsersAsync(
        List<Guid> ids, Guid currentUserId, bool isAdmin, CancellationToken ct = default);
}
