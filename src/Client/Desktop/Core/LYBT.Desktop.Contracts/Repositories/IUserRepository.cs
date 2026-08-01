using LYBT.Desktop.Contracts.Results;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Contracts.Repositories;

/// <summary>
/// 用户数据仓储接口
/// List 返回轻量 ListDto，Detail 返回完整 DetailDto。
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 分页查询用户列表 (返回轻量级 ListDto)
    /// </summary>
    Task<PagedResult<UserListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default);

    /// <summary>
    /// 根据 ID 获取用户详情 (返回完整 DetailDto)
    /// </summary>
    Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 创建新用户
    /// </summary>
    Task<UserDetailDto> CreateAsync(UserInputDto user, CancellationToken ct = default);

    /// <summary>
    /// 更新用户信息
    /// </summary>
    Task<UserDetailDto> UpdateAsync(UserInputDto user, CancellationToken ct = default);

    /// <summary>
    /// 删除用户 (软删除)
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<UserDetailDto> GetByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>
    /// 搜索用户 (基于关键词，返回 ListDto)
    /// </summary>
    Task<List<UserListDto>> SearchAsync(string keyword, CancellationToken ct = default);

    /// <summary>
    /// 获取所有医生用户
    /// </summary>
    Task<List<UserListDto>> GetDoctorsAsync(CancellationToken ct = default);

    /// <summary>
    /// 修改个人资料 (Issue #1891)
    /// </summary>
    Task<UserDetailDto> ChangeProfileAsync(Guid userId, ChangeProfileDto dto, CancellationToken ct = default);

    /// <summary>
    /// 修改密码 (Issue #1887-1892)
    /// </summary>
    Task<CommandResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);

    /// <summary>
    /// 管理员重置用户密码 (Issue #1911)
    /// </summary>
    Task<CommandResult<ResetPasswordResponseDto>> ResetPasswordAsync(Guid userId, ResetPasswordRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// 切换用户状态 (启用/禁用)
    /// </summary>
    Task<UserDetailDto?> ToggleStatusAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 批量删除用户
    /// </summary>
    Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default);
}
