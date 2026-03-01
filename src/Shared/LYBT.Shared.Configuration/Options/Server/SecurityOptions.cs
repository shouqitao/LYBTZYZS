using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Configuration.Constants;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 安全配置
/// </summary>
public sealed class SecurityOptions
{
    public const string SectionName = ConfigurationSections.Security;

    /// <summary>
    /// 速率限制配置
    /// </summary>
    public RateLimitingOptions RateLimiting { get; set; } = new();

    /// <summary>
    /// 审计日志保留天数 (CODE-29)
    /// </summary>
    [Range(30, 3650)]
    public int AuditRetentionDays { get; set; } = 365;
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
    public int InternalPermitLimit { get; set; } = 20;
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
    public int AdminPermitLimit { get; set; } = 200;
}
