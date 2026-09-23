// ---------------------------------------------------------------------------
// GitHubReleaseSource — 从 GitHub Releases 获取更新（Velopack IUpdateSource）
// ---------------------------------------------------------------------------
// 仓库主远端已切换为 GitHub（origin=git@github.com:shouqitao/LYBTZYZS.git，Gitee 为镜像），
// 故公网分发主渠道为 GitHub Releases。
//
// Velopack 1.2.0 内置 GithubSource（`{owner}/{repo}` → https://api.github.com/repos/{owner}/{repo}），
// 本类在既有 ReleaseSource 扩展点上做**显式登记**：与 GiteeReleaseSource 同为薄封装，
// 差别只在于「API 主机与 Gitee 不同」——因此**不覆写** GetApiBaseUrl/GetReleases/
// GetAssetUrlFromName，直接复用 GithubSource 的 GitHub 原生实现。
//
// GitHub 与 Gitee 的响应字段差异（Gitee OpenAPI v5 刻意对齐 GitHub，故同名居多）：
//   · 列表端点      GitHub: /releases?per_page=&page=（GithubSource 原生分页）
//                   Gitee : /releases?page=&limit=（GiteaSource 覆写了 GetReleases）
//   · tag 字段      GitHub: tag_name（与 Gitee 同名）
//   · 预发布字段    GitHub: prerelease（与 Gitee 同名）
//   · 资产列表      GitHub: assets[]（与 Gitee 同名）
//   · 资产下载地址  GitHub: assets[].browser_download_url
//                            = https://github.com/{owner}/{repo}/releases/download/{tag}/{name}
//                   Gitee : assets[].browser_download_url
//                            = https://gitee.com/{owner}/{repo}/releases/download/{tag}/{name}
//                   （assets[].url 是 API 资源地址而非下载地址，两者均不消费该字段）
// 资产命名约定与打包脚本一致：馈源清单 releases.{channel}.json + *-full.nupkg / *-delta.nupkg，
// 客户端先取清单资产再按清单 FileName 下载（见 docs/06-operations/12-desktop-release.md）。
// ---------------------------------------------------------------------------

using Velopack.Sources;

namespace LYBT.Desktop.Foundation.Services;

/// <summary>
/// GitHub Releases 更新源（基于 Velopack 内置 <see cref="GithubSource"/>，
/// API 根为 <c>https://api.github.com/repos/{owner}/{repo}</c>）。
/// </summary>
public sealed class GitHubReleaseSource : GithubSource
{
    /// <summary>GitHub REST API 主机（与仓库托管主机 <c>github.com</c> 不同，故不能按 Gitee 方式拼 {host}/api/vX/）。</summary>
    internal const string ApiHost = "api.github.com";

    /// <summary>
    /// 初始化 <see cref="GitHubReleaseSource"/> 类的新实例。
    /// </summary>
    /// <param name="repoUrl">GitHub 仓库地址，如 <c>https://github.com/owner/repo</c>。</param>
    /// <param name="accessToken">访问令牌（私有仓库必填，公开仓库可为 null）。</param>
    /// <param name="prerelease">是否接受预发布版本。</param>
    public GitHubReleaseSource(string repoUrl, string? accessToken, bool prerelease)
        : this(repoUrl, accessToken, prerelease, new HttpClientFileDownloader())
    {
    }

    /// <summary>
    /// 测试专用构造：注入自定义下载器（避免测试触网）。
    /// </summary>
    /// <param name="repoUrl">仓库地址。</param>
    /// <param name="accessToken">访问令牌。</param>
    /// <param name="prerelease">是否接受预发布版本。</param>
    /// <param name="downloader">文件下载器。</param>
    internal GitHubReleaseSource(string repoUrl, string? accessToken, bool prerelease, IFileDownloader downloader)
        : base(repoUrl, accessToken, prerelease, downloader)
    {
    }
}
