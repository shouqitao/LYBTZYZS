using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Primitives;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 系统管理员配置
/// </summary>
public sealed class SystemAdminOptions
{
    public const string SectionName = "SystemAdmin";

    /// <summary>
    /// 用户名
    /// </summary>
    [Required]
    public string UserName { get; set; } = UserConstants.SysAdminUsername;

    /// <summary>
    /// 邮箱
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "admin@lybt.com";

    /// <summary>
    /// 显示名称
    /// </summary>
    [Required]
    public string DisplayName { get; set; } = "系统管理员";

    /// <summary>
    /// 启动时自动创建
    /// </summary>
    public bool AutoCreateOnStartup { get; set; } = true;

    // ForceResetOnStartup 已移除（2026-08-13 生命周期回归——用户权威设计「无强制重置密码机制」；
    // 密码遗忘唯一途径 = PasswordHashGenerator 工具人工恢复）

    /// <summary>
    /// 是否在Production环境中允许自动创建系统管理员
    /// 默认：false（安全默认值）。设为true时需要配置InitialSetupToken
    /// </summary>
    public bool AllowAutoCreateInProduction { get; set; } = false;

    /// <summary>
    /// 在Production环境中创建系统管理员时需要的一次性设置令牌
    /// 应通过环境变量提供，永远不要提交到源代码
    /// </summary>
    public string? InitialSetupToken { get; set; }

    /// <summary>
    /// 会话超时时间 (分钟)
    /// </summary>
    [Range(30, 480)]
    public int SessionTimeoutMinutes { get; set; } = 240;
}
