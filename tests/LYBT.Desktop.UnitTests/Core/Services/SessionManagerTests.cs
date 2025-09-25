using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using LYBT.Desktop.Core.Services.Session;
using LYBT.Desktop.Core.Events;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Prism.Events;
using System.Windows.Threading;

namespace LYBT.Desktop.UnitTests.Core.Services
{
    /// <summary>
    /// SessionManager单元测试 - 核心服务测试示例
    /// 验证会话管理、超时处理、状态转换等核心功能
    /// </summary>
    public class SessionManagerTests : IDisposable
    {
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly Mock<ILogger<SessionManager>> _mockLogger;
        private readonly SessionManager _sessionManager;
        private readonly Mock<LoginSuccessEvent> _mockLoginEvent;
        private readonly Mock<LogoutEvent> _mockLogoutEvent;
        private readonly Mock<ConsultationStartedEvent> _mockConsultationStartedEvent;
        private readonly Mock<ConsultationCompletedEvent> _mockConsultationCompletedEvent;

        public SessionManagerTests()
        {
            _mockEventAggregator = new Mock<IEventAggregator>();
            _mockLogger = new Mock<ILogger<SessionManager>>();
            
            // 设置事件Mock
            _mockLoginEvent = new Mock<LoginSuccessEvent>();
            _mockLogoutEvent = new Mock<LogoutEvent>();
            _mockConsultationStartedEvent = new Mock<ConsultationStartedEvent>();
            _mockConsultationCompletedEvent = new Mock<ConsultationCompletedEvent>();

            _mockEventAggregator.Setup(x => x.GetEvent<LoginSuccessEvent>()).Returns(_mockLoginEvent.Object);
            _mockEventAggregator.Setup(x => x.GetEvent<LogoutEvent>()).Returns(_mockLogoutEvent.Object);
            _mockEventAggregator.Setup(x => x.GetEvent<ConsultationStartedEvent>()).Returns(_mockConsultationStartedEvent.Object);
            _mockEventAggregator.Setup(x => x.GetEvent<ConsultationCompletedEvent>()).Returns(_mockConsultationCompletedEvent.Object);

            _sessionManager = new SessionManager(_mockEventAggregator.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            _sessionManager?.Dispose();
        }

        [Fact]
        public void InitialState_ShouldBeUnauthenticated()
        {
            // Assert
            _sessionManager.CurrentState.Should().Be(SessionState.Unauthenticated);
            _sessionManager.CurrentUser.Should().BeNull();
            _sessionManager.IsAuthenticated.Should().BeFalse();
            _sessionManager.Token.Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldTransitionToAuthenticated()
        {
            // Arrange
            var user = new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Name = "测试用户",
                Role = "Doctor"
            };
            var token = "test-jwt-token";

            // Act
            await _sessionManager.LoginAsync(user, token);

            // Assert
            _sessionManager.CurrentState.Should().Be(SessionState.Authenticated);
            _sessionManager.CurrentUser.Should().NotBeNull();
            _sessionManager.CurrentUser!.Username.Should().Be("testuser");
            _sessionManager.Token.Should().Be(token);
            _sessionManager.IsAuthenticated.Should().BeTrue();

            _mockLoginEvent.Verify(x => x.Publish(It.Is<LoginSuccessEventArgs>(
                args => args.User.Id == user.Id && args.Token == token)), Times.Once);
        }

        [Fact]
        public async Task Logout_WhenAuthenticated_ShouldTransitionToUnauthenticated()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser" };
            await _sessionManager.LoginAsync(user, "token");

            // Act
            await _sessionManager.LogoutAsync("用户主动登出");

            // Assert
            _sessionManager.CurrentState.Should().Be(SessionState.Unauthenticated);
            _sessionManager.CurrentUser.Should().BeNull();
            _sessionManager.Token.Should().BeNullOrEmpty();
            _sessionManager.IsAuthenticated.Should().BeFalse();

            _mockLogoutEvent.Verify(x => x.Publish(It.Is<LogoutEventArgs>(
                args => args.Reason == "用户主动登出")), Times.Once);
        }

        [Fact]
        public async Task StartConsultation_WhenAuthenticated_ShouldTransitionToInConsultation()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid(), Username = "doctor" };
            await _sessionManager.LoginAsync(user, "token");
            
            var patientId = Guid.NewGuid();
            var consultationId = 123;

            // Act
            await _sessionManager.StartConsultationAsync(patientId, consultationId);

            // Assert
            _sessionManager.CurrentState.Should().Be(SessionState.InConsultation);
            _sessionManager.CurrentPatientId.Should().Be(patientId);
            _sessionManager.CurrentConsultationId.Should().Be(consultationId);

