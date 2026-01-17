using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.ViewModels.Handlers;

/// <summary>
/// 用户审计日志处理接口
/// OpenSpec: refactor-frontend-srp-patterns - Handler提取模式
/// </summary>
public interface IUserAuditHandler
{
    /// <summary>
    /// 显示用户审计日志
    /// </summary>
    /// <param name="user">用户信息</param>
    void ShowAuditLog(UserListDto user);

    /// <summary>
    /// 是否可以查看审计日志
    /// </summary>
    /// <param name="user">用户信息</param>
    bool CanShowAuditLog(UserListDto? user);
}
