using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Auth;

/// <summary>
/// 会话信息DTO。用于返回认证会话的详情。
/// </summary>
public class SessionInfoDto
{
    /// <summary>会话ID</summary>
    [DisplayName("会话ID")]
    public Guid SessionId { get; set; }

    /// <summary>用户ID</summary>
    [DisplayName("用户ID")]
    public Guid UserId { get; set; }

    /// <summary>登录时间</summary>
    [DisplayName("登录时间")]
    public DateTime LoginTime { get; set; }

    /// <summary>过期时间</summary>
    [DisplayName("过期时间")]
    public DateTime ExpiryTime { get; set; }

    /// <summary>IP地址</summary>
    [DisplayName("IP地址")]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>用户代理</summary>
    [DisplayName("用户代理")]
    public string? UserAgent { get; set; }

    /// <summary>是否有效</summary>
    [DisplayName("是否有效")]
    public bool IsValid { get; set; }
}
