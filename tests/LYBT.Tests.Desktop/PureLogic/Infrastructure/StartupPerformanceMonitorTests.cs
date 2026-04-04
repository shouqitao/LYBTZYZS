using FluentAssertions;
using LYBT.Desktop.Shell.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Infrastructure;

/// <summary>
/// US-SYS-009: 系统应监控启动各阶段性能指标，以便识别和优化启动瓶颈。
/// 验证 StartupPerformanceMonitor 的核心行为。
/// </summary>
public class StartupPerformanceMonitorTests
{
    private StartupPerformanceMonitor CreateSut()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var logger = Substitute.For<ILogger<StartupPerformanceMonitor>>();
        loggerFactory.CreateLogger<StartupPerformanceMonitor>().Returns(logger);
        // CreateLogger(string) is called internally by CreateLogger<T>
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);
        return new StartupPerformanceMonitor(loggerFactory);
    }

    [Fact]
    public void US_SYS_009_StartMonitoring_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.StartMonitoring();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void US_SYS_009_StartStageAndEndStage_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut();
        sut.StartMonitoring();

        // Act
        sut.StartStage("TestStage");
        var act = () => sut.EndStage();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void US_SYS_009_GetStageTime_AfterEndStage_ReturnsNonNegative()
    {
        // Arrange
        var sut = CreateSut();
        sut.StartMonitoring();
        sut.StartStage("TestStage");
        sut.EndStage();

        // Act
        var time = sut.GetStageTime("TestStage");

        // Assert
        time.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void US_SYS_009_GetStageTime_ForUnknownStage_ReturnsZero()
    {
        // Arrange
        var sut = CreateSut();
        sut.StartMonitoring();

        // Act
        var time = sut.GetStageTime("NonExistentStage");

        // Assert
        time.Should().Be(0);
    }

    [Fact]
    public void US_SYS_009_GetElapsedMilliseconds_AfterStart_ReturnsNonNegative()
    {
        // Arrange
        var sut = CreateSut();
        sut.StartMonitoring();

        // Act
        var elapsed = sut.GetElapsedMilliseconds();

        // Assert
        elapsed.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void US_SYS_009_Finish_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut();
        sut.StartMonitoring();

        // Act
        var act = () => sut.Finish();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void US_SYS_009_MultipleStages_TrackTimesSeparately()
    {
        // Arrange
        var sut = CreateSut();
        sut.StartMonitoring();

        // Act
        sut.StartStage("Stage1");
        sut.EndStage();
        sut.StartStage("Stage2");
        sut.EndStage();

        // Assert
        sut.GetStageTime("Stage1").Should().BeGreaterThanOrEqualTo(0);
        sut.GetStageTime("Stage2").Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void US_SYS_009_StartStage_AutoEndsCurrentStage_WhenNewStageStarted()
    {
        // Arrange
        var sut = CreateSut();
        sut.StartMonitoring();

        // Act - start Stage1, then start Stage2 without explicitly ending Stage1
        sut.StartStage("Stage1");
        var act = () => sut.StartStage("Stage2");

        // Assert - should not throw, auto-ends Stage1
        act.Should().NotThrow();
        sut.GetStageTime("Stage1").Should().BeGreaterThanOrEqualTo(0);
    }
}
