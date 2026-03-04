using System.Net.Http;
using FluentAssertions;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Security;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Prism.Events;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Foundation.Security;

/// <summary>
/// LogoutService单元测试
/// OpenSpec: refactor-login-authentication (Phase 2.3)
/// OpenSpec: unify-event-system (Phase 2.3)
/// OpenSpec: refactor-auth-role-system (Phase 1.1) - 更新为使用IAuthenticationStateMachine
/// </summary>
public class LogoutServiceTests : IDisposable
{
    private readonly ILogger<LogoutService> _logger;
    private readonly ITokenStorageService _tokenStorage;
    private readonly IAuthApi _authApi;
    private readonly IAuthenticationStateMachine _stateMachine;
    private readonly IEventAggregator _eventAggregator;
    private readonly LogoutService _sut;

    public LogoutServiceTests()
    {
        _logger = Substitute.For<ILogger<LogoutService>>();
        _tokenStorage = Substitute.For<ITokenStorageService>();
        _authApi = Substitute.For<IAuthApi>();
        _stateMachine = Substitute.For<IAuthenticationStateMachine>();
        _eventAggregator = new EventAggregator();

        _stateMachine.Fire(Arg.Any<AuthEvent>(), Arg.Any<string?>()).Returns(true);

        _sut = new LogoutService(_logger, _tokenStorage, _authApi, _stateMachine, _eventAggregator);
    }

    public void Dispose()
    {
        _sut.Dispose();
        GC.SuppressFinalize(this);
    }

