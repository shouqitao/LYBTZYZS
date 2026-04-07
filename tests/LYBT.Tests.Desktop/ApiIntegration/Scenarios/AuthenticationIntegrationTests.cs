using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.ApiIntegration.Infrastructure;
using LYBT.Tests.Desktop.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace LYBT.Tests.Desktop.ApiIntegration.Scenarios;

/// <summary>
/// Authentication API Integration Tests
/// 
/// Tests the complete authentication flow using mocked API clients:
/// - Login success/failure scenarios
/// - Token refresh and expiration handling
/// - Authentication state management
/// - Error handling for auth operations
/// </summary>
public class AuthenticationIntegrationTests : ApiIntegrationTestBase
{
    public AuthenticationIntegrationTests() { }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldSucceedAndStoreToken()
    {
        // Arrange
        var testUser = CreateTestUser("doctor1", UserRole.Doctor);
        SetupSuccessfulLogin(testUser);

        // Act
        var loginRequest = new LoginRequest
        {
            UserName = "doctor1",
            Password = "Password123!"
        };
        var result = await AuthenticationService.LoginAsync(loginRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeNullOrEmpty();
        result.Data.User.UserName.Should().Be("doctor1");
        result.Data.User.Role.Should().Be(UserRole.Doctor);

        // Verify token was stored
        var storedToken = await TokenStorage.GetLoginResponseAsync();
        storedToken.Should().NotBeNull();
        storedToken!.Token.Should().Be(result.Data.Token);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldFail()
    {
        // Arrange
        SetupFailedLogin("用户名或密码错误");

        // Act
        var loginRequest = new LoginRequest
        {
            UserName = "invaliduser",
            Password = "wrongpassword"
        };
        var result = await AuthenticationService.LoginAsync(loginRequest);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("用户名或密码错误");
        result.Data.Should().BeNull();

        // Verify no token was stored
        var storedToken = await TokenStorage.GetLoginResponseAsync();
        storedToken.Should().BeNull();
    }

    [Fact]
    public async Task Login_WithNetworkError_ShouldHandleGracefully()
    {
        // Arrange - Setup network error
        AuthApi.LoginAsync(Arg.Any<LoginRequest>())
            .Returns(Task.FromException<LoginResponse>(
                new HttpRequestException("网络连接失败")));

        // Act
        var loginRequest = new LoginRequest
        {
            UserName = "testuser",
            Password = "Password123!"
        };
        var result = await AuthenticationService.LoginAsync(loginRequest);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("网络连接失败");
    }

    [Fact]
    public async Task TokenValidation_WithValidToken_ShouldSucceed()
    {
        // Arrange
        var testUser = CreateTestUser("doctor1", UserRole.Doctor);
        var validToken = TestData.GenerateJwtToken(testUser.Id, testUser.UserName, testUser.Role.ToString());
        
        var loginResponse = new LoginResponse
        {
            Token = validToken,
            RefreshToken = TestData.GenerateRefreshToken(),
            User = testUser,
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };
        
        await TokenStorage.SaveAuthenticationAsync(loginResponse, rememberMe: false);

        // Act
        var validationResult = await TokenValidator.ValidateTokenAsync(validToken);

        // Assert
        validationResult.IsValid.Should().BeTrue();
        validationResult.UserInfo.Should().NotBeNull();
        validationResult.UserInfo!.UserName.Should().Be("doctor1");
        validationResult.UserInfo.Role.Should().Be("Doctor");
    }

    [Fact]
    public async Task TokenValidation_WithExpiredToken_ShouldFail()
    {
        // Arrange
        var testUser = CreateTestUser("doctor1", UserRole.Doctor);
        var expiredToken = TestData.GenerateExpiredJwtToken(testUser.Id, testUser.UserName, testUser.Role.ToString());
        
        var loginResponse = new LoginResponse
        {
            Token = expiredToken,
            RefreshToken = TestData.GenerateRefreshToken(),
            User = testUser,
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };
        
        await TokenStorage.SaveAuthenticationAsync(loginResponse, rememberMe: false);

        // Act
        var validationResult = await TokenValidator.ValidateTokenAsync(expiredToken);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.UserInfo.Should().BeNull();
    }

    [Fact]
    public async Task Logout_ShouldClearStoredToken()
    {
        // Arrange
        var testUser = CreateTestUser("doctor1", UserRole.Doctor);
        SetupSuccessfulLogin(testUser);
        
        var loginRequest = new LoginRequest
        {
            UserName = "doctor1",
            Password = "Password123!"
        };
        await AuthenticationService.LoginAsync(loginRequest);

        // Verify token is stored
        var storedToken = await TokenStorage.GetLoginResponseAsync();
        storedToken.Should().NotBeNull();

        // Act
        await AuthenticationService.LogoutAsync();

        // Assert
        var clearedToken = await TokenStorage.GetLoginResponseAsync();
        clearedToken.Should().BeNull();
    }

    [Fact]
    public async Task IsLoggedIn_WhenTokenValid_ShouldReturnTrue()
    {
        // Arrange
        var testUser = CreateTestUser("doctor1", UserRole.Doctor);
        var validToken = TestData.GenerateJwtToken(testUser.Id, testUser.UserName, testUser.Role.ToString());
        
        var loginResponse = new LoginResponse
        {
            Token = validToken,
            RefreshToken = TestData.GenerateRefreshToken(),
            User = testUser,
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };
        
        await TokenStorage.SaveAuthenticationAsync(loginResponse, rememberMe: false);

        // Act
        var isLoggedIn = await AuthenticationService.IsLoggedInAsync();

        // Assert
        isLoggedIn.Should().BeTrue();
    }

    [Fact]
    public async Task IsLoggedIn_WhenTokenExpired_ShouldReturnFalse()
    {
        // Arrange
        var testUser = CreateTestUser("doctor1", UserRole.Doctor);
        var expiredToken = TestData.GenerateExpiredJwtToken(testUser.Id, testUser.UserName, testUser.Role.ToString());
        
        var loginResponse = new LoginResponse
        {
            Token = expiredToken,
            RefreshToken = TestData.GenerateRefreshToken(),
            User = testUser,
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };
        
        await TokenStorage.SaveAuthenticationAsync(loginResponse, rememberMe: false);

        // Act
        var isLoggedIn = await AuthenticationService.IsLoggedInAsync();

        // Assert
        isLoggedIn.Should().BeFalse();
    }

    [Fact]
    public async Task IsLoggedIn_WhenNoToken_ShouldReturnFalse()
    {
        // Arrange - No token stored

        // Act
        var isLoggedIn = await AuthenticationService.IsLoggedInAsync();

        // Assert
        isLoggedIn.Should().BeFalse();
    }
}

