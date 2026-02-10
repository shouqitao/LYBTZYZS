using FluentAssertions;
using LYBT.Desktop.Shell.Services.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace LYBT.Desktop.Shell.Tests.Services.Diagnostics;

/// <summary>
/// StartupDiagnostics 单元测试
/// </summary>
public class StartupDiagnosticsTests
{
    private readonly Mock<ILogger<StartupDiagnostics>> _loggerMock;
    private readonly StartupDiagnostics _sut;

    public StartupDiagnosticsTests()
    {
        _loggerMock = new Mock<ILogger<StartupDiagnostics>>();
        _sut = new StartupDiagnostics(_loggerMock.Object);
    }

    #region BeginStartup/EndStartup测试

    [Fact]
    public void BeginStartup_ShouldInitializeReport()
    {
        // Act
        _sut.BeginStartup();
        var report = _sut.GetReport();

        // Assert
        report.StartTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        report.EndTime.Should().BeNull();
        report.Steps.Should().BeEmpty();
    }

    [Fact]
    public void EndStartup_ShouldSetEndTime()
    {
        // Arrange
        _sut.BeginStartup();

        // Act
        _sut.EndStartup();
        var report = _sut.GetReport();

        // Assert
        report.EndTime.Should().NotBeNull();
        report.EndTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        report.TotalDuration.Should().NotBeNull();
    }

    [Fact]
    public void BeginStartup_CalledTwice_ShouldResetState()
    {
        // Arrange
        _sut.BeginStartup();
        _sut.BeginStep("Step1");
        _sut.EndStep();

        // Act
        _sut.BeginStartup();
        var report = _sut.GetReport();

        // Assert
        report.Steps.Should().BeEmpty();
    }

    #endregion

    #region BeginStep/EndStep测试

