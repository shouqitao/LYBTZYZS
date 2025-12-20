using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Modules;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Shell.Services.Login;
using LYBT.Desktop.Shell.Services.Session;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace LYBT.Desktop.Shell.Tests.Services.Login;

/// <summary>
/// LoginCoordinator 单元测试
/// </summary>
public class LoginCoordinatorTests
{
    private readonly Mock<ILogger<LoginCoordinator>> _loggerMock;
    private readonly Mock<IAuthenticationService> _authServiceMock;
    private readonly Mock<ITokenStorageService> _tokenStorageMock;
    private readonly Mock<ISessionLifecycleManager> _sessionManagerMock;
    private readonly Mock<IModuleLoadingService> _moduleLoadingMock;
    private readonly Mock<IRoleNavigationService> _roleNavigationMock;
    private readonly LoginCoordinator _sut;

    public LoginCoordinatorTests()
    {
        _loggerMock = new Mock<ILogger<LoginCoordinator>>();
        _authServiceMock = new Mock<IAuthenticationService>();
        _tokenStorageMock = new Mock<ITokenStorageService>();
        _sessionManagerMock = new Mock<ISessionLifecycleManager>();
        _moduleLoadingMock = new Mock<IModuleLoadingService>();
        _roleNavigationMock = new Mock<IRoleNavigationService>();

        _sut = new LoginCoordinator(
            _loggerMock.Object,
            _authServiceMock.Object,
            _tokenStorageMock.Object,
            _sessionManagerMock.Object,
            _moduleLoadingMock.Object,
            _roleNavigationMock.Object);
    }

    #region 初始状态测试

