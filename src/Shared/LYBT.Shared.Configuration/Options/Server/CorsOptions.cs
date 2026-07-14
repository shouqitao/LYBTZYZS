using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// CORS 跨域配置
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// 允许的来源域名
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 是否允许携带凭证
    /// </summary>
    public bool AllowCredentials { get; set; }

    /// <summary>
    /// 允许的 HTTP 方法
    /// </summary>
    public string[] AllowedMethods { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 允许的请求头
    /// </summary>
    public string[] AllowedHeaders { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 预检请求缓存时间 (秒)
    /// </summary>
    [Range(0, 86400)]
    public int PreflightMaxAgeSeconds { get; set; } = 86400;
}
