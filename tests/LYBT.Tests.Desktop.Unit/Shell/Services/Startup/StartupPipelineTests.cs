using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Shell.Services.Startup;
using Microsoft.Extensions.Logging;
using Moq;

namespace LYBT.Desktop.Shell.Tests.Services.Startup;

/// <summary>
/// StartupPipeline 单元测试
/// </summary>
public class StartupPipelineTests
{
    private readonly Mock<ILogger<StartupPipeline>> _loggerMock;
    private readonly StartupPipeline _sut;

    public StartupPipelineTests()
    {
        _loggerMock = new Mock<ILogger<StartupPipeline>>();
        _sut = new StartupPipeline(_loggerMock.Object);
    }

    #region 初始状态测试

    [Fact]
    public void Constructor_ShouldInitialize_WithNotStartedState()
    {
        // Assert
        _sut.State.Should().Be(StartupPipelineState.NotStarted);
        _sut.Steps.Should().BeEmpty();
    }

    #endregion

    #region 步骤注册测试

    [Fact]
    public void RegisterStep_ShouldAddStep()
    {
        // Arrange
        var stepMock = CreateMockStep("TestStep", 1, true);

        // Act
        _sut.RegisterStep(stepMock.Object);

        // Assert
        _sut.Steps.Should().HaveCount(1);
        _sut.Steps[0].Name.Should().Be("TestStep");
    }

