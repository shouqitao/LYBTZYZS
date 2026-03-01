using FluentAssertions;
using LYBT.Desktop.Contracts.Security;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Modules;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Shell.Services.Login;
using LYBT.Desktop.Shell.Services.Session;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Desktop.Shell.Tests.Services.Login;

/// <summary>
/// LoginCoordinator 单元测试
/// OpenSpec: refactor-login-authentication (Phase 2.2)
/// OpenSpec: refactor-auth-role-system (Phase 1.1) - 更新为使用IAuthenticationStateMachine
/// </summary>
public class LoginCoordinatorTests
{
    private readonly ILogger<LoginCoordinator> _logger;
    private readonly IAuthenticationService _authService;
    private readonly ITokenStorageService _tokenStorage;
    private readonly ISessionLifecycleManager _sessionManager;
    private readonly IModuleLoadingService _moduleLoading;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly IAuthenticationStateMachine _stateMachine;
    private readonly IConfiguration _configuration;
    private readonly LoginCoordinator _sut;
    private AuthState _currentMockState = AuthState.Idle;

    /// <summary>
    /// 状态转换表（模拟真实状态机）
    /// </summary>
    private static readonly Dictionary<(AuthState, AuthEvent), AuthState> StateTransitions = new()
    {
        { (AuthState.Idle, AuthEvent.StartLogin), AuthState.Authenticating },
        { (AuthState.Idle, AuthEvent.StartAutoLogin), AuthState.ValidatingToken },
        { (AuthState.Authenticating, AuthEvent.CredentialsValidated), AuthState.LoadingProfile },
        { (AuthState.Authenticating, AuthEvent.LoginFailure), AuthState.Failed },
        { (AuthState.ValidatingToken, AuthEvent.TokenValidated), AuthState.LoadingProfile },
        { (AuthState.ValidatingToken, AuthEvent.LoginFailure), AuthState.Idle },
        { (AuthState.LoadingProfile, AuthEvent.ProfileLoaded), AuthState.LoadingModules },
        { (AuthState.LoadingModules, AuthEvent.ModulesLoaded), AuthState.Navigating },
        { (AuthState.Navigating, AuthEvent.NavigationCompleted), AuthState.Authenticated },
        { (AuthState.Authenticated, AuthEvent.StartLogout), AuthState.LoggingOut },
        { (AuthState.LoggingOut, AuthEvent.LogoutSuccess), AuthState.Idle },
        { (AuthState.Failed, AuthEvent.Reset), AuthState.Idle },
    };

    public LoginCoordinatorTests()
    {
        _logger = Substitute.For<ILogger<LoginCoordinator>>();
        _authService = Substitute.For<IAuthenticationService>();
        _tokenStorage = Substitute.For<ITokenStorageService>();
        _sessionManager = Substitute.For<ISessionLifecycleManager>();
        _moduleLoading = Substitute.For<IModuleLoadingService>();
        _navigationCoordinator = Substitute.For<INavigationCoordinator>();
        _stateMachine = Substitute.For<IAuthenticationStateMachine>();
        _configuration = Substitute.For<IConfiguration>();

        // 配置状态机Substitute - 使用回调跟踪状态变化
        _stateMachine.Fire(Arg.Any<AuthEvent>(), Arg.Any<string?>())
            .Returns(callInfo =>
            {
                var evt = callInfo.Arg<AuthEvent>();
                var msg = callInfo.ArgAt<string?>(1);
                if (StateTransitions.TryGetValue((_currentMockState, evt), out var newState))
                {
                    var previousState = _currentMockState;
                    _currentMockState = newState;
                    // 触发状态变更事件
                    _stateMachine.StateChanged += Raise.EventWith(
                        new AuthStateChangedEventArgs(previousState, newState, evt, msg));
                    return true;
                }
                return false;
            });

        _stateMachine.CurrentState.Returns(_ => _currentMockState);
        _stateMachine.IsAuthenticated.Returns(_ => _currentMockState == AuthState.Authenticated);

        _sut = new LoginCoordinator(
            _logger,
            _authService,
            _tokenStorage,
            _sessionManager,
            _moduleLoading,
            _navigationCoordinator,
            _stateMachine,
            _configuration);
    }

    #region 初始状态测试

