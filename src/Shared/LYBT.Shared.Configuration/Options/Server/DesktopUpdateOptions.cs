using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// Desktop 自动更新配置
/// </summary>
public sealed class DesktopUpdateOptions
{
    public const string SectionName = "DesktopUpdate";

    /// <summary>
    /// 是否启用自动更新
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 检查更新间隔 (分钟)
    /// </summary>
    [Range(1, 1440, ErrorMessage = "DesktopUpdate:CheckIntervalMinutes 必须在 1-1440 之间")]
    public int CheckIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// 下载基础 URL
    /// </summary>
    public string? DownloadBaseUrl { get; set; }

    /// <summary>
    /// Velopack 更新源绝对地址（US-SHELL-010: 如 https://server/releases——UpdateManager 消费）
    /// </summary>
    public string? FeedUrl { get; set; }
}
