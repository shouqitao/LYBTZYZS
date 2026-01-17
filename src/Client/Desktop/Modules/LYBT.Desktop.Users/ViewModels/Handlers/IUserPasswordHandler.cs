using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.ViewModels.Handlers;

/// <summary>
/// 用户密码处理接口
/// OpenSpec: refactor-frontend-srp-patterns - Handler提取模式
/// </summary>
public interface IUserPasswordHandler
{
    /// <summary>
    /// 重置用户密码
    /// </summary>
    /// <param name="user">用户信息</param>
    Task ResetPasswordAsync(UserListDto user);

    /// <summary>
    /// 是否可以重置密码
    /// </summary>
    /// <param name="user">用户信息</param>
    /// <param name="isBusy">是否忙碌</param>
    bool CanResetPassword(UserListDto? user, bool isBusy);
}
