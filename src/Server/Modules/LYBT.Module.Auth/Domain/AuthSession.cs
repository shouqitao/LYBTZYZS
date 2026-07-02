using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;
using LYBT.SharedKernel.Primitives;

namespace LYBT.Module.Auth.Domain;

/// <summary>
/// 认证会话聚合根。管理用户登录会话的生命周期。
/// </summary>
public class AuthSession : Entity, IAggregateRoot
{
    /// <summary>用户ID</summary>
    public Guid UserId { get; private set; }

    /// <summary>会话令牌哈希</summary>
    [StringLength(256)]
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>登录时间 (UTC)</summary>
    public DateTime LoginTime { get; private set; }

    /// <summary>登出时间 (UTC)</summary>
    public DateTime? LogoutTime { get; private set; }

    /// <summary>过期时间 (UTC)</summary>
    public DateTime ExpiryTime { get; private set; }

    /// <summary>IP地址</summary>
    [StringLength(45)]
    public string IpAddress { get; private set; } = string.Empty;

    /// <summary>用户代理</summary>
    [StringLength(500)]
    public string? UserAgent { get; private set; }

    /// <summary>是否已撤销</summary>
    public bool IsRevoked { get; private set; }

    /// <summary>会话状态</summary>
    public CommonStatus Status { get; private set; } = CommonStatus.Enabled;

    private AuthSession() { }

    /// <summary>
    /// 创建新的认证会话。
    /// </summary>
    public static AuthSession Create(
        Guid userId,
        string tokenHash,
        DateTime expiryTime,
        string ipAddress,
        string? userAgent = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("令牌哈希不能为空", nameof(tokenHash));
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP地址不能为空", nameof(ipAddress));

        return new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            LoginTime = DateTime.UtcNow,
            ExpiryTime = expiryTime,
            IpAddress = ipAddress.Trim(),
            UserAgent = userAgent?.Trim(),
            Status = CommonStatus.Enabled
        };
    }

    /// <summary>
    /// 登出会话。
    /// </summary>
    public void Logout()
    {
        if (IsRevoked)
            throw new InvalidOperationException("会话已被撤销");

        LogoutTime = DateTime.UtcNow;
        Status = CommonStatus.Disabled;
    }

    /// <summary>
    /// 撤销会话（强制登出）。
    /// </summary>
    public void Revoke()
    {
        IsRevoked = true;
        LogoutTime = DateTime.UtcNow;
        Status = CommonStatus.Disabled;
    }

    /// <summary>
    /// 检查会话是否有效。
    /// </summary>
    public bool IsValid()
    {
        return !IsRevoked
            && Status == CommonStatus.Enabled
            && LogoutTime == null
            && DateTime.UtcNow < ExpiryTime;
    }

    /// <summary>
    /// 检查会话是否已过期。
    /// </summary>
    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiryTime;
    }
}


