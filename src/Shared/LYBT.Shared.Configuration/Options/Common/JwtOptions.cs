using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Configuration.Constants;

namespace LYBT.Shared.Configuration.Options.Common;

/// <summary>
/// JWT 认证配置
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = ConfigurationSections.Jwt;

    /// <summary>
    /// JWT 签名密钥 (Base64 编码)
    /// </summary>
    [Required(ErrorMessage = "JWT SecretKey 不能为空")]
    [MinLength(32, ErrorMessage = "JWT SecretKey 长度不能小于 32 字符")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 令牌发行者
    /// </summary>
    [Required]
    public string Issuer { get; set; } = "LYBT.WebAPI";

    /// <summary>
    /// 令牌受众
    /// </summary>
    [Required]
    public string Audience { get; set; } = "LYBT.Client";

    /// <summary>
    /// 访问令牌过期时间 (分钟)
    /// </summary>
    [Range(5, 1440, ErrorMessage = "AccessTokenExpirationMinutes 必须在 5-1440 之间")]
    public int AccessTokenExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// 刷新令牌过期时间 (天)
    /// </summary>
    [Range(1, 30, ErrorMessage = "RefreshTokenExpirationDays 必须在 1-30 之间")]
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// 时钟偏差容忍度 (秒)
    /// </summary>
    [Range(0, 600)]
    public int ClockSkewSeconds { get; set; } = 300;
}
