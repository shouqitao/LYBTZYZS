using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.ViewModels.Handlers;

/// <summary>
/// 用户状态处理接口
/// OpenSpec: refactor-frontend-srp-patterns - Handler提取模式
/// </summary>
public interface IUserStatusHandler
{
    /// <summary>
    /// 切换用户状态
    /// </summary>
    /// <param name="user">用户信息</param>
    /// <returns>操作是否成功</returns>
    Task<bool> ToggleUserStatusAsync(UserListDto user);

    /// <summary>
    /// 恢复用户
    /// </summary>
    /// <param name="user">用户信息</param>
    /// <returns>操作是否成功</returns>
    Task<bool> RestoreAsync(UserListDto user);

    /// <summary>
    /// 是否可以切换状态
    /// </summary>
    /// <param name="user">用户信息</param>
    /// <param name="isBusy">是否忙碌</param>
    bool CanToggleUserStatus(UserListDto? user, bool isBusy);

    /// <summary>
    /// 是否可以恢复
    /// </summary>
    /// <param name="user">用户信息</param>
    /// <param name="isBusy">是否忙碌</param>
    /// <param name="isAdmin">是否管理员</param>
    bool CanRestore(UserListDto? user, bool isBusy, bool isAdmin);
}
