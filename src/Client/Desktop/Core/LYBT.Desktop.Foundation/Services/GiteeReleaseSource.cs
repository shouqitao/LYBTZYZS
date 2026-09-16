// ---------------------------------------------------------------------------
// GiteeReleaseSource — 从 Gitee Releases 获取更新（Velopack IUpdateSource）
// ---------------------------------------------------------------------------
// Velopack 1.2.0 内置 GithubSource / GiteaSource / GitlabSource / SimpleWebSource，
// **没有 Gitee 源**；而本仓库托管在 Gitee（gitee.com/shouqitao/LYBTZYZS），
// 公网分发走 Gitee Releases 是自然渠道。
//
// Gitee OpenAPI v5 的 releases 响应体与 GitHub/Gitea 同构
// （tag_name / body / prerelease / created_at / assets[{name,size,browser_download_url}]），
// 资产下载地址为 https://gitee.com/{owner}/{repo}/releases/download/{tag}/{name}，
// 与 GiteaSource 的默认拼装一致。因此本类只覆写 API 基地址：
//   Gitea  → {host}/api/v1/
//   Gitee  → {host}/api/v5/
// 其余（分页、资产筛选、同名资产的 delta 关联）复用 Velopack 既有实现。
//
// 说明：Gitee 已把「仓库是否公开」作为硬性约束——私有仓库的 releases 接口要求
// access_token（见 DesktopUpdateOptions.GiteeAccessToken）。客户端分发建议使用
// 公开仓库，或继续走自建 FeedUrl 静态目录（UpdateSourceKinds.Server）。
// ---------------------------------------------------------------------------

using Velopack.Sources;

namespace LYBT.Desktop.Foundation.Services;

/// <summary>
/// Gitee Releases 更新源（基于 GiteaSource 的 API 兼容性，仅替换 API 基地址为 Gitee OpenAPI v5）。
/// </summary>
public sealed class GiteeReleaseSource : GiteaSource
{
    /// <summary>Gitee OpenAPI 的 API 根路径。</summary>
    internal const string ApiRootPath = "/api/v5/";

    /// <summary>
    /// 初始化 <see cref="GiteeReleaseSource"/> 类的新实例。
    /// </summary>
    /// <param name="repoUrl">Gitee 仓库地址，如 <c>https://gitee.com/owner/repo</c>。</param>
    /// <param name="accessToken">访问令牌（私有仓库必填，公开仓库可为 null）。</param>
    /// <param name="prerelease">是否接受预发布版本。</param>
    public GiteeReleaseSource(string repoUrl, string? accessToken, bool prerelease)
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
    internal GiteeReleaseSource(string repoUrl, string? accessToken, bool prerelease, IFileDownloader downloader)
        : base(repoUrl, accessToken, prerelease, downloader)
    {
    }

    /// <summary>
    /// Gitee 的 API 根为 <c>{host}/api/v5/</c>（Gitea 默认为 <c>/api/v1/</c>）。
    /// </summary>
    /// <param name="repoUrl">仓库地址。</param>
    /// <returns>API 基地址。</returns>
    protected override Uri GetApiBaseUrl(Uri repoUrl) => new(repoUrl, ApiRootPath);
}
