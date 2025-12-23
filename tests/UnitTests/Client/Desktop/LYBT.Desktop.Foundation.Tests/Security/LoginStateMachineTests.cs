using FluentAssertions;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;
using Xunit;

namespace LYBT.Desktop.Foundation.Tests.Security
{
    /// <summary>
    /// LoginStateMachine单元测试
    /// OpenSpec: refactor-login-authentication (Phase 2.1)
    /// </summary>
    public class LoginStateMachineTests
    {
        private readonly ILogger<LoginStateMachine> _logger;

        public LoginStateMachineTests()
        {
            _logger = Substitute.For<ILogger<LoginStateMachine>>();
        }

        private LoginStateMachine CreateStateMachine(LoginState initialState = LoginState.NotLoggedIn)
        {
            var stateMachine = new LoginStateMachine(_logger);

            // 通过触发器将状态机带到目标状态
            switch (initialState)
            {
                case LoginState.NotLoggedIn:
                    break;
                case LoginState.LoggingIn:
                    stateMachine.Fire(LoginTrigger.StartLogin);
                    break;
                case LoginState.AutoLoggingIn:
                    stateMachine.Fire(LoginTrigger.StartAutoLogin);
                    break;
                case LoginState.LoggedIn:
                    stateMachine.Fire(LoginTrigger.StartLogin);
                    stateMachine.Fire(LoginTrigger.LoginSuccess);
                    break;
                case LoginState.LoginFailed:
                    stateMachine.Fire(LoginTrigger.StartLogin);
                    stateMachine.Fire(LoginTrigger.LoginFailure);
                    break;
                case LoginState.LoggingOut:
                    stateMachine.Fire(LoginTrigger.StartLogin);
                    stateMachine.Fire(LoginTrigger.LoginSuccess);
                    stateMachine.Fire(LoginTrigger.StartLogout);
                    break;
                case LoginState.SessionExpired:
                    stateMachine.Fire(LoginTrigger.StartLogin);
                    stateMachine.Fire(LoginTrigger.LoginSuccess);
                    stateMachine.Fire(LoginTrigger.SessionExpire);
                    break;
                case LoginState.TokenRefreshing:
                    stateMachine.Fire(LoginTrigger.StartLogin);
                    stateMachine.Fire(LoginTrigger.LoginSuccess);
                    stateMachine.Fire(LoginTrigger.StartTokenRefresh);
                    break;
            }

            return stateMachine;
        }

        #region 初始状态测试

        [Fact]
        public void Constructor_ShouldStartInNotLoggedInState()
        {
            // Arrange & Act
            var stateMachine = new LoginStateMachine(_logger);

            // Assert
            Assert.Equal(LoginState.NotLoggedIn, stateMachine.CurrentState);
            Assert.False(stateMachine.IsLoggedIn);
            Assert.False(stateMachine.IsTransitioning);
        }

        #endregion

        #region NotLoggedIn状态转换测试

        [Fact]
        public void NotLoggedIn_StartLogin_ShouldTransitionToLoggingIn()
        {
            // Arrange
            var stateMachine = CreateStateMachine();

            // Act
            var result = stateMachine.Fire(LoginTrigger.StartLogin);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.LoggingIn, stateMachine.CurrentState);
            Assert.True(stateMachine.IsTransitioning);
        }

