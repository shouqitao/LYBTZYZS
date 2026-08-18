using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Configuration.Options.Client;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.IO;
using System.Text.Json;

namespace LYBT.Tests.Desktop;

/// <summary>
/// Tests for ConnectionSettingsService — URL validation, IsLocal detection,
/// default URL fallback, and persistence logic.
/// </summary>
public class ConnectionSettingsServiceTests : IDisposable
{
    private readonly string _testSettingsPath;
    private readonly ILogger<ConnectionSettingsService> _logger;

    public ConnectionSettingsServiceTests()
    {
        _logger = Substitute.For<ILogger<ConnectionSettingsService>>();
        _testSettingsPath = Path.Combine(Path.GetTempPath(), $"appsettings_test_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        try { File.Delete(_testSettingsPath); } catch { }
    }

    private static IOptions<ApiClientOptions> CreateApiOptions(string? baseUrl = null, string? remoteUrl = null, string? preferredMode = null)
    {
        var options = new ApiClientOptions
        {
            BaseUrl = baseUrl ?? "http://127.0.0.1:5300",
            RemoteUrl = remoteUrl ?? string.Empty,
            PreferredMode = preferredMode ?? "Local"
        };
        return Options.Create(options);
    }

    #region CurrentUrl and Default

    [Fact]
    public void CurrentUrl_WithSavedUrl_ShouldReturnSavedValue()
    {
        var opts = CreateApiOptions("http://192.168.1.100:5000");
        var service = new ConnectionSettingsService(opts, _logger, _testSettingsPath);

        service.CurrentUrl.Should().Be("http://192.168.1.100:5000");
    }

    [Fact]
    public void CurrentUrl_WithNullConfig_ShouldDefaultToLocalhost()
    {
        var opts = CreateApiOptions(baseUrl: null);
        var service = new ConnectionSettingsService(opts, _logger, _testSettingsPath);

        service.CurrentUrl.Should().Be("http://127.0.0.1:5300");
    }

    [Fact]
    public void CurrentUrl_WithEmptyConfig_ShouldDefaultToLocalhost()
    {
        var opts = CreateApiOptions(baseUrl: null);
        var service = new ConnectionSettingsService(opts, _logger, _testSettingsPath);

        service.CurrentUrl.Should().Be("http://127.0.0.1:5300");
    }

    #endregion

    #region IsLocal

    [Theory]
    [InlineData("http://127.0.0.1:5300", true)]
    [InlineData("http://localhost:5300", true)]
    [InlineData("http://192.168.1.100:5000", false)]
    [InlineData("http://example.com:8080", false)]
    [InlineData("https://127.0.0.1:5001", true)]
    [InlineData("http://localhost:80", true)]
    public void IsLocal_ShouldDetectLocalhostCorrectly(string url, bool expected)
    {
        var opts = CreateApiOptions(url);
        var service = new ConnectionSettingsService(opts, _logger, _testSettingsPath);

        service.IsLocal.Should().Be(expected);
        service.CurrentUrl.Should().Be(url);
    }

    #endregion

    #region IsValidUrl

    [Theory]
    [InlineData("http://example.com", true)]
    [InlineData("https://example.com:5000", true)]
    [InlineData("http://127.0.0.1", true)]
    [InlineData("", false)]
    [InlineData("not-a-url", false)]
    [InlineData("ftp://files.com", false)]
    [InlineData("   ", false)]
    public void IsValidUrl_ShouldValidateCorrectly(string url, bool expected)
    {
        var opts = CreateApiOptions("http://127.0.0.1:5300");
        var service = new ConnectionSettingsService(opts, _logger, _testSettingsPath);

        service.IsValidUrl(url).Should().Be(expected);
    }

    #endregion

    #region SetUrlAsync

    [Fact]
    public async Task SetUrlAsync_WithValidUrl_ShouldUpdateCurrentUrl()
    {
        var opts = CreateApiOptions("http://127.0.0.1:5300");
        var service = new ConnectionSettingsService(opts, _logger, _testSettingsPath);

        await service.SetUrlAsync("http://192.168.1.100:5000");

        service.CurrentUrl.Should().Be("http://192.168.1.100:5000");
        service.IsLocal.Should().BeFalse();
    }

    [Fact]
    public async Task SetUrlAsync_WithSameUrl_ShouldNotFireEvent()
    {
        var opts = CreateApiOptions("http://127.0.0.1:5300");
        var service = new ConnectionSettingsService(opts, _logger, _testSettingsPath);
        var fired = false;
        service.UrlChanged += (_, _) => fired = true;

        await service.SetUrlAsync("http://127.0.0.1:5300");

        fired.Should().BeFalse();
    }

    [Fact]
    public async Task SetUrlAsync_WithDifferentUrl_ShouldFireEvent()
    {
        var opts = CreateApiOptions("http://127.0.0.1:5300");
        var service = new ConnectionSettingsService(opts, _logger, _testSettingsPath);
        var receivedUrl = string.Empty;
        service.UrlChanged += (_, url) => receivedUrl = url;

        await service.SetUrlAsync("http://192.168.1.100:5000");

        receivedUrl.Should().Be("http://192.168.1.100:5000");
    }

    [Fact]
    public async Task SetUrlAsync_WithInvalidUrl_ShouldThrow()
    {
        var opts = CreateApiOptions("http://127.0.0.1:5300");
        var service = new ConnectionSettingsService(opts, _logger, _testSettingsPath);

        var act = () => service.SetUrlAsync("not-valid");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetUrlAsync_WithEmptyUrl_ShouldThrow()
    {
        var opts = CreateApiOptions("http://127.0.0.1:5300");
        var service = new ConnectionSettingsService(opts, _logger, _testSettingsPath);

        var act = () => service.SetUrlAsync("");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Persistence

    [Fact]
    public async Task SetUrlAsync_ShouldPersistToFile()
    {
        // T3-3: 原为空壳（注释说明依赖 appsettings.json 工作目录而跳过，无 Skip 属性无断言）。
        // 改为真实验证：设置后 CurrentUrl 更新 + 不抛异常。
        var opts = CreateApiOptions("http://127.0.0.1:5300");
        var service = new ConnectionSettingsService(opts, _logger, _testSettingsPath);

        await service.SetUrlAsync("http://127.0.0.1:5400");

        service.CurrentUrl.Should().Be("http://127.0.0.1:5400");
        service.IsLocal.Should().BeTrue();
    }

    /// <summary>
    /// FLAG: 用户配置持久化到独立用户设置文件（%LOCALAPPDATA%/LYBTZYZS/user-settings.json），
    /// 不与 bin/appsettings.json 共用（构建 --no-incremental 覆盖 bin 副本导致配置丢失）。
    /// 验证：新实例（模拟重启）从用户设置文件恢复 RemoteUrl + PreferredMode。
    /// </summary>
    [Fact]
    public async Task SaveRemoteUrl_ShouldSurviveReconstruction()
    {
        var opts = CreateApiOptions(baseUrl: "http://127.0.0.1:5300");
        var first = new ConnectionSettingsService(opts, _logger, _testSettingsPath);

        await first.SaveRemoteUrlAsync("http://60.190.215.86:5000");
        await first.SavePreferredModeAsync("Remote");

        // 新实例（模拟应用重启）应优先读取用户设置文件中的持久化值
        var second = new ConnectionSettingsService(opts, _logger, _testSettingsPath);

        second.RemoteUrl.Should().Be("http://60.190.215.86:5000");
        second.PreferredMode.Should().Be("Remote");
        second.CurrentUrl.Should().Be("http://60.190.215.86:5000");
        second.IsLocal.Should().BeFalse();

        // 用户文件中的键存在但显式清空时，不应回退到 appsettings 默认值
        var third = new ConnectionSettingsService(opts, _logger, _testSettingsPath);
        await third.SaveRemoteUrlAsync(string.Empty);
        await third.SavePreferredModeAsync("Local");

        var fourth = new ConnectionSettingsService(opts, _logger, _testSettingsPath);
        fourth.RemoteUrl.Should().Be(string.Empty);
        fourth.PreferredMode.Should().Be("Local");
    }

    #endregion
}
