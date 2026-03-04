using FluentAssertions;
using LYBT.Desktop.Contracts.Security;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Foundation.Security;

/// <summary>
/// AuthenticationStateMachine单元测试
/// OpenSpec: refactor-auth-role-system (Phase 1.1)
/// 测试统一认证状态机的状态转换和事件发布
/// </summary>
public class AuthenticationStateMachineTests
{
    private readonly ILogger<AuthenticationStateMachine> _logger;

    public AuthenticationStateMachineTests()
    {
        _logger = Substitute.For<ILogger<AuthenticationStateMachine>>();
    }

    private AuthenticationStateMachine CreateStateMachine(AuthState initialState = AuthState.Idle)
    {
        var stateMachine = new AuthenticationStateMachine(_logger, initialState);
        return stateMachine;
    }

    #region 初始状态测试

    [Fact]
    public void Constructor_ShouldStartInIdleState()
    {
        // Arrange & Act
        var stateMachine = new AuthenticationStateMachine(_logger);

        // Assert
        Assert.Equal(AuthState.Idle, stateMachine.CurrentState);
        Assert.False(stateMachine.IsAuthenticated);
        Assert.False(stateMachine.IsTransitioning);
    }

    #endregion

    #region Idle状态转换测试

    [Fact]
    public void Idle_StartLogin_ShouldTransitionToAuthenticating()
    {
        // Arrange
        var stateMachine = CreateStateMachine();

        // Act
        var result = stateMachine.Fire(AuthEvent.StartLogin);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.Authenticating, stateMachine.CurrentState);
        Assert.True(stateMachine.IsTransitioning);
    }

    [Fact]
    public void Idle_StartAutoLogin_ShouldTransitionToValidatingToken()
    {
        // Arrange
        var stateMachine = CreateStateMachine();

        // Act
        var result = stateMachine.Fire(AuthEvent.StartAutoLogin);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.ValidatingToken, stateMachine.CurrentState);
        Assert.True(stateMachine.IsTransitioning);
    }

    [Fact]
    public void Idle_InvalidEvent_ShouldReturnFalse()
    {
        // Arrange
        var stateMachine = CreateStateMachine();

        // Act
        var result = stateMachine.Fire(AuthEvent.LoginFailure);

        // Assert
        Assert.False(result);
        Assert.Equal(AuthState.Idle, stateMachine.CurrentState);
    }

    #endregion

    #region Authenticating状态转换测试

    [Fact]
    public void Authenticating_CredentialsValidated_ShouldTransitionToLoadingProfile()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.Authenticating);

        // Act
        var result = stateMachine.Fire(AuthEvent.CredentialsValidated);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.LoadingProfile, stateMachine.CurrentState);
        Assert.True(stateMachine.IsTransitioning);
    }

    [Fact]
    public void Authenticating_LoginFailure_ShouldTransitionToFailed()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.Authenticating);

        // Act
        var result = stateMachine.Fire(AuthEvent.LoginFailure);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.Failed, stateMachine.CurrentState);
    }

    [Fact]
    public void Authenticating_Reset_ShouldTransitionToIdle()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.Authenticating);

        // Act
        var result = stateMachine.Fire(AuthEvent.Reset);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.Idle, stateMachine.CurrentState);
    }

    #endregion

    #region ValidatingToken状态转换测试

    [Fact]
    public void ValidatingToken_TokenValidated_ShouldTransitionToLoadingProfile()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.ValidatingToken);

        // Act
        var result = stateMachine.Fire(AuthEvent.TokenValidated);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.LoadingProfile, stateMachine.CurrentState);
    }

    [Fact]
    public void ValidatingToken_LoginFailure_ShouldTransitionToIdle()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.ValidatingToken);

        // Act
        var result = stateMachine.Fire(AuthEvent.LoginFailure);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.Idle, stateMachine.CurrentState);
    }

    #endregion

    #region LoadingProfile状态转换测试

    [Fact]
    public void LoadingProfile_ProfileLoaded_ShouldTransitionToLoadingModules()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.LoadingProfile);

        // Act
        var result = stateMachine.Fire(AuthEvent.ProfileLoaded);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.LoadingModules, stateMachine.CurrentState);
    }

    #endregion

    #region LoadingModules状态转换测试

    [Fact]
    public void LoadingModules_ModulesLoaded_ShouldTransitionToNavigating()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.LoadingModules);

        // Act
        var result = stateMachine.Fire(AuthEvent.ModulesLoaded);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.Navigating, stateMachine.CurrentState);
    }

    #endregion

    #region Navigating状态转换测试

    [Fact]
    public void Navigating_NavigationCompleted_ShouldTransitionToAuthenticated()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.Navigating);

        // Act
        var result = stateMachine.Fire(AuthEvent.NavigationCompleted);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.Authenticated, stateMachine.CurrentState);
        Assert.True(stateMachine.IsAuthenticated);
        Assert.False(stateMachine.IsTransitioning);
    }

    #endregion

    #region Authenticated状态转换测试

    [Fact]
    public void Authenticated_StartLogout_ShouldTransitionToLoggingOut()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.Authenticated);

        // Act
        var result = stateMachine.Fire(AuthEvent.StartLogout);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.LoggingOut, stateMachine.CurrentState);
        Assert.True(stateMachine.IsTransitioning);
    }

    [Fact]
    public void Authenticated_SessionExpire_ShouldTransitionToSessionExpired()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.Authenticated);

        // Act
        var result = stateMachine.Fire(AuthEvent.SessionExpire);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.SessionExpired, stateMachine.CurrentState);
    }

    [Fact]
    public void Authenticated_StartTokenRefresh_ShouldTransitionToRefreshingToken()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.Authenticated);

        // Act
        var result = stateMachine.Fire(AuthEvent.StartTokenRefresh);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.RefreshingToken, stateMachine.CurrentState);
        Assert.True(stateMachine.IsTransitioning);
    }

    #endregion

    #region Failed状态转换测试

    [Fact]
    public void Failed_StartLogin_ShouldTransitionToAuthenticating()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.Failed);

        // Act
        var result = stateMachine.Fire(AuthEvent.StartLogin);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.Authenticating, stateMachine.CurrentState);
    }

    [Fact]
    public void Failed_Reset_ShouldTransitionToIdle()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.Failed);

        // Act
        stateMachine.Reset();

        // Assert
        Assert.Equal(AuthState.Idle, stateMachine.CurrentState);
    }

    #endregion

    #region LoggingOut状态转换测试

    [Fact]
    public void LoggingOut_LogoutSuccess_ShouldTransitionToIdle()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.LoggingOut);

        // Act
        var result = stateMachine.Fire(AuthEvent.LogoutSuccess);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.Idle, stateMachine.CurrentState);
    }

    [Fact]
    public void LoggingOut_LogoutFailure_ShouldTransitionBackToAuthenticated()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.LoggingOut);

        // Act
        var result = stateMachine.Fire(AuthEvent.LogoutFailure);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.Authenticated, stateMachine.CurrentState);
    }

    #endregion

    #region RefreshingToken状态转换测试

    [Fact]
    public void RefreshingToken_TokenRefreshSuccess_ShouldTransitionToAuthenticated()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.RefreshingToken);

        // Act
        var result = stateMachine.Fire(AuthEvent.TokenRefreshSuccess);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.Authenticated, stateMachine.CurrentState);
    }

    [Fact]
    public void RefreshingToken_TokenRefreshFailure_ShouldTransitionToSessionExpired()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.RefreshingToken);

        // Act
        var result = stateMachine.Fire(AuthEvent.TokenRefreshFailure);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.SessionExpired, stateMachine.CurrentState);
    }

    #endregion

    #region SessionExpired状态转换测试

    [Fact]
    public void SessionExpired_StartLogin_ShouldTransitionToAuthenticating()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.SessionExpired);

        // Act
        var result = stateMachine.Fire(AuthEvent.StartLogin);

        // Assert
        Assert.True(result);
        Assert.Equal(AuthState.Authenticating, stateMachine.CurrentState);
    }

    #endregion

    #region CanFire测试

    [Fact]
    public void CanFire_ValidTransition_ShouldReturnTrue()
    {
        // Arrange
        var stateMachine = CreateStateMachine();

        // Act & Assert
        Assert.True(stateMachine.CanFire(AuthEvent.StartLogin));
        Assert.True(stateMachine.CanFire(AuthEvent.StartAutoLogin));
    }

    [Fact]
    public void CanFire_InvalidTransition_ShouldReturnFalse()
    {
        // Arrange
        var stateMachine = CreateStateMachine();

        // Act & Assert
        Assert.False(stateMachine.CanFire(AuthEvent.CredentialsValidated));
        Assert.False(stateMachine.CanFire(AuthEvent.StartLogout));
    }

    #endregion

    #region 状态变更事件测试

    /// <summary>
    /// 测试：Fire触发PubSubEvent状态变更事件
    /// OpenSpec: refactor-auth-role-system (Phase 1.1)
    /// </summary>
    [Fact]
    public async Task Fire_ShouldPublishAuthStateChangedEvent()
    {
        // Arrange
        var eventAggregator = new EventAggregator();
        var stateMachine = new AuthenticationStateMachine(_logger, eventAggregator);
        AuthStateChangedEventArgs? receivedArgs = null;
        var eventReceived = new TaskCompletionSource<bool>();

        eventAggregator.GetEvent<AuthStateChangedPubSubEvent>().Subscribe(args =>
        {
            receivedArgs = args;
            eventReceived.TrySetResult(true);
        });

        // Act
        stateMachine.Fire(AuthEvent.StartLogin);

        // Assert
        var completed = await Task.WhenAny(eventReceived.Task, Task.Delay(1000));
        completed.Should().Be(eventReceived.Task);
        receivedArgs.Should().NotBeNull();
        receivedArgs!.PreviousState.Should().Be(AuthState.Idle);
        receivedArgs.CurrentState.Should().Be(AuthState.Authenticating);
        receivedArgs.Trigger.Should().Be(AuthEvent.StartLogin);
        receivedArgs.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// 测试：无EventAggregator时，Fire不应抛出异常
    /// </summary>
    [Fact]
    public void Fire_WithoutEventAggregator_ShouldNotThrow()
    {
        // Arrange
        var stateMachine = new AuthenticationStateMachine(_logger, eventAggregator: null);

        // Act
        var action = () => stateMachine.Fire(AuthEvent.StartLogin);

        // Assert
        action.Should().NotThrow();
        stateMachine.CurrentState.Should().Be(AuthState.Authenticating);
    }

    #endregion

    #region 完整登录流程测试

    [Fact]
    public void CompleteLoginFlow_ShouldTransitionCorrectly()
    {
        // Arrange
        var stateMachine = CreateStateMachine();

        // Act & Assert: 完整登录流程
        Assert.Equal(AuthState.Idle, stateMachine.CurrentState);

        stateMachine.Fire(AuthEvent.StartLogin);
        Assert.Equal(AuthState.Authenticating, stateMachine.CurrentState);

        stateMachine.Fire(AuthEvent.CredentialsValidated);
        Assert.Equal(AuthState.LoadingProfile, stateMachine.CurrentState);

        stateMachine.Fire(AuthEvent.ProfileLoaded);
        Assert.Equal(AuthState.LoadingModules, stateMachine.CurrentState);

        stateMachine.Fire(AuthEvent.ModulesLoaded);
        Assert.Equal(AuthState.Navigating, stateMachine.CurrentState);

        stateMachine.Fire(AuthEvent.NavigationCompleted);
        Assert.Equal(AuthState.Authenticated, stateMachine.CurrentState);

        stateMachine.Fire(AuthEvent.StartLogout);
        Assert.Equal(AuthState.LoggingOut, stateMachine.CurrentState);

        stateMachine.Fire(AuthEvent.LogoutSuccess);
        Assert.Equal(AuthState.Idle, stateMachine.CurrentState);
    }

    [Fact]
    public void TokenRefreshFlow_ShouldTransitionCorrectly()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.Authenticated);

        // Act & Assert: Token刷新成功
        stateMachine.Fire(AuthEvent.StartTokenRefresh);
        Assert.Equal(AuthState.RefreshingToken, stateMachine.CurrentState);

        stateMachine.Fire(AuthEvent.TokenRefreshSuccess);
        Assert.Equal(AuthState.Authenticated, stateMachine.CurrentState);
    }

    [Fact]
    public void TokenRefreshFailureFlow_ShouldTransitionToSessionExpired()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.Authenticated);

        // Act & Assert: Token刷新失败
        stateMachine.Fire(AuthEvent.StartTokenRefresh);
        Assert.Equal(AuthState.RefreshingToken, stateMachine.CurrentState);

        stateMachine.Fire(AuthEvent.TokenRefreshFailure);
        Assert.Equal(AuthState.SessionExpired, stateMachine.CurrentState);

        // 重新登录
        stateMachine.Fire(AuthEvent.StartLogin);
        Assert.Equal(AuthState.Authenticating, stateMachine.CurrentState);
    }

    [Fact]
    public void AutoLoginFlow_Success_ShouldTransitionToAuthenticated()
    {
        // Arrange
        var stateMachine = CreateStateMachine();

        // Act & Assert
        stateMachine.Fire(AuthEvent.StartAutoLogin);
        Assert.Equal(AuthState.ValidatingToken, stateMachine.CurrentState);

        stateMachine.Fire(AuthEvent.TokenValidated);
        Assert.Equal(AuthState.LoadingProfile, stateMachine.CurrentState);

        stateMachine.Fire(AuthEvent.ProfileLoaded);
        Assert.Equal(AuthState.LoadingModules, stateMachine.CurrentState);

        stateMachine.Fire(AuthEvent.ModulesLoaded);
        Assert.Equal(AuthState.Navigating, stateMachine.CurrentState);

        stateMachine.Fire(AuthEvent.NavigationCompleted);
        Assert.Equal(AuthState.Authenticated, stateMachine.CurrentState);
    }

    [Fact]
    public void AutoLoginFlow_Failure_ShouldTransitionToIdle()
    {
        // Arrange
        var stateMachine = CreateStateMachine();

        // Act & Assert
        stateMachine.Fire(AuthEvent.StartAutoLogin);
        Assert.Equal(AuthState.ValidatingToken, stateMachine.CurrentState);

        stateMachine.Fire(AuthEvent.LoginFailure);
        Assert.Equal(AuthState.Idle, stateMachine.CurrentState);
    }

    #endregion

    #region GetPermittedEvents测试

    [Fact]
    public void GetPermittedEvents_Idle_ShouldReturnCorrectEvents()
    {
        // Arrange
        var stateMachine = CreateStateMachine();

        // Act
        var events = stateMachine.GetPermittedEvents().ToList();

        // Assert
        Assert.Contains(AuthEvent.StartLogin, events);
        Assert.Contains(AuthEvent.StartAutoLogin, events);
        Assert.Equal(2, events.Count);
    }

    [Fact]
    public void GetPermittedEvents_Authenticated_ShouldReturnCorrectEvents()
    {
        // Arrange
        var stateMachine = CreateStateMachine(AuthState.Authenticated);

        // Act
        var events = stateMachine.GetPermittedEvents().ToList();

        // Assert
        Assert.Contains(AuthEvent.StartLogout, events);
        Assert.Contains(AuthEvent.SessionExpire, events);
        Assert.Contains(AuthEvent.StartTokenRefresh, events);
        Assert.Equal(3, events.Count);
    }

    #endregion
}
