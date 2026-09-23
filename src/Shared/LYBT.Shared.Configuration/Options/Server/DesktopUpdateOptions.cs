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

    /// <summary>
    /// 更新源类型（见 <see cref="UpdateSourceKinds"/>）：<c>Server</c>（默认，<see cref="FeedUrl"/> 静态目录）、
    /// <c>Gitee</c>（Gitee Releases，需配 <see cref="GiteeRepoUrl"/>）或
    /// <c>GitHub</c>（GitHub Releases，需配 <see cref="GitHubRepoUrl"/>）。
    /// </summary>
    public string SourceKind { get; set; } = UpdateSourceKinds.Server;

    /// <summary>
    /// Gitee 仓库地址（<see cref="UpdateSourceKinds.Gitee"/> 时必填），如 https://gitee.com/owner/repo
    /// </summary>
    public string? GiteeRepoUrl { get; set; }

    /// <summary>
    /// Gitee 访问令牌（私有仓库必填——Gitee OpenAPI /releases 对私有仓库要求 access_token）
    /// </summary>
    public string? GiteeAccessToken { get; set; }

    /// <summary>
    /// 是否接受 Gitee 上的预发布（prerelease）版本
    /// </summary>
    public bool GiteePrerelease { get; set; }

    /// <summary>
    /// GitHub 仓库地址（<see cref="UpdateSourceKinds.GitHub"/> 时必填），如 https://github.com/owner/repo
    /// </summary>
    public string? GitHubRepoUrl { get; set; }

    /// <summary>
    /// GitHub 访问令牌（私有仓库必填；公开仓库可空——可空时匿名访问，受匿名速率限制 60 次/小时/IP）
    /// </summary>
    public string? GitHubToken { get; set; }

    /// <summary>
    /// 是否接受 GitHub 上的预发布（prerelease）版本
    /// </summary>
    public bool GitHubPrerelease { get; set; }
}

/// <summary>
/// <see cref="DesktopUpdateOptions.SourceKind"/> 取值。
/// </summary>
public static class UpdateSourceKinds
{
    /// <summary>静态目录更新源（<see cref="DesktopUpdateOptions.FeedUrl"/> 指向 /releases/ 目录）</summary>
    public const string Server = "Server";

    /// <summary>Gitee Releases 更新源（可选镜像渠道）</summary>
    public const string Gitee = "Gitee";

    /// <summary>GitHub Releases 更新源（主渠道——仓库 origin 为 GitHub）</summary>
    public const string GitHub = "GitHub";
}
