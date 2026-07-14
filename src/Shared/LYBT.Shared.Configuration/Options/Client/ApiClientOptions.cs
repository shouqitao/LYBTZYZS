using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Configuration.Options.Client;

/// <summary>
/// API 客户端配置
/// </summary>
/// <summary>
/// API 客户端配置
/// </summary>
public sealed class ApiClientOptions
{
    public const string SectionName = "ApiClient";

    /// <summary>
    /// API 基础地址
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://localhost:5001/";

    /// <summary>
    /// 远程服务器地址
    /// </summary>
    public string RemoteUrl { get; set; } = string.Empty;

    /// <summary>
    /// 首选连接模式: "Local" 或 "Remote"
    /// </summary>
    public string PreferredMode { get; set; } = "Local";

    /// <summary>
    /// 请求超时时间 (秒)
    /// </summary>
    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// 忽略 SSL 错误 (仅开发环境)
    /// </summary>
    public bool IgnoreSslErrors { get; set; } = false;
}
