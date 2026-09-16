using FluentAssertions;
using LYBT.Desktop.Foundation.Services;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Velopack.Sources;

namespace LYBT.Tests.Desktop.Unit.Foundation;

/// <summary>
/// Desktop 更新服务单测（US-SHELL-010/012）：
/// 覆盖「未启用 / 更新源配置不完整 → 静默返回、不触网络」与「源类型选择」两条契约。
/// </summary>
public class DesktopUpdateServiceTests
{
    private static DesktopUpdateService CreateService(DesktopUpdateOptions options)
        => new(
            Options.Create(options),
            new UpdateSourceFactory(Substitute.For<ILogger<UpdateSourceFactory>>()),
            Substitute.For<ILogger<DesktopUpdateService>>());

    [Fact]
    public async Task CheckForUpdates_Disabled_ReturnsNull()
    {
        var service = CreateService(new DesktopUpdateOptions { Enabled = false });

        var result = await service.CheckForUpdatesAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdates_NoFeedUrl_ReturnsNull()
    {
        var service = CreateService(new DesktopUpdateOptions { Enabled = true, FeedUrl = null });

        var result = await service.CheckForUpdatesAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckForUpdates_GiteeWithoutRepoUrl_ReturnsNull()
    {
        // SourceKind=Gitee 但缺仓库地址 → 视为未配置，不发起任何请求
        var service = CreateService(new DesktopUpdateOptions
        {
            Enabled = true,
            SourceKind = UpdateSourceKinds.Gitee,
            GiteeRepoUrl = null,
        });

        var result = await service.CheckForUpdatesAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task Download_Disabled_ReturnsFalse()
    {
        var service = CreateService(new DesktopUpdateOptions { Enabled = false });

        (await service.DownloadUpdateAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ApplyUpdate_WithoutPendingUpdate_DoesNotThrow()
    {
        // 未检查即应用：只记警告并返回（不因 _pendingUpdate 为空而抛）
        var service = CreateService(new DesktopUpdateOptions { Enabled = true, FeedUrl = "https://example.invalid/releases/" });

        var act = async () => await service.ApplyUpdateAndRestartAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void SourceFactory_ServerKind_ReturnsSimpleWebSource()
    {
        var factory = new UpdateSourceFactory(Substitute.For<ILogger<UpdateSourceFactory>>());

        var source = factory.Create(new DesktopUpdateOptions
        {
            SourceKind = UpdateSourceKinds.Server,
            FeedUrl = "https://example.com/releases/",
        });

        source.Should().BeOfType<SimpleWebSource>();
    }

    [Fact]
    public void SourceFactory_GiteeKind_ReturnsGiteeReleaseSource()
    {
        var factory = new UpdateSourceFactory(Substitute.For<ILogger<UpdateSourceFactory>>());

        var source = factory.Create(new DesktopUpdateOptions
        {
            SourceKind = UpdateSourceKinds.Gitee,
            GiteeRepoUrl = "https://gitee.com/owner/repo",
        });

        source.Should().BeOfType<GiteeReleaseSource>();
    }

    [Fact]
    public void SourceFactory_UnknownKind_FallsBackToServerKind()
    {
        var factory = new UpdateSourceFactory(Substitute.For<ILogger<UpdateSourceFactory>>());

        var source = factory.Create(new DesktopUpdateOptions
        {
            SourceKind = "SomethingElse",
            FeedUrl = "https://example.com/releases/",
        });

        source.Should().BeOfType<SimpleWebSource>();
    }
}
