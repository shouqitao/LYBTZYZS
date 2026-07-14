using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// LocalWebAPI JWT 认证配置
/// </summary>
public sealed class LocalJwtOptions
{
    public const string SectionName = "LocalJwt";

    /// <summary>
    /// JWT 签名密钥 (Base64 编码)
    /// </summary>
    [Required(ErrorMessage = "LocalJwt:SecretKey 不能为空")]
    [MinLength(32, ErrorMessage = "LocalJwt:SecretKey 长度不能小于 32 字符")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 令牌发行者
    /// </summary>
    [Required(ErrorMessage = "LocalJwt:Issuer 不能为空")]
    public string Issuer { get; set; } = "LYBT-LocalWebAPI";

    /// <summary>
    /// 令牌受众
    /// </summary>
    [Required(ErrorMessage = "LocalJwt:Audience 不能为空")]
    public string Audience { get; set; } = "LYBT-Desktop";

    /// <summary>
    /// 访问令牌过期时间 (分钟)
    /// </summary>
    [Range(1, 1440, ErrorMessage = "LocalJwt:ExpirationMinutes 必须在 1-1440 之间")]
    public int ExpirationMinutes { get; set; } = 60;
}
