using FluentAssertions;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Shell.Services.Session;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;

namespace LYBT.Desktop.Shell.Tests.Services.Session;

/// <summary>
/// SessionLifecycleManager 单元测试
/// </summary>
public class SessionLifecycleManagerTests : IDisposable
{
    private readonly Mock<ILogger<SessionLifecycleManager>> _loggerMock;
    private readonly Mock<ITokenLifecycleService> _tokenLifecycleServiceMock;
    private readonly Mock<IUserActivityTracker> _userActivityTrackerMock;
    private readonly Mock<IEventAggregator> _eventAggregatorMock;
    private readonly Mock<TokenLifecycleStateChangedEvent> _tokenLifecycleEventMock;
    private readonly SessionLifecycleManager _sut;

    public SessionLifecycleManagerTests()
    {
        _loggerMock = new Mock<ILogger<SessionLifecycleManager>>();
        _tokenLifecycleServiceMock = new Mock<ITokenLifecycleService>();
        _userActivityTrackerMock = new Mock<IUserActivityTracker>();
        _eventAggregatorMock = new Mock<IEventAggregator>();
        _tokenLifecycleEventMock = new Mock<TokenLifecycleStateChangedEvent>();

        _eventAggregatorMock
            .Setup(ea => ea.GetEvent<TokenLifecycleStateChangedEvent>())
            .Returns(_tokenLifecycleEventMock.Object);

        _sut = new SessionLifecycleManager(
            _loggerMock.Object,
            _tokenLifecycleServiceMock.Object,
            _userActivityTrackerMock.Object,
            _eventAggregatorMock.Object);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    #region 初始状态测试

    [Fact]
    public void Constructor_ShouldInitialize_WithUnauthenticatedState()
    {
        // Assert
        _sut.CurrentState.Should().Be(SessionState.Unauthenticated);
        _sut.IsAuthenticated.Should().BeFalse();
        _sut.CurrentUserName.Should().BeNull();
        _sut.CurrentUserRole.Should().BeNull();
    }

    #endregion

    #region StartSession测试

    [Fact]
    public async Task StartSessionAsync_ShouldTransitionToAuthenticated()
    {
        // Arrange
        var userName = "testuser";
        var userRole = "Doctor";
        var tokenExpiresAt = DateTime.Now.AddHours(1);

        // Act
        await _sut.StartSessionAsync(userName, userRole, tokenExpiresAt);

        // Assert
        _sut.CurrentState.Should().Be(SessionState.Authenticated);
        _sut.IsAuthenticated.Should().BeTrue();
        _sut.CurrentUserName.Should().Be(userName);
        _sut.CurrentUserRole.Should().Be(userRole);
    }

    [Fact]
    public async Task StartSessionAsync_ShouldStartTokenLifecycleService()
    {
        // Arrange
        var userName = "testuser";
        var userRole = "Doctor";
        var tokenExpiresAt = DateTime.Now.AddHours(1);

        // Act
        await _sut.StartSessionAsync(userName, userRole, tokenExpiresAt);

        // Assert
        _tokenLifecycleServiceMock.Verify(s => s.StartMonitoring(tokenExpiresAt), Times.Once);
    }

    [Fact]
    public async Task StartSessionAsync_ShouldStartUserActivityTracker()
    {
        // Arrange
        var userName = "testuser";
        var userRole = "Doctor";
        var tokenExpiresAt = DateTime.Now.AddHours(1);

        // Act
        await _sut.StartSessionAsync(userName, userRole, tokenExpiresAt);

        // Assert
        _userActivityTrackerMock.Verify(t => t.StartTracking(), Times.Once);
    }

    [Fact]
    public async Task StartSessionAsync_ShouldRaiseStateChangedEvent()
    {
        // Arrange
        SessionState? newState = null;
        _sut.StateChanged += (sender, args) => newState = args.CurrentState;

        // Act
        await _sut.StartSessionAsync("testuser", "Doctor", DateTime.Now.AddHours(1));

        // Assert
        newState.Should().Be(SessionState.Authenticated);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task StartSessionAsync_WithInvalidUserName_ShouldThrow(string? invalidUserName)
    {
        // Act
        var act = () => _sut.StartSessionAsync(invalidUserName!, "Doctor", DateTime.Now.AddHours(1));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region EndSession测试

    [Fact]
    public async Task EndSessionAsync_ShouldTransitionToUnauthenticated()
    {
        // Arrange
        await _sut.StartSessionAsync("testuser", "Doctor", DateTime.Now.AddHours(1));

        // Act
        await _sut.EndSessionAsync();

        // Assert
        _sut.CurrentState.Should().Be(SessionState.Unauthenticated);
        _sut.IsAuthenticated.Should().BeFalse();
        _sut.CurrentUserName.Should().BeNull();
        _sut.CurrentUserRole.Should().BeNull();
    }

    [Fact]
    public async Task EndSessionAsync_ShouldStopTokenLifecycleService()
    {
        // Arrange
        await _sut.StartSessionAsync("testuser", "Doctor", DateTime.Now.AddHours(1));

        // Act
        await _sut.EndSessionAsync();

        // Assert
        _tokenLifecycleServiceMock.Verify(s => s.StopMonitoring(), Times.Once);
        _tokenLifecycleServiceMock.Verify(s => s.Reset(), Times.Once);
    }

    [Fact]
    public async Task EndSessionAsync_ShouldStopUserActivityTracker()
    {
        // Arrange
        await _sut.StartSessionAsync("testuser", "Doctor", DateTime.Now.AddHours(1));

        // Act
        await _sut.EndSessionAsync();

        // Assert
        _userActivityTrackerMock.Verify(t => t.StopTracking(), Times.Once);
    }

    #endregion

    #region RefreshToken测试

    [Fact]
    public async Task RefreshTokenAsync_WhenAuthenticated_ShouldTransitionToRefreshing()
    {
        // Arrange
        await _sut.StartSessionAsync("testuser", "Doctor", DateTime.Now.AddHours(1));
        _tokenLifecycleServiceMock.Setup(s => s.TryRefreshTokenAsync()).ReturnsAsync(true);

        SessionState? intermediateState = null;
        _sut.StateChanged += (sender, args) =>
        {
            if (args.CurrentState == SessionState.Refreshing)
                intermediateState = args.CurrentState;
        };

        // Act
        await _sut.RefreshTokenAsync();

        // Assert
        intermediateState.Should().Be(SessionState.Refreshing);
    }

    [Fact]
    public async Task RefreshTokenAsync_Success_ShouldReturnToAuthenticated()
    {
        // Arrange
        await _sut.StartSessionAsync("testuser", "Doctor", DateTime.Now.AddHours(1));
        _tokenLifecycleServiceMock.Setup(s => s.TryRefreshTokenAsync()).ReturnsAsync(true);

        // Act
        var result = await _sut.RefreshTokenAsync();

        // Assert
        result.Should().BeTrue();
        _sut.CurrentState.Should().Be(SessionState.Authenticated);
    }

    [Fact]
    public async Task RefreshTokenAsync_Failure_ShouldTransitionToExpired()
    {
        // Arrange
        await _sut.StartSessionAsync("testuser", "Doctor", DateTime.Now.AddHours(1));
        _tokenLifecycleServiceMock.Setup(s => s.TryRefreshTokenAsync()).ReturnsAsync(false);

        // Act
        var result = await _sut.RefreshTokenAsync();

        // Assert
        result.Should().BeFalse();
        _sut.CurrentState.Should().Be(SessionState.Expired);
    }

    #endregion

    #region UpdateTokenExpiration测试

    [Fact]
    public async Task UpdateTokenExpiration_ShouldUpdateTokenLifecycleService()
    {
        // Arrange
        await _sut.StartSessionAsync("testuser", "Doctor", DateTime.Now.AddHours(1));
        var newExpiration = DateTime.Now.AddHours(2);

        // Act
        _sut.UpdateTokenExpiration(newExpiration);

        // Assert
        _tokenLifecycleServiceMock.Verify(s => s.UpdateExpiration(newExpiration), Times.Once);
    }

    #endregion

    #region RecordUserActivity测试

    [Fact]
    public async Task RecordUserActivity_ShouldCallUserActivityTracker()
    {
        // Arrange
        await _sut.StartSessionAsync("testuser", "Doctor", DateTime.Now.AddHours(1));

        // Act
        _sut.RecordUserActivity();

        // Assert
        _userActivityTrackerMock.Verify(t => t.ResetActivity(), Times.Once);
    }

    #endregion

    #region TokenRemainingTime测试

    [Fact]
    public void TokenRemainingTime_ShouldReturnValueFromTokenLifecycleService()
    {
        // Arrange
        var remainingTime = TimeSpan.FromMinutes(30);
        _tokenLifecycleServiceMock.Setup(s => s.RemainingTime).Returns(remainingTime);

        // Act
        var result = _sut.TokenRemainingTime;

        // Assert
        result.Should().Be(remainingTime);
    }

    #endregion

    #region Diagnostics测试

    [Fact]
    public async Task GetDiagnostics_ShouldReturnSessionInfo()
    {
        // Arrange
        var userName = "testuser";
        var userRole = "Doctor";
        await _sut.StartSessionAsync(userName, userRole, DateTime.Now.AddHours(1));

        // Act
        var diagnostics = _sut.GetDiagnostics();

        // Assert
        diagnostics.CurrentState.Should().Be(SessionState.Authenticated);
        diagnostics.UserName.Should().Be(userName);
        diagnostics.UserRole.Should().Be(userRole);
        diagnostics.SessionStartTime.Should().NotBeNull();
    }

    #endregion

    #region SessionExpiring/Expired事件测试

    [Fact]
    public async Task SessionExpiring_FromUserActivity_ShouldRaiseEvent()
    {
        // Arrange
        await _sut.StartSessionAsync("testuser", "Doctor", DateTime.Now.AddHours(1));

        SessionExpiringWarningEventArgs? warningArgs = null;
        _sut.SessionExpiring += (sender, args) => warningArgs = args;

        // Act - 模拟UserActivityTracker触发SessionExpiring事件
        _userActivityTrackerMock.Raise(
            t => t.SessionExpiring += null,
            new SessionExpiringEventArgs { RemainingTime = TimeSpan.FromMinutes(1) });

        // Assert
        warningArgs.Should().NotBeNull();
        warningArgs!.DueToInactivity.Should().BeTrue();
    }

    [Fact]
    public async Task SessionExpired_FromUserActivity_ShouldTransitionToExpiredState()
    {
        // Arrange
        await _sut.StartSessionAsync("testuser", "Doctor", DateTime.Now.AddHours(1));

        var expiredRaised = false;
        _sut.SessionExpired += (sender, args) => expiredRaised = true;

        // Act - 模拟UserActivityTracker触发SessionExpired事件
        _userActivityTrackerMock.Raise(t => t.SessionExpired += null, EventArgs.Empty);

        // Assert
        _sut.CurrentState.Should().Be(SessionState.Expired);
        expiredRaised.Should().BeTrue();
    }

    #endregion

    #region Dispose测试

    [Fact]
    public void Dispose_ShouldUnsubscribeFromEvents()
    {
        // Act
        _sut.Dispose();

        // Assert - 再次Dispose不应抛出异常
        var act = () => _sut.Dispose();
        act.Should().NotThrow();
    }

    #endregion
}
