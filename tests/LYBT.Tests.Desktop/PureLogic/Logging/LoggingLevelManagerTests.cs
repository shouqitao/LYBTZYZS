using FluentAssertions;
using LYBT.Shared.Logging.Management;
using Serilog.Events;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Logging;

/// <summary>
/// US-LOG-004: 系统应支持运行时动态调整日志级别，以便在不重启系统的情况下进行调试。
/// 验证 LoggingLevelManager 的核心动态级别管理行为。
/// </summary>
public class LoggingLevelManagerTests : IDisposable
{
    private readonly LoggingLevelManager _sut = new(LogEventLevel.Information);

    public void Dispose() => _sut.Dispose();

    [Fact]
    public void US_LOG_004_DefaultLevel_IsInformation()
    {
        // Arrange / Act / Assert
        _sut.DefaultLevel.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public void US_LOG_004_LevelSwitch_InitiallyAtDefaultLevel()
    {
        // Arrange / Act / Assert
        _sut.LevelSwitch.MinimumLevel.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public void US_LOG_004_IsDebugModeActive_DefaultFalse()
    {
        // Arrange / Act / Assert
        _sut.IsDebugModeActive.Should().BeFalse();
    }

    [Fact]
    public void US_LOG_004_EnableDebugMode_SetsIsDebugModeActiveTrue()
    {
        // Arrange / Act
        _sut.EnableDebugMode(LogEventLevel.Debug);

        // Assert
        _sut.IsDebugModeActive.Should().BeTrue();
    }

    [Fact]
    public void US_LOG_004_EnableDebugMode_LowersMinimumLevel()
    {
        // Arrange / Act
        _sut.EnableDebugMode(LogEventLevel.Debug);

        // Assert
        _sut.LevelSwitch.MinimumLevel.Should().Be(LogEventLevel.Debug);
    }

    [Fact]
    public void US_LOG_004_EnableDebugMode_ReturnsDebugModeInfoWithIsActiveTrue()
    {
        // Arrange / Act
        var info = _sut.EnableDebugMode(LogEventLevel.Debug);

        // Assert
        info.Should().NotBeNull();
        info.IsActive.Should().BeTrue();
    }

    [Fact]
    public void US_LOG_004_DisableDebugMode_RestoresDefaultLevel()
    {
        // Arrange
        _sut.EnableDebugMode(LogEventLevel.Debug);

        // Act
        _sut.DisableDebugMode();

        // Assert
        _sut.IsDebugModeActive.Should().BeFalse();
        _sut.LevelSwitch.MinimumLevel.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public void US_LOG_004_DisableDebugMode_ReturnsDebugModeInfoWithIsActiveFalse()
    {
        // Arrange
        _sut.EnableDebugMode(LogEventLevel.Debug);

        // Act
        var info = _sut.DisableDebugMode();

        // Assert
        info.Should().NotBeNull();
        info.IsActive.Should().BeFalse();
    }

    [Fact]
    public void US_LOG_004_GetStatus_ReturnsNonNull()
    {
        // Arrange / Act
        var status = _sut.GetStatus();

        // Assert
        status.Should().NotBeNull();
    }

    [Fact]
    public void US_LOG_004_GetStatus_WhenNotActive_ReturnsIsActiveFalse()
    {
        // Arrange / Act
        var status = _sut.GetStatus();

        // Assert
        status.IsActive.Should().BeFalse();
    }

    [Fact]
    public void US_LOG_004_GetStatus_WhenActive_ReturnsIsActiveTrue()
    {
        // Arrange
        _sut.EnableDebugMode(LogEventLevel.Debug);

        // Act
        var status = _sut.GetStatus();

        // Assert
        status.IsActive.Should().BeTrue();
    }

    [Fact]
    public void US_LOG_004_SetLevel_ChangesMinimumLevel()
    {
        // Arrange / Act
        _sut.SetLevel(LogEventLevel.Warning);

        // Assert
        _sut.LevelSwitch.MinimumLevel.Should().Be(LogEventLevel.Warning);
    }

    [Fact]
    public void US_LOG_004_SetLevel_ToVerbose_IsDebugModeActive()
    {
        // Arrange / Act
        _sut.SetLevel(LogEventLevel.Verbose);

        // Assert — Verbose < Information, so debug mode is considered active
        _sut.IsDebugModeActive.Should().BeTrue();
    }
}
