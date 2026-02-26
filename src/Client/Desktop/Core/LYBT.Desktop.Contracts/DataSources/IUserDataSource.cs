using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Contracts.DataSources;

/// <summary>
/// 用户数据源接口
/// </summary>
public interface IUserDataSource : IDataSourceBase<UserDetailDto, UserInputDto>
{
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<UserDetailDto?> GetByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>
    /// 修改密码
    /// </summary>
    Task<bool> ChangePasswordAsync(Guid id, string oldPasswordHash, string newPasswordHash, CancellationToken ct = default);

    /// <summary>
    /// 切换用户状态（启用/禁用）
    /// </summary>
    Task<bool> ToggleStatusAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 更新最后登录时间
    /// </summary>
    Task<bool> UpdateLastLoginTimeAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 重置失败登录次数
    /// </summary>
    Task<bool> ResetFailedLoginCountAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 增加失败登录次数
    /// </summary>
    Task<int> IncrementFailedLoginCountAsync(Guid id, CancellationToken ct = default);

    // ==================== Sprint 4 X2 扩展方法 ====================
    // OpenSpec: SYNC-D02 - 过渡态方法，待 SYNC-D02 完成后统一重构

    /// <summary>
    /// T4-X2-03: 恢复已删除的用户
    /// </summary>
    Task<UserDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// T4-X2-04: 批量删除用户（含保护检查）
    /// </summary>
    Task<BatchOperationResultDto> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default);

    /// <summary>
    /// T4-X2-05: 管理员重置用户密码为默认密码
    /// </summary>
    Task<ResetPasswordResponseDto> ResetPasswordAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// T4-X2-07: 批量切换用户状态（启用/禁用）
    /// </summary>
    Task<BatchOperationResultDto> BatchToggleStatusAsync(List<Guid> ids, bool enable, CancellationToken ct = default);

    /// <summary>
    /// T4-X2-08: 获取当前登录用户信息
    /// </summary>
    Task<UserDetailDto?> GetCurrentUserAsync(CancellationToken ct = default);
}
