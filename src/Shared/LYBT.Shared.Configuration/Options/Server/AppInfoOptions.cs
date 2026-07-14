using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 应用基本信息配置
/// </summary>
public sealed class AppInfoOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// 应用名称
    /// </summary>
    [Required]
    public string Name { get; set; } = "LYBTZYZS";

    /// <summary>
    /// 应用版本
    /// </summary>
    [Required]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// 运行环境
    /// </summary>
    public string Environment { get; set; } = "Development";
}
