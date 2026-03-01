using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Shell.Services.Session;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;

namespace LYBT.Desktop.Shell.Tests.Services.Session;

/// <summary>
/// SessionLifecycleManager 单元测试
/// </summary>
public class SessionLifecycleManagerTests : IDisposable
{
    private readonly ILogger<SessionLifecycleManager> _logger;
    private readonly ITokenLifecycleService _tokenLifecycleService;
    private readonly IUserActivityTracker _userActivityTracker;
    private readonly IEventAggregator _eventAggregator;
    private readonly TokenLifecycleStateChangedEvent _tokenLifecycleEvent;
    private readonly SessionLifecycleManager _sut;

    public SessionLifecycleManagerTests()
    {
        _logger = Substitute.For<ILogger<SessionLifecycleManager>>();
        _tokenLifecycleService = Substitute.For<ITokenLifecycleService>();
        _userActivityTracker = Substitute.For<IUserActivityTracker>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _tokenLifecycleEvent = Substitute.For<TokenLifecycleStateChangedEvent>();

        _eventAggregator
            .GetEvent<TokenLifecycleStateChangedEvent>()
            .Returns(_tokenLifecycleEvent);

        _sut = new SessionLifecycleManager(
            _logger,
            _tokenLifecycleService,
            _userActivityTracker,
            _eventAggregator);
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
        _tokenLifecycleService.Received(1).StartMonitoring(tokenExpiresAt);
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
        _userActivityTracker.Received(1).StartTracking();
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
        _tokenLifecycleService.Received(1).StopMonitoring();
        _tokenLifecycleService.Received(1).Reset();
    }

    [Fact]
    public async Task EndSessionAsync_ShouldStopUserActivityTracker()
    {
        // Arrange
        await _sut.StartSessionAsync("testuser", "Doctor", DateTime.Now.AddHours(1));

        // Act
        await _sut.EndSessionAsync();

        // Assert
        _userActivityTracker.Received(1).StopTracking();
    }

    #endregion

    #region RefreshToken测试

    [Fact]
    public async Task RefreshTokenAsync_WhenAuthenticated_ShouldTransitionToRefreshing()
    {
        // Arrange
        await _sut.StartSessionAsync("testuser", "Doctor", DateTime.Now.AddHours(1));
        _tokenLifecycleService.TryRefreshTokenAsync().Returns(true);

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
        _tokenLifecycleService.TryRefreshTokenAsync().Returns(true);

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
        _tokenLifecycleService.TryRefreshTokenAsync().Returns(false);

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
        _tokenLifecycleService.Received(1).UpdateExpiration(newExpiration);
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
        _userActivityTracker.Received(1).ResetActivity();
    }

    #endregion

    #region TokenRemainingTime测试

    [Fact]
    public void TokenRemainingTime_ShouldReturnValueFromTokenLifecycleService()
    {
        // Arrange
        var remainingTime = TimeSpan.FromMinutes(30);
        _tokenLifecycleService.RemainingTime.Returns(remainingTime);

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

    #region SessionExpired事件测试

    // OpenSpec: simplify-auth-architecture - SessionExpiring测试已移除

    [Fact]
    public async Task SessionExpired_FromUserActivity_ShouldTransitionToExpiredState()
    {
        // Arrange
        await _sut.StartSessionAsync("testuser", "Doctor", DateTime.Now.AddHours(1));

        var expiredRaised = false;
        _sut.SessionExpired += (sender, args) => expiredRaised = true;

        // Act - 模拟UserActivityTracker触发SessionExpired事件
        _userActivityTracker.SessionExpired += Raise.Event();

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