    #region 构造函数测试

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new LogoutService(null!, _tokenStorage, _authApi, _stateMachine));
    }

    [Fact]
    public void Constructor_WithNullTokenStorage_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new LogoutService(_logger, null!, _authApi, _stateMachine));
    }

    [Fact]
    public void Constructor_WithNullAuthApi_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new LogoutService(_logger, _tokenStorage, null!, _stateMachine));
    }

    [Fact]
    public void Constructor_WithNullStateMachine_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new LogoutService(_logger, _tokenStorage, _authApi, null!));
    }

    #endregion

    #region LogoutAsync测试

    [Fact]
    public async Task LogoutAsync_Success_ShouldReturnFullSuccess()
    {
        // Arrange
        SetupSuccessfulLogout();

        // Act
        var result = await _sut.LogoutAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(result.LocalLogoutCompleted);
        Assert.True(result.ServerLogoutCompleted);
        Assert.False(result.ServerLogoutQueued);
    }

    [Fact]
    public async Task LogoutAsync_ShouldTriggerStateMachineStartLogout()
    {
        // Arrange
        SetupSuccessfulLogout();

        // Act
        await _sut.LogoutAsync();

        // Assert
        _stateMachine.Received(1).Fire(AuthEvent.StartLogout, Arg.Any<string?>());
    }

    [Fact]
    public async Task LogoutAsync_ShouldTriggerStateMachineLogoutSuccess()
    {
        // Arrange
        SetupSuccessfulLogout();

        // Act
        await _sut.LogoutAsync();

        // Assert
        _stateMachine.Received(1).Fire(AuthEvent.LogoutSuccess, Arg.Any<string?>());
    }

    [Fact]
    public async Task LogoutAsync_ShouldClearLocalAuthentication()
    {
        // Arrange
        SetupSuccessfulLogout();

        // Act
        await _sut.LogoutAsync();

        // Assert
        await _tokenStorage.Received(1).ClearAuthenticationAsync();
    }

    [Fact]
    public async Task LogoutAsync_WithNoUser_ShouldStillSucceed()
    {
        // Arrange
        _tokenStorage.GetLoginResponseAsync().Returns((LoginResponse?)null);

        // Act
        var result = await _sut.LogoutAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(result.LocalLogoutCompleted);
        Assert.True(result.ServerLogoutCompleted); // 无用户信息，跳过服务端登出，视为成功
    }

    [Fact]
    public async Task LogoutAsync_ServerFailure_ShouldQueueForRetry()
    {
        // Arrange
        var loginResponse = CreateLoginResponse();
        _tokenStorage.GetLoginResponseAsync().Returns(loginResponse);
        _authApi.LogoutAsync(Arg.Any<LogoutRequest>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _sut.LogoutAsync();

        // Assert
        Assert.True(result.Success); // 本地成功即视为成功
        Assert.True(result.LocalLogoutCompleted);
        Assert.False(result.ServerLogoutCompleted);
        Assert.True(result.ServerLogoutQueued);
        Assert.Equal(1, _sut.PendingServerLogoutCount);
    }

    [Fact]
    public async Task LogoutAsync_ServerFailure_ShouldPublishServerLogoutFailedEvent()
    {
        // Arrange
        var loginResponse = CreateLoginResponse();
        _tokenStorage.GetLoginResponseAsync().Returns(loginResponse);
        _authApi.LogoutAsync(Arg.Any<LogoutRequest>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        ServerLogoutFailedPayload? receivedPayload = null;
        _eventAggregator.GetEvent<AuthEvents.ServerLogoutFailedEvent>().Subscribe(payload =>
        {
            receivedPayload = payload;
        });

        // Act
        await _sut.LogoutAsync();

        // Assert
        receivedPayload.Should().NotBeNull();
        receivedPayload!.UserName.Should().Be(loginResponse.User.UserName);
        receivedPayload.Reason.Should().Be(ServerLogoutFailureReason.NetworkUnavailable);
        receivedPayload.QueuedForRetry.Should().BeTrue();
        receivedPayload.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task LogoutAsync_TokenInvalid_ShouldNotQueueForRetry()
    {
        // Arrange
        var loginResponse = CreateLoginResponse();
        _tokenStorage.GetLoginResponseAsync().Returns(loginResponse);
        _authApi.LogoutAsync(Arg.Any<LogoutRequest>())
            .Returns(new ApiResponse { Success = false, Message = "401 Unauthorized" });

        // Act
        var result = await _sut.LogoutAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(result.ServerLogoutCompleted); // Token无效视为成功
        Assert.False(result.ServerLogoutQueued);
        Assert.Equal(0, _sut.PendingServerLogoutCount);
    }

    #endregion

    #region ExecuteLocalLogoutAsync测试

    [Fact]
    public async Task ExecuteLocalLogoutAsync_ShouldClearAuthentication()
    {
        // Act
        await _sut.ExecuteLocalLogoutAsync();

        // Assert
        await _tokenStorage.Received(1).ClearAuthenticationAsync();
    }

    [Fact]
    public async Task ExecuteLocalLogoutAsync_WithException_ShouldNotThrow()
    {
        // Arrange
        _tokenStorage.ClearAuthenticationAsync()
            .ThrowsAsync(new Exception("Storage error"));

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => _sut.ExecuteLocalLogoutAsync());
        Assert.Null(exception);
    }

    #endregion

    #region ProcessPendingServerLogoutsAsync测试

    [Fact]
    public async Task ProcessPendingServerLogoutsAsync_WithEmptyQueue_ShouldReturnZero()
    {
        // Act
        var result = await _sut.ProcessPendingServerLogoutsAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ProcessPendingServerLogoutsAsync_WithPendingItem_ShouldProcess()
    {
        // Arrange - 首先添加一个待处理项
        var loginResponse = CreateLoginResponse();
        _tokenStorage.GetLoginResponseAsync().Returns(loginResponse);
        _authApi.LogoutAsync(Arg.Any<LogoutRequest>())
            .ThrowsAsync(new HttpRequestException("Network error"));
        await _sut.LogoutAsync();
        Assert.Equal(1, _sut.PendingServerLogoutCount);

        // 现在设置API成功
        _authApi.LogoutAsync(Arg.Any<LogoutRequest>())
            .Returns(new ApiResponse { Success = true });

        // Act
        var result = await _sut.ProcessPendingServerLogoutsAsync();

        // Assert
        Assert.Equal(1, result);
        Assert.Equal(0, _sut.PendingServerLogoutCount);
    }

    [Fact]
    public async Task ProcessPendingServerLogoutsAsync_AllCleared_ShouldPublishPendingLogoutsClearedEvent()
    {
        // Arrange
        var loginResponse = CreateLoginResponse();
        _tokenStorage.GetLoginResponseAsync().Returns(loginResponse);
        _authApi.LogoutAsync(Arg.Any<LogoutRequest>())
            .ThrowsAsync(new HttpRequestException("Network error"));
        await _sut.LogoutAsync();

        // 设置API成功
        _authApi.LogoutAsync(Arg.Any<LogoutRequest>())
            .Returns(new ApiResponse { Success = true });

        PendingLogoutsClearedPayload? receivedPayload = null;
        _eventAggregator.GetEvent<AuthEvents.PendingLogoutsClearedEvent>().Subscribe(payload =>
        {
            receivedPayload = payload;
        });

        // Act
        await _sut.ProcessPendingServerLogoutsAsync();

        // Assert
        receivedPayload.Should().NotBeNull();
        receivedPayload!.ProcessedCount.Should().Be(1);
        receivedPayload.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    #endregion

    #region PendingServerLogoutCount测试

    [Fact]
    public void PendingServerLogoutCount_Initial_ShouldBeZero()
    {
        // Assert
        Assert.Equal(0, _sut.PendingServerLogoutCount);
    }

    [Fact]
    public async Task PendingServerLogoutCount_AfterNetworkFailure_ShouldBeOne()
    {
        // Arrange
        var loginResponse = CreateLoginResponse();
        _tokenStorage.GetLoginResponseAsync().Returns(loginResponse);
        _authApi.LogoutAsync(Arg.Any<LogoutRequest>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        await _sut.LogoutAsync();

        // Assert
        Assert.Equal(1, _sut.PendingServerLogoutCount);
    }

    #endregion

    #region 超时测试

    [Fact]
    public async Task LogoutAsync_Timeout_ShouldQueueForRetry()
    {
        // Arrange
        var loginResponse = CreateLoginResponse();
        _tokenStorage.GetLoginResponseAsync().Returns(loginResponse);
        var timeoutException = new TaskCanceledException("Timeout",
            new TimeoutException("The operation has timed out"));
        _authApi.LogoutAsync(Arg.Any<LogoutRequest>())
            .ThrowsAsync(timeoutException);

        // Act
        var result = await _sut.LogoutAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(result.ServerLogoutQueued);
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulLogout()
    {
        var loginResponse = CreateLoginResponse();
        _tokenStorage.GetLoginResponseAsync().Returns(loginResponse);
        _authApi.LogoutAsync(Arg.Any<LogoutRequest>())
            .Returns(new ApiResponse { Success = true });
    }

    private static LoginResponse CreateLoginResponse()
    {
        return new LoginResponse
        {
            User = new UserDetailDto
            {
                Id = Guid.NewGuid(),
                UserName = "testuser",
                RealName = "Test User",
                Role = UserRole.Doctor
            },
            Token = "test-token",
            RefreshToken = "test-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
    }

    #endregion
}
