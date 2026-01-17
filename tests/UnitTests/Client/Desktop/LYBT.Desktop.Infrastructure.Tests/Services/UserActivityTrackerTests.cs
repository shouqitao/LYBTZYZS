using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Desktop.Infrastructure.Tests.Services;

/// <summary>
/// UserActivityTracker单元测试
/// OpenSpec: refactor-token-sliding-expiration - Task 7.2
/// </summary>
public class UserActivityTrackerTests
{
    /// <summary>
    /// 静态构造函数：初始化WPF资源
    /// </summary>
    static UserActivityTrackerTests()
    {
        WpfTestInitializer.Initialize();
    }

    private readonly ILogger<UserActivityTracker> _mockLogger;
    private readonly IApplicationTickService _mockTickService;

    public UserActivityTrackerTests()
    {
        _mockLogger = Substitute.For<ILogger<UserActivityTracker>>();
        _mockTickService = Substitute.For<IApplicationTickService>();
        _mockTickService.TickCount.Returns(0L);
    }

    #region Constructor Tests

    /// <summary>
    /// 测试：构造函数传入null logger应该抛出异常
    /// </summary>
    [StaFact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new UserActivityTracker(null!, _mockTickService);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// 测试：构造函数传入null tickService应该抛出异常
    /// </summary>
    [StaFact]
    public void Constructor_WithNullTickService_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new UserActivityTracker(_mockLogger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tickService");
    }

    /// <summary>
    /// 测试：构造函数应该使用默认配置值
    /// </summary>
    [StaFact]
    public void Constructor_ShouldUseDefaultConfigValues()
    {
        // Arrange & Act
        using var tracker = new UserActivityTracker(_mockLogger, _mockTickService);

        // Assert
        tracker.IsTracking.Should().BeFalse("因为新创建的tracker不应该在追踪中");
        tracker.IsUserActive.Should().BeTrue("因为刚创建时应该视为活跃");
    }

    /// <summary>
    /// 测试：构造函数应该接受自定义配置值
    /// </summary>
    [StaFact]
    public void Constructor_WithCustomConfig_ShouldAcceptValues()
    {
        // Arrange & Act
        using var tracker = new UserActivityTracker(
            _mockLogger,
            _mockTickService,
            inactivityTimeoutMinutes: 30,
            warningBeforeTimeoutMinutes: 5,
            activityCheckIntervalSeconds: 120);

        // Assert
        tracker.Should().NotBeNull();
        tracker.IsTracking.Should().BeFalse();
    }

    #endregion

    #region StartTracking Tests

    /// <summary>
    /// 测试：StartTracking应该设置IsTracking为true
    /// </summary>
    [StaFact]
    public void StartTracking_ShouldSetIsTrackingToTrue()
    {
        // Arrange
        using var tracker = new UserActivityTracker(_mockLogger, _mockTickService);

        // Act
        tracker.StartTracking();

        // Assert
        tracker.IsTracking.Should().BeTrue("因为StartTracking应该启动追踪");
    }

    /// <summary>
    /// 测试：StartTracking应该订阅TickService的Tick事件
    /// </summary>
    [StaFact]
    public void StartTracking_ShouldSubscribeToTickEvent()
    {
        // Arrange
        using var tracker = new UserActivityTracker(_mockLogger, _mockTickService);

        // Act
        tracker.StartTracking();

        // Assert
        _mockTickService.Received(1).Tick += Arg.Any<EventHandler<ApplicationTickEventArgs>>();
    }

    /// <summary>
    /// 测试：重复调用StartTracking不应该重复订阅
    /// </summary>
    [StaFact]
    public void StartTracking_WhenAlreadyTracking_ShouldNotSubscribeAgain()
    {
        // Arrange
        using var tracker = new UserActivityTracker(_mockLogger, _mockTickService);
        tracker.StartTracking();
        _mockTickService.ClearReceivedCalls();

        // Act
        tracker.StartTracking();

        // Assert
        _mockTickService.DidNotReceive().Tick += Arg.Any<EventHandler<ApplicationTickEventArgs>>();
    }

    /// <summary>
    /// 测试：StartTracking应该重置LastActivityTime
    /// </summary>
    [StaFact]
    public void StartTracking_ShouldResetLastActivityTime()
    {
        // Arrange
        using var tracker = new UserActivityTracker(_mockLogger, _mockTickService);
        var beforeStart = DateTime.Now;

        // Act
        tracker.StartTracking();
        var afterStart = DateTime.Now;

        // Assert
        tracker.LastActivityTime.Should().BeOnOrAfter(beforeStart);
        tracker.LastActivityTime.Should().BeOnOrBefore(afterStart);
    }

    #endregion

    #region StopTracking Tests

    /// <summary>
    /// 测试：StopTracking应该设置IsTracking为false
    /// </summary>
    [StaFact]
    public void StopTracking_ShouldSetIsTrackingToFalse()
    {
        // Arrange
        using var tracker = new UserActivityTracker(_mockLogger, _mockTickService);
        tracker.StartTracking();

        // Act
        tracker.StopTracking();

        // Assert
        tracker.IsTracking.Should().BeFalse("因为StopTracking应该停止追踪");
    }

    /// <summary>
    /// 测试：StopTracking应该取消订阅TickService的Tick事件
    /// </summary>
    [StaFact]
    public void StopTracking_ShouldUnsubscribeFromTickEvent()
    {
        // Arrange
        using var tracker = new UserActivityTracker(_mockLogger, _mockTickService);
        tracker.StartTracking();
        _mockTickService.ClearReceivedCalls();

        // Act
        tracker.StopTracking();

        // Assert
        _mockTickService.Received(1).Tick -= Arg.Any<EventHandler<ApplicationTickEventArgs>>();
    }

    /// <summary>
    /// 测试：未启动时调用StopTracking不应该抛出异常
    /// </summary>
    [StaFact]
    public void StopTracking_WhenNotTracking_ShouldNotThrow()
    {
        // Arrange
        using var tracker = new UserActivityTracker(_mockLogger, _mockTickService);

        // Act
        var act = () => tracker.StopTracking();

        // Assert
        act.Should().NotThrow("因为未启动时调用StopTracking不应该抛出异常");
    }

    #endregion

    #region ResetActivity Tests

    /// <summary>
    /// 测试：ResetActivity应该更新LastActivityTime
    /// </summary>
    [StaFact]
    public void ResetActivity_ShouldUpdateLastActivityTime()
    {
        // Arrange
        using var tracker = new UserActivityTracker(_mockLogger, _mockTickService);
        var initialTime = tracker.LastActivityTime;

        // 等待一小段时间以确保时间差
        Thread.Sleep(10);
        var beforeReset = DateTime.Now;

        // Act
        tracker.ResetActivity();
        var afterReset = DateTime.Now;

        // Assert
        tracker.LastActivityTime.Should().BeOnOrAfter(beforeReset);
        tracker.LastActivityTime.Should().BeOnOrBefore(afterReset);
    }

    /// <summary>
    /// 测试：ResetActivity应该使IsUserActive返回true
    /// </summary>
    [StaFact]
    public void ResetActivity_ShouldSetIsUserActiveToTrue()
    {
        // Arrange
        using var tracker = new UserActivityTracker(_mockLogger, _mockTickService);

        // Act
        tracker.ResetActivity();

        // Assert
        tracker.IsUserActive.Should().BeTrue("因为ResetActivity后用户应该被视为活跃");
    }

    #endregion

    #region IsUserActive Tests

    /// <summary>
    /// 测试：刚创建的tracker应该返回IsUserActive=true
    /// </summary>
    [StaFact]
    public void IsUserActive_WhenJustCreated_ShouldReturnTrue()
    {
        // Arrange
        using var tracker = new UserActivityTracker(_mockLogger, _mockTickService);

        // Assert
        tracker.IsUserActive.Should().BeTrue("因为刚创建的tracker应该视为活跃");
    }

    #endregion

    #region TimeUntilInactive Tests

    /// <summary>
    /// 测试：刚创建的tracker应该返回接近完整超时时间
    /// </summary>
    [StaFact]
    public void TimeUntilInactive_WhenJustCreated_ShouldReturnNearFullTimeout()
    {
        // Arrange
        var timeoutMinutes = 15;
        using var tracker = new UserActivityTracker(
            _mockLogger,
            _mockTickService,
            inactivityTimeoutMinutes: timeoutMinutes);

        // Assert
        tracker.TimeUntilInactive.TotalMinutes.Should().BeGreaterThan(timeoutMinutes - 1,
            "因为刚创建时剩余时间应该接近完整超时时间");
    }

    /// <summary>
    /// 测试：ResetActivity后应该返回接近完整超时时间
    /// </summary>
    [StaFact]
    public void TimeUntilInactive_AfterResetActivity_ShouldReturnNearFullTimeout()
    {
        // Arrange
        var timeoutMinutes = 15;
        using var tracker = new UserActivityTracker(
            _mockLogger,
            _mockTickService,
            inactivityTimeoutMinutes: timeoutMinutes);

        // Act
        tracker.ResetActivity();

        // Assert
        tracker.TimeUntilInactive.TotalMinutes.Should().BeGreaterThan(timeoutMinutes - 1,
            "因为ResetActivity后剩余时间应该接近完整超时时间");
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// 测试：Dispose应该停止追踪
    /// </summary>
    [StaFact]
    public void Dispose_ShouldStopTracking()
    {
        // Arrange
        var tracker = new UserActivityTracker(_mockLogger, _mockTickService);
        tracker.StartTracking();

        // Act
        tracker.Dispose();

        // Assert
        tracker.IsTracking.Should().BeFalse("因为Dispose应该停止追踪");
    }

    /// <summary>
    /// 测试：多次调用Dispose不应该抛出异常
    /// </summary>
    [StaFact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var tracker = new UserActivityTracker(_mockLogger, _mockTickService);

        // Act
        var act = () =>
        {
            tracker.Dispose();
            tracker.Dispose();
            tracker.Dispose();
        };

        // Assert
        act.Should().NotThrow("因为多次调用Dispose不应该抛出异常");
    }

    /// <summary>
    /// 测试：Dispose后调用StartTracking不应该启动追踪
    /// </summary>
    [StaFact]
    public void StartTracking_AfterDispose_ShouldNotTrack()
    {
        // Arrange
        var tracker = new UserActivityTracker(_mockLogger, _mockTickService);
        tracker.Dispose();

        // Act
        tracker.StartTracking();

        // Assert
        tracker.IsTracking.Should().BeFalse("因为Dispose后不应该能启动追踪");
    }

    #endregion

    #region IUserActivityState Interface Tests

    /// <summary>
    /// 测试：UserActivityTracker应该实现IUserActivityState接口
    /// </summary>
    [StaFact]
    public void UserActivityTracker_ShouldImplementIUserActivityState()
    {
        // Arrange
        using var tracker = new UserActivityTracker(_mockLogger, _mockTickService);

        // Assert
        tracker.Should().BeAssignableTo<LYBT.Desktop.Contracts.Services.IUserActivityState>(
            "因为UserActivityTracker应该实现IUserActivityState接口");
    }

    #endregion
}
