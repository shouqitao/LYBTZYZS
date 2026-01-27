using FluentAssertions;
using LYBT.Desktop.Foundation.Security;
using Prism.Events;

namespace LYBT.Desktop.Foundation.Tests.Security;

/// <summary>
/// TokenEvents单元测试
/// OpenSpec: unify-event-system (EVENT-005)
/// </summary>
public class TokenEventsTests
{
    private readonly EventAggregator _eventAggregator;

    public TokenEventsTests()
    {
        _eventAggregator = new EventAggregator();
    }

    #region RefreshSucceededEvent测试

    /// <summary>
    /// 测试：RefreshSucceededEvent可正常订阅和发布
    /// </summary>
    [Fact]
    public void RefreshSucceededEvent_CanSubscribeAndPublish()
    {
        // Arrange
        TokenRefreshSucceededPayload? receivedPayload = null;
        var refreshEvent = _eventAggregator.GetEvent<TokenEvents.RefreshSucceededEvent>();
        refreshEvent.Subscribe(payload => receivedPayload = payload);

        var payload = new TokenRefreshSucceededPayload
        {
            NewExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        refreshEvent.Publish(payload);

        // Assert
        receivedPayload.Should().NotBeNull();
        receivedPayload!.NewExpiresAt.Should().Be(payload.NewExpiresAt);
        receivedPayload.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region RefreshFailedEvent测试

    /// <summary>
    /// 测试：RefreshFailedEvent可正常订阅和发布
    /// </summary>
    [Fact]
    public void RefreshFailedEvent_CanSubscribeAndPublish()
    {
        // Arrange
        TokenRefreshFailedPayload? receivedPayload = null;
        var failedEvent = _eventAggregator.GetEvent<TokenEvents.RefreshFailedEvent>();
        failedEvent.Subscribe(payload => receivedPayload = payload);

        var payload = new TokenRefreshFailedPayload
        {
            Reason = TokenRefreshFailureReason.NetworkError,
            UserMessage = "网络连接失败",
            IsRetryable = true,
            RequiresReLogin = false
        };

        // Act
        failedEvent.Publish(payload);

        // Assert
        receivedPayload.Should().NotBeNull();
        receivedPayload!.Reason.Should().Be(TokenRefreshFailureReason.NetworkError);
        receivedPayload.UserMessage.Should().Be("网络连接失败");
        receivedPayload.IsRetryable.Should().BeTrue();
        receivedPayload.RequiresReLogin.Should().BeFalse();
    }

    #endregion

    #region LifecycleChangedEvent测试

    /// <summary>
    /// 测试：LifecycleChangedEvent可正常订阅和发布
    /// </summary>
    [Fact]
    public void LifecycleChangedEvent_CanSubscribeAndPublish()
    {
        // Arrange
        TokenLifecycleChangedPayload? receivedPayload = null;
        var lifecycleEvent = _eventAggregator.GetEvent<TokenEvents.LifecycleChangedEvent>();
        lifecycleEvent.Subscribe(payload => receivedPayload = payload);

        var payload = new TokenLifecycleChangedPayload
        {
            PreviousState = TokenLifecycleState.Active,
            CurrentState = TokenLifecycleState.Warning,
            RemainingTime = TimeSpan.FromMinutes(5)
        };

        // Act
        lifecycleEvent.Publish(payload);

        // Assert
        receivedPayload.Should().NotBeNull();
        receivedPayload!.PreviousState.Should().Be(TokenLifecycleState.Active);
        receivedPayload.CurrentState.Should().Be(TokenLifecycleState.Warning);
        receivedPayload.RemainingTime.Should().Be(TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// 测试：TokenLifecycleChangedPayload的RequiresUserInteraction属性
    /// </summary>
    [Theory]
    [InlineData(TokenLifecycleState.NotAuthenticated, false)]
    [InlineData(TokenLifecycleState.Active, false)]
    [InlineData(TokenLifecycleState.Warning, true)]
    [InlineData(TokenLifecycleState.Expired, false)]
    public void TokenLifecycleChangedPayload_RequiresUserInteraction_BasedOnState(
        TokenLifecycleState currentState,
        bool expectedRequiresInteraction)
    {
        // Arrange
        var payload = new TokenLifecycleChangedPayload
        {
            PreviousState = TokenLifecycleState.Active,
            CurrentState = currentState
        };

        // Assert
        payload.RequiresUserInteraction.Should().Be(expectedRequiresInteraction);
    }

    /// <summary>
    /// 测试：TokenLifecycleChangedPayload的RequiresReLogin属性
    /// </summary>
    [Theory]
    [InlineData(TokenLifecycleState.NotAuthenticated, false)]
    [InlineData(TokenLifecycleState.Active, false)]
    [InlineData(TokenLifecycleState.Warning, false)]
    [InlineData(TokenLifecycleState.Expired, true)]
    public void TokenLifecycleChangedPayload_RequiresReLogin_BasedOnState(
        TokenLifecycleState currentState,
        bool expectedRequiresReLogin)
    {
        // Arrange
        var payload = new TokenLifecycleChangedPayload
        {
            PreviousState = TokenLifecycleState.Active,
            CurrentState = currentState
        };

        // Assert
        payload.RequiresReLogin.Should().Be(expectedRequiresReLogin);
    }

    /// <summary>
    /// 测试：TokenLifecycleChangedPayload默认Timestamp为当前时间
    /// </summary>
    [Fact]
    public void TokenLifecycleChangedPayload_HasDefaultTimestamp()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var payload = new TokenLifecycleChangedPayload
        {
            PreviousState = TokenLifecycleState.Active,
            CurrentState = TokenLifecycleState.Warning
        };

        // Assert
        payload.Timestamp.Should().BeOnOrAfter(beforeCreate);
        payload.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    // OpenSpec: simplify-auth-architecture - ExpiringEvent测试已移除

    #region ExpiredEvent测试

    /// <summary>
    /// 测试：ExpiredEvent可正常订阅和发布
    /// </summary>
    [Fact]
    public void ExpiredEvent_CanSubscribeAndPublish()
    {
        // Arrange
        SessionExpiredPayload? receivedPayload = null;
        var expiredEvent = _eventAggregator.GetEvent<TokenEvents.ExpiredEvent>();
        expiredEvent.Subscribe(payload => receivedPayload = payload);

        var payload = new SessionExpiredPayload
        {
            Reason = SessionExpiredReason.TokenExpired,
            UserName = "testuser"
        };

        // Act
        expiredEvent.Publish(payload);

        // Assert
        receivedPayload.Should().NotBeNull();
        receivedPayload!.Reason.Should().Be(SessionExpiredReason.TokenExpired);
        receivedPayload.UserName.Should().Be("testuser");
    }

    #endregion

    #region 多订阅者测试

    /// <summary>
    /// 测试：多个订阅者都能接收到事件
    /// </summary>
    [Fact]
    public void TokenEvents_MultipleSubscribers_AllReceiveEvent()
    {
        // Arrange
        var receivedCount = 0;
        var refreshEvent = _eventAggregator.GetEvent<TokenEvents.RefreshSucceededEvent>();
        refreshEvent.Subscribe(_ => receivedCount++);
        refreshEvent.Subscribe(_ => receivedCount++);
        refreshEvent.Subscribe(_ => receivedCount++);

        var payload = new TokenRefreshSucceededPayload
        {
            NewExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        refreshEvent.Publish(payload);

        // Assert
        receivedCount.Should().Be(3);
    }

    /// <summary>
    /// 测试：取消订阅后不再接收事件
    /// </summary>
    [Fact]
    public void TokenEvents_Unsubscribe_NoLongerReceivesEvent()
    {
        // Arrange
        var receivedCount = 0;
        var refreshEvent = _eventAggregator.GetEvent<TokenEvents.RefreshSucceededEvent>();
        var token = refreshEvent.Subscribe(_ => receivedCount++);

        var payload = new TokenRefreshSucceededPayload
        {
            NewExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        refreshEvent.Publish(payload); // 第一次发布
        token.Dispose();
        refreshEvent.Publish(payload); // 第二次发布

        // Assert
        receivedCount.Should().Be(1); // 只接收到第一次
    }

    #endregion
}
