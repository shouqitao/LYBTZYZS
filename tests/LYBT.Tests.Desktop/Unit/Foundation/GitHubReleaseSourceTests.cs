using FluentAssertions;
using LYBT.Desktop.Foundation.Services;
using NSubstitute;
using Velopack.Logging;
using Velopack.Sources;

namespace LYBT.Tests.Desktop.Unit.Foundation;

/// <summary>
/// GitHub Releases 更新源单测（US-SHELL-010/012：公网分发主渠道为 GitHub Releases，
/// 仓库 origin 已切至 github.com/shouqitao/LYBTZYZS，Gitee 为镜像）。
/// </summary>
/// <remarks>
/// <para>锁定的运行时契约（由 Velopack 1.2.0 的 GithubSource/GitBase 行为决定，非本仓约定）：</para>
/// <list type="number">
///   <item>API 根为 <c>https://api.github.com/repos/{owner}/{repo}</c>——GitHub 的 API 主机与
///         托管主机 <c>github.com</c> 不同（Gitee 走 <c>{host}/api/v5/</c>，两者不可互相套用）。</item>
///   <item>馈源**不**直接读 <c>*-full.nupkg</c>，而是要求 release 附一个名为
///         <c>releases.{channel}.json</c> 的资产（即 <c>vpk pack</c> 产出的馈源清单），
///         再依清单里的 <c>FileName</c> 逐个下载包。</item>
///   <item>资产下载地址取 <c>assets[].browser_download_url</c>（GitHub 与 Gitee 同名字段）；
///         <c>assets[].url</c> 是 API 资源地址，不用于下载。</item>
/// </list>
/// <para>测试用注入的 <see cref="IFileDownloader"/> 分别回放「releases 列表」与「清单」两种响应，
/// 全程不触网。</para>
/// </remarks>
public class GitHubReleaseSourceTests
{
    private const string RepoUrl = "https://github.com/owner/repo";
    private const string Channel = "win";

    private sealed class NullVelopackLogger : IVelopackLogger
    {
        public void Log(VelopackLogLevel logLevel, string? message, Exception? exception = null) { }
    }

    /// <summary>vpk pack 产出的馈源清单（releases.win.json）样本。</summary>
    private const string ManifestJson = """
    {"Assets":[
      {"PackageId":"LYBTZYZS","Version":"0.0.2","Type":"Full","FileName":"LYBTZYZS-0.0.2-full.nupkg","SHA1":"FBC08263C0E51261F368443E85C34C348C459DF6","SHA256":"F79AAD9D7219CCB695888A8ACB991FBBE2018013B6220E2B737BBEB52F6E72A3","Size":123958638},
      {"PackageId":"LYBTZYZS","Version":"0.0.2","Type":"Delta","FileName":"LYBTZYZS-0.0.2-delta.nupkg","SHA1":"2467B5026FE336D6DE0A5F894849D3E2BDC771BA","SHA256":"37F3BBE5EE1004D5E52CAD6AC8B01A984029D169A2825E28B6BFF3E14EC7424D","Size":1574253},
      {"PackageId":"LYBTZYZS","Version":"0.0.1","Type":"Full","FileName":"LYBTZYZS-0.0.1-full.nupkg","SHA1":"070FF02FE23DB0E73272BE13BCEBB36B984AD9FA","SHA256":"2C53C55E25036A3D832C7720C157A6ABB83A47DD6701DE5CFD7D0D398C6FBC32","Size":123958482}
    ]}
    """;