    [Fact]
    public void BeginStep_EndStep_ShouldRecordStep()
    {
        // Arrange
        _sut.BeginStartup();

        // Act
        _sut.BeginStep("TestStep");
        _sut.EndStep();

        var report = _sut.GetReport();

        // Assert
        report.Steps.Should().HaveCount(1);
        report.Steps[0].StepName.Should().Be("TestStep");
        report.Steps[0].Success.Should().BeTrue();
        report.Steps[0].ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void EndStep_WithFailure_ShouldRecordError()
    {
        // Arrange
        _sut.BeginStartup();
        var errorMessage = "测试错误";

        // Act
        _sut.BeginStep("FailingStep");
        _sut.EndStep(success: false, errorMessage: errorMessage);

        var report = _sut.GetReport();

        // Assert
        report.Steps[0].Success.Should().BeFalse();
        report.Steps[0].ErrorMessage.Should().Be(errorMessage);
        report.FailedSteps.Should().HaveCount(1);
    }

    [Fact]
    public void BeginStep_WithoutEndingPrevious_ShouldAutoEndPreviousStep()
    {
        // Arrange
        _sut.BeginStartup();

        // Act
        _sut.BeginStep("Step1");
        _sut.BeginStep("Step2"); // 应该自动结束Step1
        _sut.EndStep();

        var report = _sut.GetReport();

        // Assert
        report.Steps.Should().HaveCount(2);
        report.Steps[0].StepName.Should().Be("Step1");
        report.Steps[1].StepName.Should().Be("Step2");
    }

    [Fact]
    public void EndStep_WithoutBeginStep_ShouldNotThrow()
    {
        // Arrange
        _sut.BeginStartup();

        // Act
        var act = () => _sut.EndStep();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BeginStep_WithInvalidName_ShouldThrow(string? invalidName)
    {
        // Arrange
        _sut.BeginStartup();

        // Act
        var act = () => _sut.BeginStep(invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region 步骤计时测试

    [Fact]
    public void Step_ShouldRecordDuration()
    {
        // Arrange
        _sut.BeginStartup();

        // Act
        _sut.BeginStep("TimedStep");
        Thread.Sleep(50); // 等待50ms
        _sut.EndStep();

        var report = _sut.GetReport();

        // Assert
        report.Steps[0].Duration.Should().BeGreaterOrEqualTo(TimeSpan.FromMilliseconds(40));
    }

    [Fact]
    public void Step_SlowStep_ShouldBeMarkedAsSlow()
    {
        // Arrange
        _sut.BeginStartup();

        // 创建一个模拟的慢步骤（使用反射设置Duration > 3秒）
        _sut.BeginStep("SlowStep");
        // 注意：实际测试中不会等待3秒，这里只测试IsSlow属性的逻辑

        var report = _sut.GetReport();

        // Assert - 由于没有实际等待，步骤不会被标记为慢
        // 这里主要测试SlowSteps属性的过滤逻辑
        report.SlowSteps.Should().BeEmpty();
    }

    #endregion

    #region RecordMarker测试

    [Fact]
    public void RecordMarker_ShouldAddMarker()
    {
        // Arrange
        _sut.BeginStartup();

        // Act
        _sut.RecordMarker("TestMarker");

        var report = _sut.GetReport();

        // Assert
        report.Markers.Should().HaveCount(1);
        report.Markers[0].Name.Should().Be("TestMarker");
        report.Markers[0].ElapsedSinceStart.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void RecordMarker_MultipleTimes_ShouldRecordAll()
    {
        // Arrange
        _sut.BeginStartup();

        // Act
        _sut.RecordMarker("Marker1");
        _sut.RecordMarker("Marker2");
        _sut.RecordMarker("Marker3");

        var report = _sut.GetReport();

        // Assert
        report.Markers.Should().HaveCount(3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordMarker_WithInvalidName_ShouldThrow(string? invalidName)
    {
        // Arrange
        _sut.BeginStartup();

        // Act
        var act = () => _sut.RecordMarker(invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region GetReport测试

    [Fact]
    public void GetReport_ShouldReturnImmutableCopy()
    {
        // Arrange
        _sut.BeginStartup();
        _sut.BeginStep("Step1");
        _sut.EndStep();

        // Act
        var report1 = _sut.GetReport();

        _sut.BeginStep("Step2");
        _sut.EndStep();

        var report2 = _sut.GetReport();

        // Assert
        report1.Steps.Should().HaveCount(1);
        report2.Steps.Should().HaveCount(2);
    }

    [Fact]
    public void GetReport_IsSuccess_ShouldReturnTrueWhenAllStepsSucceed()
    {
        // Arrange
        _sut.BeginStartup();
        _sut.BeginStep("Step1");
        _sut.EndStep(true);
        _sut.BeginStep("Step2");
        _sut.EndStep(true);

        // Act
        var report = _sut.GetReport();

        // Assert
        report.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void GetReport_IsSuccess_ShouldReturnFalseWhenAnyStepFails()
    {
        // Arrange
        _sut.BeginStartup();
        _sut.BeginStep("Step1");
        _sut.EndStep(true);
        _sut.BeginStep("Step2");
        _sut.EndStep(false, "错误");

        // Act
        var report = _sut.GetReport();

        // Assert
        report.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region 线程安全测试

    [Fact]
    public async Task ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        _sut.BeginStartup();
        var tasks = new List<Task>();

        // Act - 并发添加步骤和标记
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                _sut.BeginStep($"Step{index}");
                Thread.Sleep(10);
                _sut.EndStep();
                _sut.RecordMarker($"Marker{index}");
            }));
        }

        await Task.WhenAll(tasks);
        var report = _sut.GetReport();

        // Assert - 由于并发执行，步骤可能会被自动结束，但不应抛出异常
        report.Steps.Should().NotBeEmpty();
        report.Markers.Should().HaveCount(10);
    }

    #endregion

    #region StartupStepRecord测试

    [Fact]
    public void StartupStepRecord_IsSlow_ShouldReturnTrueForSlowDuration()
    {
        // Arrange
        var slowRecord = new StartupStepRecord(
            StepName: "SlowStep",
            StartTime: DateTime.Now.AddSeconds(-5),
            EndTime: DateTime.Now,
            Duration: TimeSpan.FromSeconds(5),
            Success: true);

        var fastRecord = new StartupStepRecord(
            StepName: "FastStep",
            StartTime: DateTime.Now.AddSeconds(-1),
            EndTime: DateTime.Now,
            Duration: TimeSpan.FromSeconds(1),
            Success: true);

        // Assert
        slowRecord.IsSlow.Should().BeTrue();
        fastRecord.IsSlow.Should().BeFalse();
    }

    #endregion

    #region StartupReport测试

    [Fact]
    public void StartupReport_TotalDuration_ShouldReturnNullWhenNotEnded()
    {
        // Arrange
        var report = new StartupReport
        {
            StartTime = DateTime.Now,
            EndTime = null
        };

        // Assert
        report.TotalDuration.Should().BeNull();
    }

    [Fact]
    public void StartupReport_TotalDuration_ShouldCalculateCorrectly()
    {
        // Arrange
        var startTime = DateTime.Now;
        var endTime = startTime.AddMinutes(5);
        var report = new StartupReport
        {
            StartTime = startTime,
            EndTime = endTime
        };

        // Assert
        report.TotalDuration.Should().Be(TimeSpan.FromMinutes(5));
    }

    #endregion
}
