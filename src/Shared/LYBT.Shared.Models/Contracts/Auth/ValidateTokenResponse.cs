using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Auth;

/// <summary>
/// Token验证响应 - Issue #1824
/// 用于返回Token验证结果和用户信息
/// </summary>
public class ValidateTokenResponse
{
    /// <summary>
    /// Token是否有效
    /// </summary>
    [DisplayName("Token有效性")]
    public bool IsValid { get; set; }

    /// <summary>
    /// 用户ID（验证成功时返回）
    /// </summary>
    [DisplayName("用户ID")]
    public int? UserId { get; set; }

    /// <summary>
    /// 用户名（验证成功时返回）
    /// </summary>
    [DisplayName("用户名")]
    public string? Username { get; set; }

    /// <summary>
    /// 用户角色（验证成功时返回）
    /// </summary>
    [DisplayName("角色")]
    public string? Role { get; set; }

    /// <summary>
    /// Token过期时间（验证成功时返回）
    /// </summary>
    [DisplayName("过期时间")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 错误消息（验证失败时返回）
    /// </summary>
    [DisplayName("错误消息")]
    public string? ErrorMessage { get; set; }
}
