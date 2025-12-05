using FluentAssertions;
using LYBT.Desktop.Shell.Services.Lifecycle;
using Microsoft.Extensions.Logging;
using Moq;

namespace LYBT.Desktop.Shell.Tests.Services.Lifecycle;

/// <summary>
/// ApplicationLifecycle 单元测试
/// </summary>
public class ApplicationLifecycleTests
{
    private readonly Mock<ILogger<ApplicationLifecycle>> _loggerMock;
    private readonly ApplicationLifecycle _sut;

    public ApplicationLifecycleTests()
    {
        _loggerMock = new Mock<ILogger<ApplicationLifecycle>>();
        _sut = new ApplicationLifecycle(_loggerMock.Object);
    }

    #region 初始状态测试

    [Fact]
    public void Constructor_ShouldInitialize_WithNotStartedState()
    {
        // Assert
        _sut.CurrentState.Should().Be(ApplicationState.NotStarted);
        _sut.GetTransitionHistory().Should().BeEmpty();
    }

    #endregion

    #region 状态转换测试

    [Fact]
    public async Task TransitionToAsync_ValidTransition_ShouldChangeState()
    {
        // Arrange
        _sut.CurrentState.Should().Be(ApplicationState.NotStarted);

        // Act
        var result = await _sut.TransitionToAsync(ApplicationState.Initializing);

        // Assert
        result.Should().BeTrue();
        _sut.CurrentState.Should().Be(ApplicationState.Initializing);
    }

    [Fact]
    public async Task TransitionToAsync_InvalidTransition_ShouldReturnFalseAndNotChangeState()
    {
        // Arrange
        _sut.CurrentState.Should().Be(ApplicationState.NotStarted);

        // Act - 尝试从NotStarted直接跳到Running（无效）
        var result = await _sut.TransitionToAsync(ApplicationState.Running);

        // Assert
        result.Should().BeFalse();
        _sut.CurrentState.Should().Be(ApplicationState.NotStarted);
    }

    [Fact]
    public async Task TransitionToAsync_NotStartedToInitializing_ShouldSucceed()
    {
        // Act
        var result = await _sut.TransitionToAsync(ApplicationState.Initializing);

        // Assert
        result.Should().BeTrue();
        _sut.CurrentState.Should().Be(ApplicationState.Initializing);
    }

    [Fact]
    public async Task TransitionToAsync_InitializingToAuthenticating_ShouldSucceed()
    {
        // Arrange
        await _sut.TransitionToAsync(ApplicationState.Initializing);

        // Act
        var result = await _sut.TransitionToAsync(ApplicationState.Authenticating);

        // Assert
        result.Should().BeTrue();
        _sut.CurrentState.Should().Be(ApplicationState.Authenticating);
    }

    [Fact]
    public async Task TransitionToAsync_AuthenticatingToReady_ShouldSucceed()
    {
        // Arrange
        await _sut.TransitionToAsync(ApplicationState.Initializing);
        await _sut.TransitionToAsync(ApplicationState.Authenticating);

        // Act
        var result = await _sut.TransitionToAsync(ApplicationState.Ready);

        // Assert
        result.Should().BeTrue();
        _sut.CurrentState.Should().Be(ApplicationState.Ready);
    }

    [Fact]
    public async Task TransitionToAsync_ReadyToRunning_ShouldSucceed()
    {
        // Arrange
        await _sut.TransitionToAsync(ApplicationState.Initializing);
        await _sut.TransitionToAsync(ApplicationState.Authenticating);
        await _sut.TransitionToAsync(ApplicationState.Ready);

        // Act
        var result = await _sut.TransitionToAsync(ApplicationState.Running);

        // Assert
        result.Should().BeTrue();
        _sut.CurrentState.Should().Be(ApplicationState.Running);
    }

    [Fact]
    public async Task TransitionToAsync_RunningToShuttingDown_ShouldSucceed()
    {
        // Arrange
        await _sut.TransitionToAsync(ApplicationState.Initializing);
        await _sut.TransitionToAsync(ApplicationState.Authenticating);
        await _sut.TransitionToAsync(ApplicationState.Ready);
        await _sut.TransitionToAsync(ApplicationState.Running);

        // Act
        var result = await _sut.TransitionToAsync(ApplicationState.ShuttingDown);

        // Assert
        result.Should().BeTrue();
        _sut.CurrentState.Should().Be(ApplicationState.ShuttingDown);
    }

    [Fact]
    public async Task TransitionToAsync_RunningToAuthenticating_ShouldSucceed()
    {
        // Arrange - 重新认证场景
        await _sut.TransitionToAsync(ApplicationState.Initializing);
        await _sut.TransitionToAsync(ApplicationState.Authenticating);
        await _sut.TransitionToAsync(ApplicationState.Ready);
        await _sut.TransitionToAsync(ApplicationState.Running);

        // Act
        var result = await _sut.TransitionToAsync(ApplicationState.Authenticating);

        // Assert
        result.Should().BeTrue();
        _sut.CurrentState.Should().Be(ApplicationState.Authenticating);
    }

    [Fact]
    public async Task TransitionToAsync_ShuttingDownToAnyState_ShouldFail()
    {
        // Arrange
        await _sut.TransitionToAsync(ApplicationState.Initializing);
        await _sut.TransitionToAsync(ApplicationState.ShuttingDown);

        // Act
        var result = await _sut.TransitionToAsync(ApplicationState.Running);

        // Assert
        result.Should().BeFalse();
        _sut.CurrentState.Should().Be(ApplicationState.ShuttingDown);
    }

