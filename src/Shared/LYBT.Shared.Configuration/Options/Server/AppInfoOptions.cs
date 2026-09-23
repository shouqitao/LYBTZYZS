using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Primitives;

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
    /// 应用版本（默认取程序集元数据；单源 = Directory.Build.props 的 VersionPrefix）
    /// </summary>
    [Required]
    public string Version { get; set; } = AppVersion.Current;

    /// <summary>
    /// 运行环境
    /// </summary>
    public string Environment { get; set; } = "Development";
}