    /// <summary>GitHub REST API 的 releases 列表响应样本（字段与 Gitee 同名，但含 GitHub 专有字段）。</summary>
    private static string ReleasesListJson(bool prerelease) => $$"""
    [
      {
        "url": "https://api.github.com/repos/owner/repo/releases/{{(prerelease ? 1003 : 1002)}}",
        "html_url": "https://github.com/owner/repo/releases/tag/{{(prerelease ? "v0.0.3-rc1" : "v0.0.2")}}",
        "assets_url": "https://api.github.com/repos/owner/repo/releases/{{(prerelease ? 1003 : 1002)}}/assets",
        "upload_url": "https://uploads.github.com/repos/owner/repo/releases/{{(prerelease ? 1003 : 1002)}}/assets{?name,label}",
        "id": {{(prerelease ? 1003 : 1002)}},
        "node_id": "RE_{{(prerelease ? 1003 : 1002)}}",
        "tag_name": "{{(prerelease ? "v0.0.3-rc1" : "v0.0.2")}}",
        "target_commitish": "master",
        "draft": false,
        "prerelease": {{(prerelease ? "true" : "false")}},
        "name": "{{(prerelease ? "0.0.3-rc1" : "0.0.2")}}",
        "body": "release notes",
        "created_at": "2026-09-16T10:00:00Z",
        "published_at": "2026-09-16T10:00:00Z",
        "assets": [
          {
            "url": "https://api.github.com/repos/owner/repo/releases/assets/3001",
            "id": 3001,
            "name": "releases.{{Channel}}.json",
            "size": 700,
            "created_at": "2026-09-16T10:00:00Z",
            "content_type": "application/json",
            "state": "uploaded",
            "browser_download_url": "https://github.com/owner/repo/releases/download/{{(prerelease ? "v0.0.3-rc1" : "v0.0.2")}}/releases.{{Channel}}.json"
          },
          {
            "url": "https://api.github.com/repos/owner/repo/releases/assets/3002",
            "id": 3002,
            "name": "LYBTZYZS-{{(prerelease ? "0.0.3-rc1" : "0.0.2")}}-full.nupkg",
            "size": 123958638,
            "created_at": "2026-09-16T10:00:00Z",
            "content_type": "application/octet-stream",
            "state": "uploaded",
            "browser_download_url": "https://github.com/owner/repo/releases/download/{{(prerelease ? "v0.0.3-rc1" : "v0.0.2")}}/LYBTZYZS-{{(prerelease ? "0.0.3-rc1" : "0.0.2")}}-full.nupkg"
          }
        ]
      }
    ]
    """;

    private static (GitHubReleaseSource Source, List<string> RequestedUrls) CreateSource(
        Func<string, string?> respond, bool prerelease = false, string? accessToken = null)
    {
        var requested = new List<string>();
        var downloader = Substitute.For<IFileDownloader>();

        // Velopack 用 DownloadString 取「releases 列表」，用 DownloadBytes 取「releases.{ch}.json 清单」——
        // 两者都要配，缺一会得到空响应并在解析阶段抛 JsonException。
        downloader
            .DownloadString(
                Arg.Do<string>(url => requested.Add(url)),
                Arg.Any<IDictionary<string, string>?>(),
                Arg.Any<double>())
            .Returns(call => Task.FromResult(
                respond(call.ArgAt<string>(0)) ?? throw new InvalidOperationException("unexpected DownloadString url")));

        downloader
            .DownloadBytes(
                Arg.Do<string>(url => requested.Add(url)),
                Arg.Any<IDictionary<string, string>?>(),
                Arg.Any<double>())
            .Returns(call => Task.FromResult(System.Text.Encoding.UTF8.GetBytes(
                respond(call.ArgAt<string>(0)) ?? throw new InvalidOperationException("unexpected DownloadBytes url"))));

        return (new GitHubReleaseSource(RepoUrl, accessToken, prerelease, downloader), requested);
    }

    /// <summary>列表请求回放 releases 列表；清单请求回放 manifest。</summary>
    private static Func<string, string?> DefaultResponder(bool prerelease) => url =>
        url.Contains("/releases?", StringComparison.Ordinal) ? ReleasesListJson(prerelease)
        : url.EndsWith($"releases.{Channel}.json", StringComparison.Ordinal) ? ManifestJson
        : null;

