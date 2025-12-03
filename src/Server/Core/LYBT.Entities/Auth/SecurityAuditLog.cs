using System.ComponentModel.DataAnnotations;

namespace LYBT.Entities.Auth;

/// <summary>
/// 安全审计日志实体
/// 记录所有认证相关的安全事件（登录、登出、Token刷新、Token撤销等）
/// </summary>
public class SecurityAuditLog
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// 事件类型（Login, Logout, RefreshToken, TokenRevoked, LoginFailed等）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID（可选，部分事件可能无用户信息）
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 用户类型（User或SuperAdmin）
    /// </summary>
    [StringLength(50)]
    public string? UserType { get; set; }

    /// <summary>
    /// 用户名称
    /// </summary>
    [StringLength(256)]
    public string? UserName { get; set; }

    /// <summary>
    /// 客户端IP地址
    /// </summary>
    [StringLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 客户端User-Agent
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息（失败时记录）
    /// </summary>
    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 扩展元数据（JSON格式，存储额外信息）
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// 创建时间（UTC）
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
