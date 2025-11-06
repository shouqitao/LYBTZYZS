namespace LYBT.Module.Auth.Models;

/// <summary>
/// 安全审计事件模型
/// Issue #1871: 用于记录认证相关安全事件的传输对象
/// </summary>
public class SecurityAuditEvent
{
    /// <summary>
    /// 事件类型（Login, Logout, RefreshToken, TokenRevoked, LoginFailed等）
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID（可选）
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 用户类型（User或SuperAdmin，可选）
    /// </summary>
    public string? UserType { get; set; }

    /// <summary>
    /// 用户名（可选）
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息（失败时记录）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 元数据（JSON格式，可选）
    /// </summary>
    public string? Metadata { get; set; }
}
