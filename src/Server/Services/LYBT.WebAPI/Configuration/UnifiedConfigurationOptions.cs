using System.ComponentModel.DataAnnotations;

namespace LYBT.WebAPI.Configuration;

/// <summary>
/// WebAPI 统一配置选项（集中化绑定，替代分散的 configuration["key"] 读取）
/// </summary>
public class WebApiConfigurationOptions
{
    public const string SectionName = "WebApiOptions";

    /// <summary>
    /// 性能优化选项
    /// </summary>
    public PerformanceOptions Performance { get; set; } = new();

    /// <summary>
    /// Swagger 文档选项
    /// </summary>
    public SwaggerOptions Swagger { get; set; } = new();

    /// <summary>
    /// JSON 编码选项
    /// </summary>
    public JsonOptions Json { get; set; } = new();
}

/// <summary>
/// 性能优化选项
/// </summary>
public class PerformanceOptions
{
    /// <summary>
    /// 最小工作线程数
    /// </summary>
    [Range(1, 1000)]
    public int MinWorkerThreads { get; set; } = 50;

    /// <summary>
    /// 最小 IO 线程数
    /// </summary>
    [Range(1, 1000)]
    public int MinIoThreads { get; set; } = 50;

    /// <summary>
    /// 最大并发连接数
    /// </summary>
    [Range(1, 10000)]
    public int MaxConcurrentConnections { get; set; } = 100;

    /// <summary>
    /// 请求体最大字节数
    /// </summary>
    [Range(1024, 100 * 1024 * 1024)] // 1KB ~ 100MB
    public long MaxRequestBodySize { get; set; } = 30 * 1024 * 1024; // 30MB

    /// <summary>
    /// 响应缓存最大字节数
    /// </summary>
    [Range(1024, 100 * 1024 * 1024)]
    public long ResponseCacheMaxBodySize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// 内存缓存大小上限
    /// </summary>
    [Range(1000, 1000000)]
    public long MemoryCacheSizeLimit { get; set; } = 100_000;
}

/// <summary>
/// Swagger 文档选项
/// </summary>
public class SwaggerOptions
{
    /// <summary>
    /// API 文档标题
    /// </summary>
    [Required]
    public string Title { get; set; } = "凌隐宝堂中医诊所 API";

    /// <summary>
    /// API 文档描述
    /// </summary>
    [Required]
    public string Description { get; set; } = "凌隐宝堂中医诊所 RESTful API 接口文档";

    /// <summary>
    /// 联系人姓名
    /// </summary>
    [Required]
    public string ContactName { get; set; } = "技术支持";

    /// <summary>
    /// 联系邮箱
    /// </summary>
    [EmailAddress]
    public string ContactEmail { get; set; } = "support@lybt.com";

    /// <summary>
    /// 联系 URL
    /// </summary>
    [Url]
    public string ContactUrl { get; set; } = "https://lybt.com/support";

    /// <summary>
    /// 许可证名称
    /// </summary>
    [Required]
    public string LicenseName { get; set; } = "专有许可";

    /// <summary>
    /// 许可证 URL
    /// </summary>
    [Url]
    public string LicenseUrl { get; set; } = "https://lybt.com/license";

    /// <summary>
    /// 是否启用 XML 注释
    /// </summary>
    public bool EnableXmlComments { get; set; } = true;

    /// <summary>
    /// 路由前缀
    /// </summary>
    public string RoutePrefix { get; set; } = "swagger";

    /// <summary>
    /// 文档页标题
    /// </summary>
    public string DocumentTitle { get; set; } = "凌隐宝堂中医诊所 API 文档";
}

/// <summary>
/// JSON 编码处理选项
/// </summary>
public class JsonOptions
{
    /// <summary>
    /// 是否使用 UnsafeRelaxedJsonEscaping（默认关闭，更安全）
    /// </summary>
    public bool UnsafeRelaxedEscaping { get; set; } = false;
}

