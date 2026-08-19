using LYBT.Tests.Desktop.Infrastructure;
using FluentAssertions;
using LYBT.Desktop.Auth.ViewModels;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Tests.Desktop.Infrastructure;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop;

[Trait("US", "US-SHELL-007")]
public class ConnectionStatusViewModelTests : DesktopTestBase
{
    private readonly IApplicationStateService _appState;
    private readonly IConnectionModeService _connectionMode;

    public ConnectionStatusViewModelTests()
    {
        _appState = Substitute.For<IApplicationStateService>();
        _appState.IsApiHealthy.Returns(true);
        _appState.ConnectionStatus.Returns("已连接");
        _appState.ApiBaseUrl.Returns("http://localhost:5000");

        _connectionMode = Substitute.For<IConnectionModeService>();
        _connectionMode.CurrentModeDisplay.Returns("远程模式");
        _connectionMode.IsRemote.Returns(true);
        _connectionMode.IsLocal.Returns(false);
        _connectionMode.IsRemoteAvailable.Returns(true);
        _connectionMode.ApiStatusDisplay.Returns("远程 WebAPI 已连接");
        _connectionMode.CheckRemoteAvailableAsync().Returns(Task.FromResult(true));
        _connectionMode.SetModeAsync(Arg.Any<ConnectionMode>()).Returns(Task.FromResult(ModeSwitchResult.Success()));
    }

    private ConnectionStatusViewModel CreateSut(IConnectionModeService? modeService = null)
        => new(Services, _appState, modeService ?? _connectionMode);

    [Fact]
    public void Constructor_InitializesDefaultState_Should_When_CreatedWithRemoteMode()
    {
        _connectionMode.IsRemote.Returns(true);
        _connectionMode.CurrentModeDisplay.Returns("远程模式");

        var sut = CreateSut();

        sut.ApiStatus.Should().Be(ApiHealthStatus.Checking);
        sut.ApiStatusMessage.Should().Be("正在检查连接...");
        sut.IsRemoteMode.Should().BeTrue();
        sut.CurrentModeDisplay.Should().Be("远程模式");
    }

    [Fact]
    public void Constructor_SubscribesToEvents_Should_When_Created()
    {
        var sut = CreateSut();

        _connectionMode.CurrentModeDisplay.Returns("本地模式");
        _connectionMode.IsRemote.Returns(false);
        _connectionMode.IsLocal.Returns(true);
        _connectionMode.ApiStatusDisplay.Returns("本地已连接");

        _connectionMode.ModeChanged += Raise.Event<EventHandler<ConnectionMode>>(this, ConnectionMode.Local);

        sut.CurrentModeDisplay.Should().Be("本地模式");
    }

    [Fact]
    public async Task SwitchToLocal_CallsSetModeAsyncLocal_Should_When_Executed()
    {
        var sut = CreateSut();
        _connectionMode.ClearReceivedCalls();
        _connectionMode.SetModeAsync(Arg.Any<ConnectionMode>()).Returns(Task.FromResult(ModeSwitchResult.Success()));
        _connectionMode.IsRemote.Returns(false);
        _connectionMode.IsLocal.Returns(true);
        _connectionMode.CurrentModeDisplay.Returns("本地模式");

        await sut.SwitchToLocalCommand.ExecuteAsync(null);

        await _connectionMode.Received(1).SetModeAsync(ConnectionMode.Local);
    }

    [Fact]
    public async Task SwitchToLocal_UpdatesIsRemoteMode_Should_When_Succeeded()
    {
        var sut = CreateSut();
        _connectionMode.SetModeAsync(ConnectionMode.Local).Returns(Task.FromResult(ModeSwitchResult.Success()));
        _connectionMode.IsRemote.Returns(false);
        _connectionMode.CurrentModeDisplay.Returns("本地模式");
        _connectionMode.ApiStatusDisplay.Returns("本地已连接");

        await sut.SwitchToLocalCommand.ExecuteAsync(null);
        await Task.Delay(50);

        sut.IsRemoteMode.Should().BeFalse();
        sut.CurrentModeDisplay.Should().Be("本地模式");
    }

    [Fact]
    public async Task SwitchToRemote_CallsSetModeAsyncRemote_Should_When_Executed()
    {
        var sut = CreateSut();
        _connectionMode.ClearReceivedCalls();
        _connectionMode.SetModeAsync(Arg.Any<ConnectionMode>()).Returns(Task.FromResult(ModeSwitchResult.Success()));
        _connectionMode.IsRemote.Returns(true);
        _connectionMode.CurrentModeDisplay.Returns("远程模式");

        await sut.SwitchToRemoteCommand.ExecuteAsync(null);

        await _connectionMode.Received(1).SetModeAsync(ConnectionMode.Remote);
    }

    [Fact]
    public async Task SwitchToRemote_UpdatesIsRemoteMode_Should_When_Succeeded()
    {
        var sut = CreateSut();
        _connectionMode.SetModeAsync(ConnectionMode.Remote).Returns(Task.FromResult(ModeSwitchResult.Success()));
        _connectionMode.IsRemote.Returns(true);
        _connectionMode.CurrentModeDisplay.Returns("远程模式");
        _connectionMode.ApiStatusDisplay.Returns("远程 WebAPI 已连接");

        await sut.SwitchToRemoteCommand.ExecuteAsync(null);
        await Task.Delay(50);

        sut.IsRemoteMode.Should().BeTrue();
        sut.CurrentModeDisplay.Should().Be("远程模式");
    }

    [Fact]
    public void ModeChanged_UpdatesDisplay_Should_When_ExternalModeChangeFired()
    {
        var sut = CreateSut();
        _connectionMode.CurrentModeDisplay.Returns("本地模式");
        _connectionMode.IsRemote.Returns(false);
        _connectionMode.ApiStatusDisplay.Returns("本地已连接");

        _connectionMode.ModeChanged += Raise.Event<EventHandler<ConnectionMode>>(this, ConnectionMode.Local);

        sut.CurrentModeDisplay.Should().Be("本地模式");
        sut.IsRemoteMode.Should().BeFalse();
    }

    [Fact]
    public async Task SyncModeDisplay_ProbesRemoteAvailability_Should_When_SwitchCompleted()
    {
        var sut = CreateSut();
        _connectionMode.CheckRemoteAvailableAsync().Returns(Task.FromResult(false));
        _connectionMode.SetModeAsync(ConnectionMode.Local).Returns(Task.FromResult(ModeSwitchResult.Success()));
        _connectionMode.IsRemote.Returns(false);
        _connectionMode.CurrentModeDisplay.Returns("本地模式");

        await sut.SwitchToLocalCommand.ExecuteAsync(null);
        await Task.Delay(50);

        await _connectionMode.Received().CheckRemoteAvailableAsync();
        sut.IsRemoteAvailable.Should().BeFalse();
    }
}