        [Fact]
        public void NotLoggedIn_StartAutoLogin_ShouldTransitionToAutoLoggingIn()
        {
            // Arrange
            var stateMachine = CreateStateMachine();

            // Act
            var result = stateMachine.Fire(LoginTrigger.StartAutoLogin);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.AutoLoggingIn, stateMachine.CurrentState);
            Assert.True(stateMachine.IsTransitioning);
        }

        [Fact]
        public void NotLoggedIn_InvalidTrigger_ShouldReturnFalse()
        {
            // Arrange
            var stateMachine = CreateStateMachine();

            // Act
            var result = stateMachine.Fire(LoginTrigger.LoginSuccess);

            // Assert
            Assert.False(result);
            Assert.Equal(LoginState.NotLoggedIn, stateMachine.CurrentState);
        }

        #endregion

        #region LoggingIn状态转换测试

        [Fact]
        public void LoggingIn_LoginSuccess_ShouldTransitionToLoggedIn()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.LoggingIn);

            // Act
            var result = stateMachine.Fire(LoginTrigger.LoginSuccess);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.LoggedIn, stateMachine.CurrentState);
            Assert.True(stateMachine.IsLoggedIn);
            Assert.False(stateMachine.IsTransitioning);
        }

        [Fact]
        public void LoggingIn_LoginFailure_ShouldTransitionToLoginFailed()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.LoggingIn);

            // Act
            var result = stateMachine.Fire(LoginTrigger.LoginFailure);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.LoginFailed, stateMachine.CurrentState);
        }

        [Fact]
        public void LoggingIn_Reset_ShouldTransitionToNotLoggedIn()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.LoggingIn);

            // Act
            var result = stateMachine.Fire(LoginTrigger.Reset);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.NotLoggedIn, stateMachine.CurrentState);
        }

        #endregion

        #region AutoLoggingIn状态转换测试

        [Fact]
        public void AutoLoggingIn_LoginSuccess_ShouldTransitionToLoggedIn()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.AutoLoggingIn);

            // Act
            var result = stateMachine.Fire(LoginTrigger.LoginSuccess);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.LoggedIn, stateMachine.CurrentState);
        }

        [Fact]
        public void AutoLoggingIn_LoginFailure_ShouldTransitionToNotLoggedIn()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.AutoLoggingIn);

            // Act
            var result = stateMachine.Fire(LoginTrigger.LoginFailure);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.NotLoggedIn, stateMachine.CurrentState);
        }

        #endregion

        #region LoggedIn状态转换测试

        [Fact]
        public void LoggedIn_StartLogout_ShouldTransitionToLoggingOut()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.LoggedIn);

            // Act
            var result = stateMachine.Fire(LoginTrigger.StartLogout);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.LoggingOut, stateMachine.CurrentState);
            Assert.True(stateMachine.IsTransitioning);
        }

        [Fact]
        public void LoggedIn_SessionExpire_ShouldTransitionToSessionExpired()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.LoggedIn);

            // Act
            var result = stateMachine.Fire(LoginTrigger.SessionExpire);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.SessionExpired, stateMachine.CurrentState);
        }

        [Fact]
        public void LoggedIn_StartTokenRefresh_ShouldTransitionToTokenRefreshing()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.LoggedIn);

            // Act
            var result = stateMachine.Fire(LoginTrigger.StartTokenRefresh);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.TokenRefreshing, stateMachine.CurrentState);
            Assert.True(stateMachine.IsTransitioning);
        }

        #endregion

        #region LoginFailed状态转换测试

        [Fact]
        public void LoginFailed_StartLogin_ShouldTransitionToLoggingIn()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.LoginFailed);

            // Act
            var result = stateMachine.Fire(LoginTrigger.StartLogin);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.LoggingIn, stateMachine.CurrentState);
        }

        [Fact]
        public void LoginFailed_Reset_ShouldTransitionToNotLoggedIn()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.LoginFailed);

            // Act
            stateMachine.Reset();

            // Assert
            Assert.Equal(LoginState.NotLoggedIn, stateMachine.CurrentState);
        }

        #endregion

        #region LoggingOut状态转换测试

        [Fact]
        public void LoggingOut_LogoutSuccess_ShouldTransitionToNotLoggedIn()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.LoggingOut);

            // Act
            var result = stateMachine.Fire(LoginTrigger.LogoutSuccess);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.NotLoggedIn, stateMachine.CurrentState);
        }

        [Fact]
        public void LoggingOut_LogoutFailure_ShouldTransitionBackToLoggedIn()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.LoggingOut);

            // Act
            var result = stateMachine.Fire(LoginTrigger.LogoutFailure);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.LoggedIn, stateMachine.CurrentState);
        }

        #endregion

        #region TokenRefreshing状态转换测试

        [Fact]
        public void TokenRefreshing_TokenRefreshSuccess_ShouldTransitionToLoggedIn()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.TokenRefreshing);

            // Act
            var result = stateMachine.Fire(LoginTrigger.TokenRefreshSuccess);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.LoggedIn, stateMachine.CurrentState);
        }

        [Fact]
        public void TokenRefreshing_TokenRefreshFailure_ShouldTransitionToSessionExpired()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.TokenRefreshing);

            // Act
            var result = stateMachine.Fire(LoginTrigger.TokenRefreshFailure);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.SessionExpired, stateMachine.CurrentState);
        }

        #endregion

        #region SessionExpired状态转换测试

        [Fact]
        public void SessionExpired_StartLogin_ShouldTransitionToLoggingIn()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.SessionExpired);

            // Act
            var result = stateMachine.Fire(LoginTrigger.StartLogin);

            // Assert
            Assert.True(result);
            Assert.Equal(LoginState.LoggingIn, stateMachine.CurrentState);
        }

        #endregion

        #region CanFire测试

        [Fact]
        public void CanFire_ValidTransition_ShouldReturnTrue()
        {
            // Arrange
            var stateMachine = CreateStateMachine();

            // Act & Assert
            Assert.True(stateMachine.CanFire(LoginTrigger.StartLogin));
            Assert.True(stateMachine.CanFire(LoginTrigger.StartAutoLogin));
        }

        [Fact]
        public void CanFire_InvalidTransition_ShouldReturnFalse()
        {
            // Arrange
            var stateMachine = CreateStateMachine();

            // Act & Assert
            Assert.False(stateMachine.CanFire(LoginTrigger.LoginSuccess));
            Assert.False(stateMachine.CanFire(LoginTrigger.StartLogout));
        }

        #endregion

        #region 状态变更事件测试

        /// <summary>
        /// 测试：Fire触发PubSubEvent状态变更事件
        /// OpenSpec: unify-event-system (Phase 2.2)
        /// </summary>
        [Fact]
        public async Task Fire_ShouldPublishLoginStateChangedEvent()
        {
            // Arrange
            var eventAggregator = new EventAggregator();
            var stateMachine = new LoginStateMachine(_logger, eventAggregator);
            LoginStateChangedPayload? receivedPayload = null;
            var eventReceived = new TaskCompletionSource<bool>();

            eventAggregator.GetEvent<AuthEvents.LoginStateChangedEvent>().Subscribe(payload =>
            {
                receivedPayload = payload;
                eventReceived.TrySetResult(true);
            });

            // Act
            stateMachine.Fire(LoginTrigger.StartLogin);

            // Assert
            var completed = await Task.WhenAny(eventReceived.Task, Task.Delay(1000));
            completed.Should().Be(eventReceived.Task);
            receivedPayload.Should().NotBeNull();
            receivedPayload!.PreviousState.Should().Be(LoginState.NotLoggedIn);
            receivedPayload.CurrentState.Should().Be(LoginState.LoggingIn);
            receivedPayload.Trigger.Should().Be(LoginTrigger.StartLogin);
            receivedPayload.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// 测试：无EventAggregator时，Fire不应抛出异常
        /// </summary>
        [Fact]
        public void Fire_WithoutEventAggregator_ShouldNotThrow()
        {
            // Arrange
            var stateMachine = new LoginStateMachine(_logger, eventAggregator: null);

            // Act
            var action = () => stateMachine.Fire(LoginTrigger.StartLogin);

            // Assert
            action.Should().NotThrow();
            stateMachine.CurrentState.Should().Be(LoginState.LoggingIn);
        }

        #endregion

        #region 完整登录流程测试

        [Fact]
        public void CompleteLoginFlow_ShouldTransitionCorrectly()
        {
            // Arrange
            var stateMachine = CreateStateMachine();

            // Act & Assert: 完整登录流程
            Assert.Equal(LoginState.NotLoggedIn, stateMachine.CurrentState);

            stateMachine.Fire(LoginTrigger.StartLogin);
            Assert.Equal(LoginState.LoggingIn, stateMachine.CurrentState);

            stateMachine.Fire(LoginTrigger.LoginSuccess);
            Assert.Equal(LoginState.LoggedIn, stateMachine.CurrentState);

            stateMachine.Fire(LoginTrigger.StartLogout);
            Assert.Equal(LoginState.LoggingOut, stateMachine.CurrentState);

            stateMachine.Fire(LoginTrigger.LogoutSuccess);
            Assert.Equal(LoginState.NotLoggedIn, stateMachine.CurrentState);
        }

        [Fact]
        public void TokenRefreshFlow_ShouldTransitionCorrectly()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.LoggedIn);

            // Act & Assert: Token刷新成功
            stateMachine.Fire(LoginTrigger.StartTokenRefresh);
            Assert.Equal(LoginState.TokenRefreshing, stateMachine.CurrentState);

            stateMachine.Fire(LoginTrigger.TokenRefreshSuccess);
            Assert.Equal(LoginState.LoggedIn, stateMachine.CurrentState);
        }

        [Fact]
        public void TokenRefreshFailureFlow_ShouldTransitionToSessionExpired()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.LoggedIn);

            // Act & Assert: Token刷新失败
            stateMachine.Fire(LoginTrigger.StartTokenRefresh);
            Assert.Equal(LoginState.TokenRefreshing, stateMachine.CurrentState);

            stateMachine.Fire(LoginTrigger.TokenRefreshFailure);
            Assert.Equal(LoginState.SessionExpired, stateMachine.CurrentState);

            // 重新登录
            stateMachine.Fire(LoginTrigger.StartLogin);
            Assert.Equal(LoginState.LoggingIn, stateMachine.CurrentState);
        }

        [Fact]
        public void AutoLoginFlow_Success_ShouldTransitionToLoggedIn()
        {
            // Arrange
            var stateMachine = CreateStateMachine();

            // Act & Assert
            stateMachine.Fire(LoginTrigger.StartAutoLogin);
            Assert.Equal(LoginState.AutoLoggingIn, stateMachine.CurrentState);

            stateMachine.Fire(LoginTrigger.LoginSuccess);
            Assert.Equal(LoginState.LoggedIn, stateMachine.CurrentState);
        }

        [Fact]
        public void AutoLoginFlow_Failure_ShouldTransitionToNotLoggedIn()
        {
            // Arrange
            var stateMachine = CreateStateMachine();

            // Act & Assert
            stateMachine.Fire(LoginTrigger.StartAutoLogin);
            Assert.Equal(LoginState.AutoLoggingIn, stateMachine.CurrentState);

            stateMachine.Fire(LoginTrigger.LoginFailure);
            Assert.Equal(LoginState.NotLoggedIn, stateMachine.CurrentState);
        }

        #endregion

        #region GetPermittedTriggers测试

        [Fact]
        public void GetPermittedTriggers_NotLoggedIn_ShouldReturnCorrectTriggers()
        {
            // Arrange
            var stateMachine = CreateStateMachine();

            // Act
            var triggers = stateMachine.GetPermittedTriggers().ToList();

            // Assert
            Assert.Contains(LoginTrigger.StartLogin, triggers);
            Assert.Contains(LoginTrigger.StartAutoLogin, triggers);
            Assert.Equal(2, triggers.Count);
        }

        [Fact]
        public void GetPermittedTriggers_LoggedIn_ShouldReturnCorrectTriggers()
        {
            // Arrange
            var stateMachine = CreateStateMachine(LoginState.LoggedIn);

            // Act
            var triggers = stateMachine.GetPermittedTriggers().ToList();

            // Assert
            Assert.Contains(LoginTrigger.StartLogout, triggers);
            Assert.Contains(LoginTrigger.SessionExpire, triggers);
            Assert.Contains(LoginTrigger.StartTokenRefresh, triggers);
            Assert.Equal(3, triggers.Count);
        }

        #endregion
    }
}