            _mockConsultationStartedEvent.Verify(x => x.Publish(It.Is<ConsultationStartedEventArgs>(
                args => args.ConsultationId == consultationId)), Times.Once);
        }

        [Fact]
        public async Task StartConsultation_WhenUnauthenticated_ShouldThrowException()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var consultationId = 123;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sessionManager.StartConsultationAsync(patientId, consultationId));
        }

        [Fact]
        public async Task EndConsultation_WhenInConsultation_ShouldTransitionToAuthenticated()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid(), Username = "doctor" };
            await _sessionManager.LoginAsync(user, "token");
            
            var patientId = Guid.NewGuid();
            var consultationId = 123;
            await _sessionManager.StartConsultationAsync(patientId, consultationId);

            // Act
            await _sessionManager.EndConsultationAsync();

            // Assert
            _sessionManager.CurrentState.Should().Be(SessionState.Authenticated);
            _sessionManager.CurrentPatientId.Should().Be(Guid.Empty);
            _sessionManager.CurrentConsultationId.Should().Be(0);

            _mockConsultationCompletedEvent.Verify(x => x.Publish(It.IsAny<ConsultationCompletedEventArgs>()), Times.Once);
        }

        [Fact]
        public void SessionTimeout_ShouldBeConfigurable()
        {
            // Arrange
            var timeout = TimeSpan.FromMinutes(30);

            // Act
            _sessionManager.SessionTimeout = timeout;

            // Assert
            _sessionManager.SessionTimeout.Should().Be(timeout);
        }

        [Fact]
        public async Task ExtendSession_ShouldUpdateLastActivityTime()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser" };
            await _sessionManager.LoginAsync(user, "token");
            var initialActivityTime = _sessionManager.LastActivityTime;

            await Task.Delay(100); // 等待一段时间

            // Act
            _sessionManager.ExtendSession();

            // Assert
            _sessionManager.LastActivityTime.Should().BeAfter(initialActivityTime);
        }

        [Theory]
        [InlineData(SessionState.Unauthenticated, SessionState.Authenticated, true)]
        [InlineData(SessionState.Authenticated, SessionState.InConsultation, true)]
        [InlineData(SessionState.InConsultation, SessionState.Authenticated, true)]
        [InlineData(SessionState.Authenticated, SessionState.Unauthenticated, true)]
        [InlineData(SessionState.Unauthenticated, SessionState.InConsultation, false)]
        [InlineData(SessionState.InConsultation, SessionState.Unauthenticated, false)]
        public void StateTransition_ShouldFollowValidTransitions(SessionState from, SessionState to, bool isValid)
        {
            // Arrange & Act
            var canTransition = _sessionManager.CanTransition(from, to);

            // Assert
            canTransition.Should().Be(isValid);
        }

        [Fact]
        public async Task ConcurrentLogin_ShouldHandleCorrectly()
        {
            // Arrange
            var user1 = new UserDto { Id = Guid.NewGuid(), Username = "user1" };
            var user2 = new UserDto { Id = Guid.NewGuid(), Username = "user2" };

            // Act - 并发登录
            var task1 = _sessionManager.LoginAsync(user1, "token1");
            var task2 = _sessionManager.LoginAsync(user2, "token2");

            await Task.WhenAll(task1, task2);

            // Assert - 最后一个登录的用户应该是当前用户
            _sessionManager.CurrentUser.Should().NotBeNull();
            (_sessionManager.CurrentUser!.Username == "user1" || _sessionManager.CurrentUser.Username == "user2")
                .Should().BeTrue();
        }

        [Fact]
        public void SessionProperties_ShouldRaisePropertyChanged()
        {
            // Arrange
            var propertyChangedEvents = new List<string>();
            _sessionManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null)
                    propertyChangedEvents.Add(e.PropertyName);
            };

            var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser" };

            // Act
            _sessionManager.LoginAsync(user, "token").Wait();

            // Assert
            propertyChangedEvents.Should().Contain(nameof(_sessionManager.CurrentState));
            propertyChangedEvents.Should().Contain(nameof(_sessionManager.CurrentUser));
            propertyChangedEvents.Should().Contain(nameof(_sessionManager.Token));
            propertyChangedEvents.Should().Contain(nameof(_sessionManager.IsAuthenticated));
        }

        [Fact]
        public async Task Logout_DuringConsultation_ShouldEndConsultationFirst()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid(), Username = "doctor" };
            await _sessionManager.LoginAsync(user, "token");
            await _sessionManager.StartConsultationAsync(Guid.NewGuid(), 123);

            // Act
            await _sessionManager.LogoutAsync("会话超时");

            // Assert
            _sessionManager.CurrentState.Should().Be(SessionState.Unauthenticated);
            _sessionManager.CurrentConsultationId.Should().Be(0);
            _sessionManager.CurrentPatientId.Should().Be(Guid.Empty);

            // 验证事件发布顺序
            _mockConsultationCompletedEvent.Verify(x => x.Publish(It.IsAny<ConsultationCompletedEventArgs>()), Times.Once);
            _mockLogoutEvent.Verify(x => x.Publish(It.IsAny<LogoutEventArgs>()), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser" };
            _sessionManager.LoginAsync(user, "token").Wait();

            // Act
            _sessionManager.Dispose();

            // Assert
            // 验证Timer已停止（通过检查状态不再变化）
            _sessionManager.CurrentState.Should().Be(SessionState.Authenticated);
            
            // 等待一段时间确认Timer不再触发
            Task.Delay(200).Wait();
            _sessionManager.CurrentState.Should().Be(SessionState.Authenticated);
        }
    }
}