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
}
