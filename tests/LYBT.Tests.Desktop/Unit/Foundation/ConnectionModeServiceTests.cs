using FluentAssertions;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop;

/// <summary>
/// TDD Batch 7 L1 — ConnectionModeService（模式切换状态机，今天修改）单元测试。
/// 覆盖：切本地/远程、三条守卫（URL 配置/远程可达/未完成医案）、URL 变更统一走
/// SetModeAsync 不绕过守卫、探测更新缓存。
/// 可达性探测发真实 HTTP → 用进程内 Kestrel 迷你服务器提供 /api/v1/health。
/// </summary>
public class ConnectionModeServiceTests : IAsyncLifetime
{
    private readonly FakeHealthServer _server = new();
    private readonly IConnectionSettingsService _settings = Substitute.For<IConnectionSettingsService>();
    private readonly IApplicationStateService _appState = Substitute.For<IApplicationStateService>();
    private readonly IApiClient _apiClient = Substitute.For<IApiClient>();
    private readonly ILogger<ConnectionModeService> _logger = Substitute.For<ILogger<ConnectionModeService>>();

    public Task InitializeAsync() => _server.InitializeAsync();
    public Task DisposeAsync() => _server.DisposeAsync();

    private ConnectionModeService CreateSut(string remoteUrl, bool isLocal)
    {
        _settings.RemoteUrl.Returns(remoteUrl);
        _settings.IsValidUrl(Arg.Any<string>()).Returns(true);
        _settings.IsLocal.Returns(isLocal);
        _settings.SavePreferredModeAsync(Arg.Any<string>()).Returns(Task.CompletedTask);

        var medicalCases = Substitute.For<IApiClientMedicalCases>();
        medicalCases.GetPendingCasesAsync(Arg.Any<Guid?>())
            .Returns(Task.FromResult(new ApiResponse<List<PendingMedicalCaseDto>>
            {
                Success = true,
                Data = []
            }));
        _apiClient.MedicalCases.Returns(medicalCases);

        return new ConnectionModeService(_settings, _appState, _apiClient, _logger);
    }

    /// <summary>进程内 Kestrel：提供 /api/v1/health -> 200，用作「可达远程」的探测端点。</summary>
    private sealed class FakeHealthServer : IAsyncLifetime
    {
        private WebApplication? _app;
        public string BaseUrl { get; private set; } = string.Empty;

        public async Task InitializeAsync()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://127.0.0.1:0");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
            var builder = WebApplication.CreateBuilder(new[] { "--no-launch-settings" });
            builder.WebHost.ConfigureKestrel(o => o.Listen(System.Net.IPAddress.Loopback, 0));
            _app = builder.Build();
            _app.MapGet("/api/v1/health", () => Results.Ok(new { status = "ok" }));
            await _app.StartAsync();
            BaseUrl = _app.Urls.First();
        }

        public Task DisposeAsync()
        {
            if (_app is null) return Task.CompletedTask;
            return _app.DisposeAsync().AsTask();
        }
    }

    [Fact]
    public async Task SetMode_Local_Succeeds()
    {
        var svc = CreateSut(remoteUrl: "", isLocal: true);

        var result = await svc.SetModeAsync(ConnectionMode.Local);

        result.Succeeded.Should().BeTrue();
        svc.IsLocal.Should().BeTrue();
        svc.CurrentMode.Should().Be(ConnectionMode.Local);
        await _settings.Received(1).SavePreferredModeAsync("Local");
    }

    [Fact]
    public async Task SetMode_Remote_NoUrl_ReturnsBlocked()
    {
        var svc = CreateSut(remoteUrl: "", isLocal: true);

        var result = await svc.SetModeAsync(ConnectionMode.Remote);

        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be("NO_REMOTE_URL");
        svc.IsLocal.Should().BeTrue(); // 未切换
    }

    [Fact]
    public async Task SetMode_Remote_Unreachable_ReturnsBlocked()
    {
        // 127.0.0.1:9 无服务监听 → 连接拒绝（探测失败即视为不可达）
        var svc = CreateSut(remoteUrl: "http://127.0.0.1:9", isLocal: true);

        var result = await svc.SetModeAsync(ConnectionMode.Remote);

        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be("REMOTE_UNREACHABLE");
        svc.IsLocal.Should().BeTrue();
    }

    [Fact]
    public async Task SetMode_Remote_Succeeds()
    {
        var svc = CreateSut(remoteUrl: _server.BaseUrl, isLocal: true);

        var result = await svc.SetModeAsync(ConnectionMode.Remote);

        result.Succeeded.Should().BeTrue();
        svc.IsRemote.Should().BeTrue();
        svc.CurrentMode.Should().Be(ConnectionMode.Remote);
        await _settings.Received(1).SavePreferredModeAsync("Remote");
    }

    [Fact]
    public async Task OnUrlChanged_CallsSetModeAsync()
    {
        // 初始 Local（ctor 读 IsLocal→true）；URL 变更事件触发时 IsLocal=false（远程 URL）
        // → OnUrlChanged 推导 Remote → 统一走 SetModeAsync → 守卫阻断（不可达），不直接 ApplyMode
        _settings.IsLocal.Returns(true, false);
        _settings.RemoteUrl.Returns("http://127.0.0.1:9");
        _settings.IsValidUrl(Arg.Any<string>()).Returns(true);
        _settings.SavePreferredModeAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        var medicalCases = Substitute.For<IApiClientMedicalCases>();
        _apiClient.MedicalCases.Returns(medicalCases);
        var svc = new ConnectionModeService(_settings, _appState, _apiClient, _logger);

        _settings.UrlChanged += Raise.Event<EventHandler<string>>(_settings, "http://127.0.0.1:9");
        await Task.Delay(300); // 等 fire-and-forget SetModeAsync 完成

        // 用户配置不可达远程 → 守卫阻断 → 保持本地（不再直接 ApplyMode 绕过守卫）
        svc.CurrentMode.Should().Be(ConnectionMode.Local);
        // 未走到未完成医案守卫（远程不可达先阻断）
        await medicalCases.DidNotReceive().GetPendingCasesAsync(Arg.Any<Guid?>());
    }

    [Fact]
    public async Task CheckRemoteAvailable_UpdatesCache()
    {
        var svc = CreateSut(remoteUrl: _server.BaseUrl, isLocal: true);
        svc.IsRemoteAvailable.Should().BeFalse(); // 初始未探测

        var available = await svc.CheckRemoteAvailableAsync();

        available.Should().BeTrue();
        svc.IsRemoteAvailable.Should().BeTrue(); // 探测更新缓存
    }
}