    [Fact]
    public void RegisterStep_ShouldThrow_WhenStepIsNull()
    {
        // Act
        var act = () => _sut.RegisterStep(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterStep_ShouldThrow_WhenStepNameAlreadyExists()
    {
        // Arrange
        var step1 = CreateMockStep("DuplicateName", 1, true);
        var step2 = CreateMockStep("DuplicateName", 2, true);
        _sut.RegisterStep(step1.Object);

        // Act
        var act = () => _sut.RegisterStep(step2.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*已经注册*");
    }

    [Fact]
    public void RegisterStep_ShouldThrow_WhenPipelineAlreadyStarted()
    {
        // Arrange
        _ = _sut.ExecuteAsync();
        var step = CreateMockStep("LateStep", 1, true);

        // Act
        var act = () => _sut.RegisterStep(step.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*启动后*");
    }

    #endregion

    #region 执行测试

    [Fact]
    public async Task ExecuteAsync_WithNoSteps_ShouldComplete()
    {
        // Act
        var result = await _sut.ExecuteAsync();

        // Assert
        result.Success.Should().BeTrue();
        _sut.State.Should().Be(StartupPipelineState.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExecuteSteps_InOrderByOrder()
    {
        // Arrange
        var executionOrder = new List<string>();

        var step1 = CreateMockStep("Step1", 30, true, () => executionOrder.Add("Step1"));
        var step2 = CreateMockStep("Step2", 10, true, () => executionOrder.Add("Step2"));
        var step3 = CreateMockStep("Step3", 20, true, () => executionOrder.Add("Step3"));

        _sut.RegisterStep(step1.Object);
        _sut.RegisterStep(step2.Object);
        _sut.RegisterStep(step3.Object);

        // Act
        await _sut.ExecuteAsync();

        // Assert
        executionOrder.Should().ContainInOrder("Step2", "Step3", "Step1");
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequiredStepFails_ShouldStopAndReturnFailed()
    {
        // Arrange
        var step1 = CreateMockStep("Step1", 10, true);
        var step2 = CreateFailingStep("FailingStep", 20, true, "Test error");
        var step3 = CreateMockStep("Step3", 30, true);

        _sut.RegisterStep(step1.Object);
        _sut.RegisterStep(step2.Object);
        _sut.RegisterStep(step3.Object);

        // Act
        var result = await _sut.ExecuteAsync();

        // Assert
        result.Success.Should().BeFalse();
        result.FailedStepName.Should().Be("FailingStep");
        result.ErrorMessage.Should().Be("Test error");
        _sut.State.Should().Be(StartupPipelineState.Failed);

        // Step3 应该没有执行
        result.StepResults.Should().NotContainKey("Step3");
    }

    [Fact]
    public async Task ExecuteAsync_WhenOptionalStepFails_ShouldContinue()
    {
        // Arrange
        var executionOrder = new List<string>();

        var step1 = CreateMockStep("Step1", 10, true, () => executionOrder.Add("Step1"));
        var step2 = CreateFailingStep("OptionalFail", 20, false, "Optional error");
        var step3 = CreateMockStep("Step3", 30, true, () => executionOrder.Add("Step3"));

        _sut.RegisterStep(step1.Object);
        _sut.RegisterStep(step2.Object);
        _sut.RegisterStep(step3.Object);

        // Act
        var result = await _sut.ExecuteAsync();

        // Assert
        result.Success.Should().BeTrue();
        _sut.State.Should().Be(StartupPipelineState.Completed);
        executionOrder.Should().ContainInOrder("Step1", "Step3");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenExecutedTwice()
    {
        // Arrange
        await _sut.ExecuteAsync();

        // Act
        var act = async () => await _sut.ExecuteAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*无法重复执行*");
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ShouldReturnCancelledResult()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var step1 = CreateMockStep("Step1", 10, true, () => cts.Cancel());
        var step2 = CreateMockStep("Step2", 20, true);

        _sut.RegisterStep(step1.Object);
        _sut.RegisterStep(step2.Object);

        // Act
        var result = await _sut.ExecuteAsync(cancellationToken: cts.Token);

        // Assert
        result.Success.Should().BeFalse();
        _sut.State.Should().Be(StartupPipelineState.Cancelled);
    }

    #endregion

    #region 事件测试

    [Fact]
    public async Task ExecuteAsync_ShouldRaiseStateChangedEvent()
    {
        // Arrange
        var stateChanges = new List<(StartupPipelineState Previous, StartupPipelineState Current)>();
        _sut.StateChanged += (_, e) => stateChanges.Add((e.PreviousState, e.CurrentState));

        // Act
        await _sut.ExecuteAsync();

        // Assert
        stateChanges.Should().Contain((StartupPipelineState.NotStarted, StartupPipelineState.Running));
        stateChanges.Should().Contain((StartupPipelineState.Running, StartupPipelineState.Completed));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRaiseStepCompletedEvent()
    {
        // Arrange
        var completedSteps = new List<string>();
        _sut.StepCompleted += (_, e) => completedSteps.Add(e.StepName);

        var step1 = CreateMockStep("Step1", 10, true);
        var step2 = CreateMockStep("Step2", 20, true);
        _sut.RegisterStep(step1.Object);
        _sut.RegisterStep(step2.Object);

        // Act
        await _sut.ExecuteAsync();

        // Assert
        completedSteps.Should().ContainInOrder("Step1", "Step2");
    }

    #endregion

    #region 诊断测试

    [Fact]
    public void GetDiagnostics_ShouldReturnCorrectInfo()
    {
        // Arrange
        var step1 = CreateMockStep("Step1", 10, true);
        var step2 = CreateMockStep("Step2", 20, false);
        _sut.RegisterStep(step1.Object);
        _sut.RegisterStep(step2.Object);

        // Act
        var diagnostics = _sut.GetDiagnostics();

        // Assert
        diagnostics.CurrentState.Should().Be(StartupPipelineState.NotStarted);
        diagnostics.TotalSteps.Should().Be(2);
        diagnostics.CompletedSteps.Should().Be(0);
        diagnostics.StepDiagnostics.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDiagnostics_AfterExecution_ShouldShowCompletedSteps()
    {
        // Arrange
        var step1 = CreateMockStep("Step1", 10, true);
        _sut.RegisterStep(step1.Object);

        // Act
        await _sut.ExecuteAsync();
        var diagnostics = _sut.GetDiagnostics();

        // Assert
        diagnostics.CurrentState.Should().Be(StartupPipelineState.Completed);
        diagnostics.CompletedSteps.Should().Be(1);
        diagnostics.StepDiagnostics[0].Executed.Should().BeTrue();
        diagnostics.StepDiagnostics[0].Success.Should().BeTrue();
    }

    #endregion

    #region 辅助方法

    private static Mock<IStartupStep> CreateMockStep(string name, int order, bool isRequired, Action? onExecute = null)
    {
        var stepMock = new Mock<IStartupStep>();
        stepMock.SetupGet(s => s.Name).Returns(name);
        stepMock.SetupGet(s => s.Order).Returns(order);
        stepMock.SetupGet(s => s.IsRequired).Returns(isRequired);
        stepMock.Setup(s => s.ExecuteAsync(It.IsAny<IProgress<string>?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                onExecute?.Invoke();
                return Task.FromResult(StartupStepResult.Succeeded(TimeSpan.FromMilliseconds(10)));
            });
        return stepMock;
    }

    private static Mock<IStartupStep> CreateFailingStep(string name, int order, bool isRequired, string errorMessage)
    {
        var stepMock = new Mock<IStartupStep>();
        stepMock.SetupGet(s => s.Name).Returns(name);
        stepMock.SetupGet(s => s.Order).Returns(order);
        stepMock.SetupGet(s => s.IsRequired).Returns(isRequired);
        stepMock.Setup(s => s.ExecuteAsync(It.IsAny<IProgress<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StartupStepResult.Failed(errorMessage));
        return stepMock;
    }

    #endregion
}
