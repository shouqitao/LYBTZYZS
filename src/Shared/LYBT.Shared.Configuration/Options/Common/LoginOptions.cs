using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Configuration.Options.Common;

/// <summary>
/// 登录流程选项（A-31-C3a：Local 登录统一走 LoginCommandHandler，差异由此控制）。
/// </summary>
public sealed class LoginOptions
{
    public const string SectionName = "Login";

    /// <summary>是否本地模式（LocalWebAPI）。本地为嵌入式桌面服务，行为差异由此区分。</summary>
    public bool IsLocal { get; set; }

    /// <summary>是否启用账户锁定（本地默认关闭，Remote 跟随 Security:AccountLockout）。</summary>
    public bool LockoutEnabled { get; set; } = true;

    /// <summary>安全审计级别（None=不写审计，Minimal=仅失败事件，Full=全部）。</summary>
    public SecurityAuditLevel AuditLevel { get; set; } = SecurityAuditLevel.Full;
}

/// <summary>
/// 安全审计级别
/// </summary>
public enum SecurityAuditLevel
{
    /// <summary>不记录安全审计</summary>
    None = 0,

    /// <summary>仅记录失败事件（登录失败/令牌被拒）</summary>
    Minimal = 1,

    /// <summary>记录全部安全事件</summary>
    Full = 2,
}