    #endregion

    #region 转换历史测试

    [Fact]
    public async Task TransitionToAsync_ShouldRecordTransitionHistory()
    {
        // Act
        await _sut.TransitionToAsync(ApplicationState.Initializing);
        await _sut.TransitionToAsync(ApplicationState.Authenticating);

        // Assert
        var history = _sut.GetTransitionHistory();
        history.Should().HaveCount(2);
        history[0].FromState.Should().Be(ApplicationState.NotStarted);
        history[0].ToState.Should().Be(ApplicationState.Initializing);
        history[0].Success.Should().BeTrue();
        history[1].FromState.Should().Be(ApplicationState.Initializing);
        history[1].ToState.Should().Be(ApplicationState.Authenticating);
        history[1].Success.Should().BeTrue();
    }

    [Fact]
    public async Task TransitionToAsync_InvalidTransition_ShouldRecordFailure()
    {
        // Act - 无效转换
        await _sut.TransitionToAsync(ApplicationState.Running);

        // Assert
        var history = _sut.GetTransitionHistory();
        history.Should().HaveCount(1);
        history[0].Success.Should().BeFalse();
        history[0].ErrorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region 事件测试

    [Fact]
    public async Task TransitionToAsync_ShouldRaiseStateChangedEvent()
    {
        // Arrange
        ApplicationState? previousState = null;
        ApplicationState? currentState = null;
        _sut.StateChanged += (sender, args) =>
        {
            previousState = args.PreviousState;
            currentState = args.CurrentState;
        };

        // Act
        await _sut.TransitionToAsync(ApplicationState.Initializing);

        // Assert
        previousState.Should().Be(ApplicationState.NotStarted);
        currentState.Should().Be(ApplicationState.Initializing);
    }

    [Fact]
    public async Task TransitionToAsync_InvalidTransition_ShouldNotRaiseEvent()
    {
        // Arrange
        var eventRaised = false;
        _sut.StateChanged += (sender, args) => eventRaised = true;

        // Act
        await _sut.TransitionToAsync(ApplicationState.Running);

        // Assert
        eventRaised.Should().BeFalse();
    }

    #endregion

    #region Handler测试

    [Fact]
    public async Task RegisterStateHandler_ShouldBeCalledOnTransition()
    {
        // Arrange
        var handlerCalled = false;
        _sut.RegisterStateHandler(ApplicationState.Initializing, () =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await _sut.TransitionToAsync(ApplicationState.Initializing);

        // Assert
        handlerCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterStateHandler_ShouldNotBeCalledForOtherStates()
    {
        // Arrange
        var handlerCalled = false;
        _sut.RegisterStateHandler(ApplicationState.Running, () =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await _sut.TransitionToAsync(ApplicationState.Initializing);

        // Assert
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterStateHandler_HandlerThrows_ShouldRollbackState()
    {
        // Arrange
        _sut.RegisterStateHandler(ApplicationState.Initializing, () =>
        {
            throw new InvalidOperationException("测试异常");
        });

        // Act
        var result = await _sut.TransitionToAsync(ApplicationState.Initializing);

        // Assert
        result.Should().BeFalse();
        _sut.CurrentState.Should().Be(ApplicationState.NotStarted);
    }

    [Fact]
    public void RemoveStateHandler_ShouldRemoveHandler()
    {
        // Arrange
        var handlerCalled = false;
        _sut.RegisterStateHandler(ApplicationState.Initializing, () =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        // Act
        _sut.RemoveStateHandler(ApplicationState.Initializing);

        // Assert - 应该不抛出异常
        var act = () => _sut.RemoveStateHandler(ApplicationState.Initializing);
        act.Should().NotThrow();
    }

    #endregion

    #region 完整启动流程测试

    [Fact]
    public async Task FullStartupFlow_ShouldSucceed()
    {
        // Act - 模拟完整启动流程
        var step1 = await _sut.TransitionToAsync(ApplicationState.Initializing);
        var step2 = await _sut.TransitionToAsync(ApplicationState.Authenticating);
        var step3 = await _sut.TransitionToAsync(ApplicationState.Ready);
        var step4 = await _sut.TransitionToAsync(ApplicationState.Running);

        // Assert
        step1.Should().BeTrue();
        step2.Should().BeTrue();
        step3.Should().BeTrue();
        step4.Should().BeTrue();
        _sut.CurrentState.Should().Be(ApplicationState.Running);
        _sut.GetTransitionHistory().Should().HaveCount(4);
    }

    [Fact]
    public async Task FullShutdownFlow_ShouldSucceed()
    {
        // Arrange
        await _sut.TransitionToAsync(ApplicationState.Initializing);
        await _sut.TransitionToAsync(ApplicationState.Authenticating);
        await _sut.TransitionToAsync(ApplicationState.Ready);
        await _sut.TransitionToAsync(ApplicationState.Running);

        // Act
        var result = await _sut.TransitionToAsync(ApplicationState.ShuttingDown);

        // Assert
        result.Should().BeTrue();
        _sut.CurrentState.Should().Be(ApplicationState.ShuttingDown);
    }

    #endregion
}