    [Fact]
    public void Constructor_ShouldInitialize_WithNotLoggedInState()
    {
        // Assert
        _sut.CurrentState.Should().Be(AuthState.Idle);
        _sut.IsLoggedIn.Should().BeFalse();
        _sut.CurrentUser.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        // Act
        var act = () => new LoginCoordinator(
            null!,
            _authService,
            _tokenStorage,
            _sessionManager,
            _moduleLoading,
            _navigationCoordinator,
            _stateMachine,
            _configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullAuthService_ShouldThrow()
    {
        // Act
        var act = () => new LoginCoordinator(
            _logger,
            null!,
            _tokenStorage,
            _sessionManager,
            _moduleLoading,
            _navigationCoordinator,
            _stateMachine,
            _configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("authenticationService");
    }

    #endregion

    #region LoginAsync测试

    [Fact]
    public async Task LoginAsync_Success_ShouldTransitionToLoggedIn()
    {
        // Arrange
        var user = CreateTestUser();
        var loginResponse = CreateLoginResponse(user);
        SetupSuccessfulLogin(loginResponse);

        // Act
        var result = await _sut.LoginAsync("testuser", "password");

        // Assert
        result.Success.Should().BeTrue();
        result.User.Should().Be(user);
        _sut.CurrentState.Should().Be(AuthState.Authenticated);
        _sut.IsLoggedIn.Should().BeTrue();
        _sut.CurrentUser.Should().Be(user);
    }

    [Fact]
    public async Task LoginAsync_Success_ShouldStartSession()
    {
        // Arrange
        var user = CreateTestUser();
        var loginResponse = CreateLoginResponse(user);
        SetupSuccessfulLogin(loginResponse);

        // Act
        await _sut.LoginAsync("testuser", "password");

        // Assert
        await _sessionManager.Received(1)
            .StartSessionAsync(user.UserName!, Arg.Any<string>(), loginResponse.ExpiresAt);
    }

    [Fact]
    public async Task LoginAsync_Success_ShouldLoadModules()
    {
        // Arrange
        var user = CreateTestUser();
        var loginResponse = CreateLoginResponse(user);
        SetupSuccessfulLogin(loginResponse);

        // Act
        await _sut.LoginAsync("testuser", "password");

        // Assert
        await _moduleLoading.Received(1)
            .LoadModulesAsync(Arg.Is<string[]>(arr => arr.Contains("PatientsModule")));
    }

    [Fact]
    public async Task LoginAsync_AdminUser_ShouldLoadAdminModules()
    {
        // Arrange
        var user = CreateTestUser(UserRole.Admin);
        var loginResponse = CreateLoginResponse(user);
        SetupSuccessfulLogin(loginResponse);

        // Act
        await _sut.LoginAsync("adminuser", "password");

        // Assert
        await _moduleLoading.Received(1)
            .LoadModulesAsync(Arg.Is<string[]>(arr =>
                arr.Contains("UsersModule") &&
                arr.Contains("HerbsModule") &&
                arr.Contains("FormulaModule")));
    }

    [Fact]
    public async Task LoginAsync_Failure_ShouldReturnFailedResult()
    {
        // Arrange
        _authService.LoginAsync(Arg.Any<LoginRequest>())
            .Returns(ServiceResult<LoginResponse>.Failure("认证失败"));

        // Act
        var result = await _sut.LoginAsync("testuser", "wrongpassword");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("认证失败");
        // 登录失败后状态机进入Failed状态，而非直接回到Idle
        _sut.CurrentState.Should().Be(AuthState.Failed);
        _sut.IsLoggedIn.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_ShouldRaiseStateChangedEvents()
    {
        // Arrange
        var user = CreateTestUser();
        var loginResponse = CreateLoginResponse(user);
        SetupSuccessfulLogin(loginResponse);
        var stateChanges = new List<AuthState>();
        _sut.StateChanged += (_, args) => stateChanges.Add(args.CurrentState);

        // Act
        await _sut.LoginAsync("testuser", "password");

        // Assert
        stateChanges.Should().Contain(AuthState.Authenticating);
        stateChanges.Should().Contain(AuthState.LoadingProfile);
        stateChanges.Should().Contain(AuthState.LoadingModules);
        stateChanges.Should().Contain(AuthState.Navigating);
        stateChanges.Should().Contain(AuthState.Authenticated);
    }

    [Fact]
    public async Task LoginAsync_Success_ShouldRaiseLoginSucceededEvent()
    {
        // Arrange
        var user = CreateTestUser();
        var loginResponse = CreateLoginResponse(user);
        SetupSuccessfulLogin(loginResponse);
        LoginSuccessEventArgs? eventArgs = null;
        _sut.LoginSucceeded += (_, args) => eventArgs = args;

        // Act
        await _sut.LoginAsync("testuser", "password");

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.User.Should().Be(user);
        // IsAutoLogin 属性已在 simplify-login-options 重构中移除
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoginAsync_WithInvalidUsername_ShouldThrow(string? invalidUsername)
    {
        // Act
        var act = () => _sut.LoginAsync(invalidUsername!, "password");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrow(string? invalidPassword)
    {
        // Act
        var act = () => _sut.LoginAsync("testuser", invalidPassword!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    // TryAutoLoginAsync 测试已移除 - 该功能在 simplify-login-options 重构中被删除
    // 自动登录功能已重新设计为"记住密码"模式

    #region HandleLoginSuccessAsync测试

    [Fact]
    public async Task HandleLoginSuccessAsync_ShouldStartSession()
    {
        // Arrange
        var user = CreateTestUser();
        var expiresAt = DateTime.Now.AddHours(1);
        SetupNavigationService();

        // Act
        await _sut.HandleLoginSuccessAsync(user, expiresAt);

        // Assert
        await _sessionManager.Received(1)
            .StartSessionAsync(user.UserName!, Arg.Any<string>(), expiresAt);
    }

    [Fact]
    public async Task HandleLoginSuccessAsync_ShouldLoadModules()
    {
        // Arrange
        var user = CreateTestUser();
        SetupNavigationService();

        // Act
        await _sut.HandleLoginSuccessAsync(user, DateTime.Now.AddHours(1));

        // Assert
        await _moduleLoading.Received()
            .LoadModulesAsync(Arg.Any<string[]>());
    }

    [Fact]
    public async Task HandleLoginSuccessAsync_WithNullUser_ShouldThrow()
    {
        // Act
        var act = () => _sut.HandleLoginSuccessAsync(null!, DateTime.Now.AddHours(1));

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region LogoutAsync测试

    [Fact]
    public async Task LogoutAsync_ShouldTransitionToNotLoggedIn()
    {
        // Arrange
        var user = CreateTestUser();
        var loginResponse = CreateLoginResponse(user);
        SetupSuccessfulLogin(loginResponse);
        await _sut.LoginAsync("testuser", "password");

        // Act
        await _sut.LogoutAsync();

        // Assert
        _sut.CurrentState.Should().Be(AuthState.Idle);
        _sut.IsLoggedIn.Should().BeFalse();
        _sut.CurrentUser.Should().BeNull();
    }

    [Fact]
    public async Task LogoutAsync_ShouldEndSession()
    {
        // Arrange
        var user = CreateTestUser();
        var loginResponse = CreateLoginResponse(user);
        SetupSuccessfulLogin(loginResponse);
        await _sut.LoginAsync("testuser", "password");

        // Act
        await _sut.LogoutAsync();

        // Assert
        await _sessionManager.Received(1).EndSessionAsync();
    }

    [Fact]
    public async Task LogoutAsync_ShouldCallAuthServiceLogout()
    {
        // Arrange
        var user = CreateTestUser();
        var loginResponse = CreateLoginResponse(user);
        SetupSuccessfulLogin(loginResponse);
        await _sut.LoginAsync("testuser", "password");

        // Act
        await _sut.LogoutAsync();

        // Assert
        await _authService.Received(1).LogoutAsync();
    }

    [Fact]
    public async Task LogoutAsync_ShouldRaiseLogoutCompletedEvent()
    {
        // Arrange
        var user = CreateTestUser();
        var loginResponse = CreateLoginResponse(user);
        SetupSuccessfulLogin(loginResponse);
        await _sut.LoginAsync("testuser", "password");
        var logoutRaised = false;
        _sut.LogoutCompleted += (_, _) => logoutRaised = true;

        // Act
        await _sut.LogoutAsync();

        // Assert
        logoutRaised.Should().BeTrue();
    }

    #endregion

    #region GetDiagnostics测试

    [Fact]
    public void GetDiagnostics_Initial_ShouldReturnNotLoggedInState()
    {
        // Act
        var diagnostics = _sut.GetDiagnostics();

        // Assert
        diagnostics.CurrentState.Should().Be(AuthState.Idle);
        diagnostics.IsLoggedIn.Should().BeFalse();
        diagnostics.UserName.Should().BeNull();
        diagnostics.UserRole.Should().BeNull();
        diagnostics.LoginAttemptCount.Should().Be(0);
    }

    [Fact]
    public async Task GetDiagnostics_AfterLogin_ShouldReturnUserInfo()
    {
        // Arrange
        var user = CreateTestUser();
        var loginResponse = CreateLoginResponse(user);
        SetupSuccessfulLogin(loginResponse);
        await _sut.LoginAsync("testuser", "password");

        // Act
        var diagnostics = _sut.GetDiagnostics();

        // Assert
        diagnostics.CurrentState.Should().Be(AuthState.Authenticated);
        diagnostics.IsLoggedIn.Should().BeTrue();
        diagnostics.UserName.Should().Be(user.UserName);
        diagnostics.LoginAttemptCount.Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private static UserDetailDto CreateTestUser(UserRole role = UserRole.Doctor)
    {
        return new UserDetailDto
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            RealName = "Test User",
            Role = role
        };
    }

    private static LoginResponse CreateLoginResponse(UserDetailDto user)
    {
        return new LoginResponse
        {
            User = user,
            Token = "test-token",
            RefreshToken = "test-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
    }

    private void SetupSuccessfulLogin(LoginResponse response)
    {
        _authService.LoginAsync(Arg.Any<LoginRequest>())
            .Returns(ServiceResult<LoginResponse>.Success(response));
        SetupNavigationService();
    }

    private void SetupSuccessfulAutoLogin(LoginResponse response)
    {
        _tokenStorage.GetTokenAsync().Returns("valid-token");
        _authService.ValidateTokenAsync(Arg.Any<string>())
            .Returns(ServiceResult<ValidateTokenResponse>.Success(new ValidateTokenResponse
            {
                IsValid = true,
                Username = response.User.UserName,
                Role = response.User.Role.ToString(),
                ExpiresAt = response.ExpiresAt
            }));
        _tokenStorage.GetLoginResponseAsync().Returns(response);
        SetupNavigationService();
    }

    private void SetupNavigationService()
    {
        // INavigationCoordinator.NavigateToHome is synchronous, no setup needed for NSubstitute
    }

    #endregion
}
