using LYBT.Entities.Users;

namespace LYBT.Desktop.Contracts.DataSources;

/// <summary>
/// 用户数据源接口
/// OpenSpec: implement-local-mode
/// </summary>
public interface IUserDataSource : IDataSourceBase<User>
{
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="oldPasswordHash">旧密码哈希（用于验证）</param>
    /// <param name="newPasswordHash">新密码哈希</param>
    /// <param name="ct">取消令牌</param>
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
}
