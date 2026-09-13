namespace LYBT.Desktop.Shell.Services;

/// <summary>登出结果</summary>
public enum LogoutOutcome
{
    /// <summary>已完成登出</summary>
    LoggedOut,

    /// <summary>用户取消（或选择继续停留）——未登出</summary>
    Cancelled,

    /// <summary>登出过程异常——未完成登出（已记录日志）</summary>
    Failed,
}

/// <summary>
/// Shell 登出协调服务 — 全 Shell 唯一登出入口。
/// <para>统一「活跃医案离开守卫 → 确认 → 执行登出」流程：宿主（MainWindowViewModel，Alt+F4/菜单）与
/// 侧栏（SideNavViewModel，退出按钮）共用，避免任一路径绕过守卫导致进行中医案数据丢失。</para>
/// </summary>
public interface IShellLogoutService
{
    /// <summary>请求登出（含守卫与确认；不抛异常，结果以 <see cref="LogoutOutcome"/> 返回）</summary>
    Task<LogoutOutcome> RequestLogoutAsync();
}
