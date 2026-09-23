// ---------------------------------------------------------------------------
// IUpdateSourceFactory / UpdateSourceFactory — 依据配置选择 Velopack 更新源
// ---------------------------------------------------------------------------
// 抽出工厂的目的：① 让「源选择」可被单测覆盖（不必联网）；② DesktopUpdateService
// 不直接 new 具体源，便于后续新增渠道（自建静态目录 / GitHub / Gitee / 其它托管）。
// ---------------------------------------------------------------------------

using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Logging;
using Velopack.Sources;

namespace LYBT.Desktop.Foundation.Services;

/// <summary>
/// 依据 <see cref="DesktopUpdateOptions"/> 构造 Velopack 更新源。
/// </summary>
public interface IUpdateSourceFactory
{
    /// <summary>
    /// 创建更新源；配置不完整（缺少对应地址）时返回 null（调用方按「未启用」处理）。
    /// </summary>
    /// <param name="options">自动更新配置。</param>
    /// <returns>更新源，或 null。</returns>
    IUpdateSource? Create(DesktopUpdateOptions options);
}

/// <inheritdoc />
public sealed class UpdateSourceFactory : IUpdateSourceFactory
{
    private readonly ILogger<UpdateSourceFactory> _logger;

    /// <summary>
    /// 初始化 <see cref="UpdateSourceFactory"/> 类的新实例。
    /// </summary>
    /// <param name="logger">日志器。</param>
    public UpdateSourceFactory(ILogger<UpdateSourceFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IUpdateSource? Create(DesktopUpdateOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.Equals(options.SourceKind, UpdateSourceKinds.Gitee, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.GiteeRepoUrl))
            {
                _logger.LogWarning(
                    "[UPDATE] SourceKind=Gitee 但未配置 DesktopUpdate:GiteeRepoUrl——自动更新停用");
                return null;
            }

            // 私有仓库必须带 access_token，否则 Gitee /releases 返回 Not Found Project
            if (string.IsNullOrWhiteSpace(options.GiteeAccessToken))
            {
                _logger.LogInformation(
                    "[UPDATE] 未配置 DesktopUpdate:GiteeAccessToken——按公开仓库访问 Gitee Releases");
            }

            return new GiteeReleaseSource(options.GiteeRepoUrl, options.GiteeAccessToken, options.GiteePrerelease);
        }

        if (string.Equals(options.SourceKind, UpdateSourceKinds.GitHub, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.GitHubRepoUrl))
            {
                _logger.LogWarning(
                    "[UPDATE] SourceKind=GitHub 但未配置 DesktopUpdate:GitHubRepoUrl——自动更新停用");
                return null;
            }

            // 公开仓库可匿名访问（受匿名速率限制）；私有仓库必须带 token
            if (string.IsNullOrWhiteSpace(options.GitHubToken))
            {
                _logger.LogInformation(
                    "[UPDATE] 未配置 DesktopUpdate:GitHubToken——按公开仓库匿名访问 GitHub Releases");
            }

            return new GitHubReleaseSource(options.GitHubRepoUrl, options.GitHubToken, options.GitHubPrerelease);
        }

        if (string.IsNullOrWhiteSpace(options.FeedUrl))
        {
            _logger.LogWarning(
                "[UPDATE] SourceKind=Server 但未配置 DesktopUpdate:FeedUrl——自动更新停用");
            return null;
        }

        return new SimpleWebSource(options.FeedUrl, new HttpClientFileDownloader());
    }
}
