using FluentAssertions;
using Xunit;
using LYBT.Shared.Logging.Management;
using Serilog.Events;

namespace LYBT.Tests.Server.PureLogic.Shared.Logging;

/// <summary>
/// LoggingLevelManager 单元测试
/// Sprint3-A3-09: Shared.Logging 零覆盖测试
/// </summary>
public class LoggingLevelManagerTests : IDisposable
{
    private readonly LoggingLevelManager _manager;

    public LoggingLevelManagerTests()
    {
        _manager = new LoggingLevelManager(LogEventLevel.Information);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultLevel()
    {
        _manager.DefaultLevel.Should().Be(LogEventLevel.Information);
        _manager.LevelSwitch.MinimumLevel.Should().Be(LogEventLevel.Information);
        _manager.IsDebugModeActive.Should().BeFalse();
    }

    [Fact]
    public void EnableDebugMode_ShouldLowerLevel()
    {
        var result = _manager.EnableDebugMode(LogEventLevel.Debug);

        result.IsActive.Should().BeTrue();
        result.CurrentLevel.Should().Be("Debug");
        _manager.LevelSwitch.MinimumLevel.Should().Be(LogEventLevel.Debug);
        _manager.IsDebugModeActive.Should().BeTrue();
        _manager.DebugModeStartedAt.Should().NotBeNull();
    }

    [Fact]
    public void EnableDebugMode_WithDuration_ShouldSetExpiration()
    {
        var result = _manager.EnableDebugMode(LogEventLevel.Debug, 30);

        result.ExpiresAt.Should().NotBeNull();
        result.DurationMinutes.Should().Be(30);
        _manager.DebugModeExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public void EnableDebugMode_WithoutDuration_ShouldNotSetExpiration()
    {
        var result = _manager.EnableDebugMode(LogEventLevel.Debug, null);

        result.ExpiresAt.Should().BeNull();
        _manager.DebugModeExpiresAt.Should().BeNull();
    }

    [Fact]
    public void DisableDebugMode_ShouldRestoreDefaultLevel()
    {
        _manager.EnableDebugMode(LogEventLevel.Debug);
        var result = _manager.DisableDebugMode();

        result.IsActive.Should().BeFalse();
        result.CurrentLevel.Should().Be("Information");
        _manager.LevelSwitch.MinimumLevel.Should().Be(LogEventLevel.Information);
        _manager.IsDebugModeActive.Should().BeFalse();
        _manager.DebugModeStartedAt.Should().BeNull();
    }

    [Fact]
    public void SetLevel_ShouldChangeMinimumLevel()
    {
        _manager.SetLevel(LogEventLevel.Warning);
        _manager.LevelSwitch.MinimumLevel.Should().Be(LogEventLevel.Warning);
    }

    [Fact]
    public void GetStatus_WhenNotInDebugMode_ShouldReturnInactive()
    {
        var status = _manager.GetStatus();

        status.IsActive.Should().BeFalse();
        status.CurrentLevel.Should().Be("Information");
        status.DefaultLevel.Should().Be("Information");
    }

    [Fact]
    public void GetStatus_WhenInDebugMode_ShouldReturnActive()
    {
        _manager.EnableDebugMode(LogEventLevel.Verbose, 60);
        var status = _manager.GetStatus();

        status.IsActive.Should().BeTrue();
        status.CurrentLevel.Should().Be("Verbose");
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var manager = new LoggingLevelManager();
        manager.EnableDebugMode(LogEventLevel.Debug, 10);

        var act = () => manager.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledTwice_ShouldNotThrow()
    {
        var manager = new LoggingLevelManager();
        manager.Dispose();

        var act = () => manager.Dispose();
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        _manager.Dispose();
    }
}
