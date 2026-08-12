using FluentAssertions;
using LYBT.Desktop.Foundation.Services;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LYBT.Tests.Desktop.Unit.Foundation;

/// <summary>
/// Desktop 更新服务单测（US-SHELL-010: 未启用/未配置 FeedUrl 时静默返回 null——不触网络）
/// </summary>
public class DesktopUpdateServiceTests
{
    private static DesktopUpdateService CreateService(DesktopUpdateOptions options)
        => new(Options.Create(options), Substitute.For<ILogger<DesktopUpdateService>>());

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
    public async Task Download_Disabled_ReturnsFalse()
    {
        var service = CreateService(new DesktopUpdateOptions { Enabled = false });

        (await service.DownloadUpdateAsync()).Should().BeFalse();
    }
}