    [Fact]
    public async Task GetReleaseFeed_UsesGitHubApiBaseUrl()
    {
        var (source, requested) = CreateSource(DefaultResponder(false));

        await source.GetReleaseFeed(new NullVelopackLogger(), "LYBTZYZS", Channel, null, null);

        requested.Should().NotBeEmpty();
        requested[0].Should().StartWith("https://api.github.com/repos/owner/repo/releases",
            "GitHub 的 API 主机是 api.github.com（不是 github.com/api/...，也不能套用 Gitee 的 {host}/api/v5/）");
    }

    [Fact]
    public async Task GetReleaseFeed_ReadsManifestAssetFromRelease()
    {
        var (source, requested) = CreateSource(DefaultResponder(false));

        var feed = await source.GetReleaseFeed(new NullVelopackLogger(), "LYBTZYZS", Channel, null, null);

        requested.Should().Contain(url => url.EndsWith($"releases.{Channel}.json", StringComparison.Ordinal),
            "Velopack 的 git 源要求 release 附带 releases.{channel}.json 清单资产");
        feed.Assets.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetReleaseFeed_ResolvesAssetsFromBrowserDownloadUrl()
    {
        var (source, requested) = CreateSource(DefaultResponder(false));

        var feed = await source.GetReleaseFeed(new NullVelopackLogger(), "LYBTZYZS", Channel, null, null);

        requested.Should().Contain(url => url.StartsWith("https://github.com/owner/repo/releases/download/", StringComparison.Ordinal),
            "包体下载走 assets[].browser_download_url（assets[].url 是 API 资源地址，不可用于下载）");
        feed.Assets.Should().Contain(a => a.FileName.EndsWith("-full.nupkg", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetReleaseFeed_ParsesManifestIntoFeed()
    {
        var (source, _) = CreateSource(DefaultResponder(false));

        var feed = await source.GetReleaseFeed(new NullVelopackLogger(), "LYBTZYZS", Channel, null, null);

        var versions = feed.Assets.Select(a => a.Version.ToString()).Distinct().ToList();
        versions.Should().Contain("0.0.2");
        versions.Should().Contain("0.0.1");
        feed.Assets.Should().OnlyContain(a => a.PackageId == "LYBTZYZS");
    }

    [Fact]
    public async Task GetReleaseFeed_ExposesDeltaAsset()
    {
        var (source, _) = CreateSource(DefaultResponder(false));

        var feed = await source.GetReleaseFeed(new NullVelopackLogger(), "LYBTZYZS", Channel, null, null);

        feed.Assets.Should().Contain(a => a.FileName.EndsWith("-delta.nupkg", StringComparison.Ordinal),
            "增量包必须出现在馈源中，否则客户端只能下载全量包");
    }

    [Fact]
    public async Task GetReleaseFeed_PrereleaseFiltered_ExcludesPrerelease()
    {
        var stable = await CreateSource(DefaultResponder(false)).Source
            .GetReleaseFeed(new NullVelopackLogger(), "LYBTZYZS", Channel, null, null);

        stable.Assets.Select(a => a.Version.ToString()).Should().NotContain(v => v.Contains("rc", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetReleaseFeed_PrereleaseEnabled_IncludesPrerelease()
    {
        var future = await CreateSource(DefaultResponder(true), prerelease: true).Source
            .GetReleaseFeed(new NullVelopackLogger(), "LYBTZYZS", Channel, null, null);

        future.Assets.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetReleaseFeed_PrereleaseTag_NotRequestedWhenPrereleaseDisabled()
    {
        var (source, requested) = CreateSource(DefaultResponder(false));

        await source.GetReleaseFeed(new NullVelopackLogger(), "LYBTZYZS", Channel, null, null);

        requested.Should().NotContain(url => url.Contains("v0.0.3-rc1", StringComparison.Ordinal),
            "prerelease=false 时不得拉取预发布 tag 的资产");
    }
}
