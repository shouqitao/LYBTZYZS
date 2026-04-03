using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// Swagger API 文档配置
/// </summary>
public sealed class SwaggerOptions
{
    public const string SectionName = "Swagger";

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

    /// <summary>
    /// 是否在生产环境启用
    /// </summary>
    public bool EnableInProduction { get; set; } = false;
}