    [Fact]
    public void Constructor_ShouldInitialize_WithNotLoggedInState()
    {
        // Assert
        _sut.CurrentState.Should().Be(LoginFlowState.NotLoggedIn);
        _sut.IsLoggedIn.Should().BeFalse();
        _sut.CurrentUser.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        // Act
        var act = () => new LoginCoordinator(
            null!,
            _authServiceMock.Object,
            _tokenStorageMock.Object,
            _sessionManagerMock.Object,
            _moduleLoadingMock.Object,
            _roleNavigationMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullAuthService_ShouldThrow()
    {
        // Act
        var act = () => new LoginCoordinator(
            _loggerMock.Object,
            null!,
            _tokenStorageMock.Object,
            _sessionManagerMock.Object,
            _moduleLoadingMock.Object,
            _roleNavigationMock.Object);

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
        _sut.CurrentState.Should().Be(LoginFlowState.LoggedIn);
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
        _sessionManagerMock.Verify(
            s => s.StartSessionAsync(user.UserName!, It.IsAny<string>(), loginResponse.ExpiresAt),
            Times.Once);
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
        _moduleLoadingMock.Verify(
            m => m.LoadModulesAsync(It.Is<string[]>(arr => arr.Contains("PatientsModule"))),
            Times.Once);
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
        _moduleLoadingMock.Verify(
            m => m.LoadModulesAsync(It.Is<string[]>(arr =>
                arr.Contains("UsersModule") &&
                arr.Contains("HerbsModule") &&
                arr.Contains("FormulaModule"))),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_Failure_ShouldReturnFailedResult()
    {
        // Arrange
        _authServiceMock.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(ServiceResult<LoginResponse>.Failure("认证失败"));

        // Act
        var result = await _sut.LoginAsync("testuser", "wrongpassword");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("认证失败");
        _sut.CurrentState.Should().Be(LoginFlowState.NotLoggedIn);
        _sut.IsLoggedIn.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_ShouldRaiseStateChangedEvents()
    {
        // Arrange
        var user = CreateTestUser();
        var loginResponse = CreateLoginResponse(user);
        SetupSuccessfulLogin(loginResponse);
        var stateChanges = new List<LoginFlowState>();
        _sut.StateChanged += (_, args) => stateChanges.Add(args.CurrentState);

        // Act
        await _sut.LoginAsync("testuser", "password");

        // Assert
        stateChanges.Should().Contain(LoginFlowState.Authenticating);
        stateChanges.Should().Contain(LoginFlowState.StartingSession);
        stateChanges.Should().Contain(LoginFlowState.LoadingModules);
        stateChanges.Should().Contain(LoginFlowState.Navigating);
        stateChanges.Should().Contain(LoginFlowState.LoggedIn);
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
        eventArgs.IsAutoLogin.Should().BeFalse();
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

    #region TryAutoLoginAsync测试

    [Fact]
    public async Task TryAutoLoginAsync_WithNoToken_ShouldReturnFalse()
    {
        // Arrange
        _tokenStorageMock.Setup(t => t.GetTokenAsync()).ReturnsAsync((string?)null);

        // Act
        var result = await _sut.TryAutoLoginAsync();

        // Assert
        result.Should().BeFalse();
        _sut.CurrentState.Should().Be(LoginFlowState.NotLoggedIn);
    }

    [Fact]
    public async Task TryAutoLoginAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        _tokenStorageMock.Setup(t => t.GetTokenAsync()).ReturnsAsync("invalid-token");
        _authServiceMock.Setup(a => a.ValidateTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(ServiceResult<ValidateTokenResponse>.Failure("Token无效"));

        // Act
        var result = await _sut.TryAutoLoginAsync();

        // Assert
        result.Should().BeFalse();
        _sut.CurrentState.Should().Be(LoginFlowState.NotLoggedIn);
        _authServiceMock.Verify(a => a.ClearAuthInfo(), Times.Once);
    }

    [Fact]
    public async Task TryAutoLoginAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateTestUser();
        var loginResponse = CreateLoginResponse(user);
        SetupSuccessfulAutoLogin(loginResponse);

        // Act
        var result = await _sut.TryAutoLoginAsync();

        // Assert
        result.Should().BeTrue();
        _sut.CurrentState.Should().Be(LoginFlowState.LoggedIn);
        _sut.CurrentUser.Should().Be(user);
    }

    [Fact]
    public async Task TryAutoLoginAsync_Success_ShouldRaiseLoginSucceededWithAutoLoginFlag()
    {
        // Arrange
        var user = CreateTestUser();
        var loginResponse = CreateLoginResponse(user);
        SetupSuccessfulAutoLogin(loginResponse);
        LoginSuccessEventArgs? eventArgs = null;
        _sut.LoginSucceeded += (_, args) => eventArgs = args;

        // Act
        await _sut.TryAutoLoginAsync();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.IsAutoLogin.Should().BeTrue();
    }

    #endregion

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
        _sessionManagerMock.Verify(
            s => s.StartSessionAsync(user.UserName!, It.IsAny<string>(), expiresAt),
            Times.Once);
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
        _moduleLoadingMock.Verify(
            m => m.LoadModulesAsync(It.IsAny<string[]>()),
            Times.AtLeastOnce);
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
        _sut.CurrentState.Should().Be(LoginFlowState.NotLoggedIn);
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
        _sessionManagerMock.Verify(s => s.EndSessionAsync(), Times.Once);
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
        _authServiceMock.Verify(a => a.LogoutAsync(), Times.Once);
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
        diagnostics.CurrentState.Should().Be(LoginFlowState.NotLoggedIn);
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
        diagnostics.CurrentState.Should().Be(LoginFlowState.LoggedIn);
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
        _authServiceMock.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(ServiceResult<LoginResponse>.Success(response));
        SetupNavigationService();
    }

    private void SetupSuccessfulAutoLogin(LoginResponse response)
    {
        _tokenStorageMock.Setup(t => t.GetTokenAsync()).ReturnsAsync("valid-token");
        _authServiceMock.Setup(a => a.ValidateTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(ServiceResult<ValidateTokenResponse>.Success(new ValidateTokenResponse
            {
                IsValid = true,
                Username = response.User.UserName,
                Role = response.User.Role.ToString(),
                ExpiresAt = response.ExpiresAt
            }));
        _tokenStorageMock.Setup(t => t.GetLoginResponseAsync()).ReturnsAsync(response);
        SetupNavigationService();
    }

    private void SetupNavigationService()
    {
        // IRoleNavigationService.NavigateToRoleHome is synchronous, no setup needed for Moq
    }

    #endregion
}
