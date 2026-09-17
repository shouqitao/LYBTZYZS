using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 安全配置
/// </summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>系统审计用户 ID（非 HTTP 上下文如种子/后台任务归属）</summary>
    public Guid SystemUserId { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>患者敏感字段 AES-GCM 密钥（Base64 32 字节，P1-9）</summary>
    public string? AesKey { get; set; }

    /// <summary>
    /// 速率限制配置
    /// </summary>
    public RateLimitingOptions RateLimiting { get; set; } = new();

    /// <summary>
    /// 审计日志保留天数 (CODE-29)
    /// </summary>
    [Range(30, 3650)]
    public int AuditRetentionDays { get; set; } = 365;

    /// <summary>
    /// 账户锁定配置
    /// </summary>
    public AccountLockoutOptions AccountLockout { get; set; } = new();
}

/// <summary>
/// 速率限制配置
/// </summary>
public sealed class RateLimitingOptions
{
    public bool Enabled { get; set; } = true;
    public RateLimitOptions GlobalLimit { get; set; } = new() { PermitLimit = 200 };
    public LoginRateLimitOptions LoginLimit { get; set; } = new();
    public ApiRateLimitOptions ApiLimit { get; set; } = new();
    public List<string> WhitelistedIPs { get; set; } = ["127.0.0.1", "::1"];
}

/// <summary>
/// 速率限制基础配置
/// </summary>
public class RateLimitOptions
{
    [Range(1, 10000)]
    public int PermitLimit { get; set; } = 100;

    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;

    [Range(0, 100)]
    public int QueueLimit { get; set; } = 0;
}

/// <summary>
/// 登录速率限制配置
/// </summary>
public sealed class LoginRateLimitOptions : RateLimitOptions
{
    /// <summary>
    /// 内网/可信源登录限流阈值（预留：当前 ConfigureRateLimiting 仅消费 PermitLimit，
    /// 未按 IP 分区内网放宽；启用分区策略时再接入）
    /// </summary>
    public int InternalPermitLimit { get; set; } = 20;

    /// <summary>
    /// 内网登录排队上限（预留，同 InternalPermitLimit）
    /// </summary>
    public int InternalQueueLimit { get; set; } = 0;

    public LoginRateLimitOptions()
    {
        PermitLimit = 5;
    }
}

/// <summary>
/// API 速率限制配置
/// </summary>
public sealed class ApiRateLimitOptions : RateLimitOptions
{
    /// <summary>
    /// 管理员角色 API 限流阈值（预留：当前策略按 IP 分区，未按角色区分；
    /// 启用角色分区策略时再接入）
    /// </summary>
    public int AdminPermitLimit { get; set; } = 200;
}

/// <summary>
/// 账户锁定配置
/// </summary>
public sealed class AccountLockoutOptions
{
    /// <summary>
    /// 是否启用账户锁定（测试/开发环境建议设为 false）
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 最大失败登录次数（达到后锁定账户）
    /// </summary>
    [Range(1, 100)]
    public int MaxFailedCount { get; set; } = 5;

    /// <summary>
    /// 账户锁定时间（分钟）
    /// </summary>
    [Range(1, 1440)]
    public int LockoutMinutes { get; set; } = 15;
}
