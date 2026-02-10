using System.Net.Http;
using FluentAssertions;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Desktop.Foundation.Tests.Security;

/// <summary>
/// AuthenticationService 单元测试
/// Issue #1866: 测试本地Token验证、登录登出、用户信息获取
/// </summary>
public class AuthenticationServiceTests
{
    private readonly IAuthApi _authApi;
    private readonly ITokenStorageService _tokenStorage;
    private readonly ITokenValidator _tokenValidator;
    private readonly ICredentialVault _credentialVault;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly AuthenticationService _authService;

    public AuthenticationServiceTests()
    {
        _authApi = Substitute.For<IAuthApi>();
        _tokenStorage = Substitute.For<ITokenStorageService>();
        _tokenValidator = Substitute.For<ITokenValidator>();
        _credentialVault = Substitute.For<ICredentialVault>();
        _logger = Substitute.For<ILogger<AuthenticationService>>();

        _authService = new AuthenticationService(
            _authApi,
            _tokenStorage,
            _tokenValidator,
            _credentialVault,
            _logger
        );
    }

    /// <summary>
    /// 测试：有效Token验证成功
    /// </summary>
    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ReturnsSuccess()
    {
        // Arrange
        var token = "valid_test_token";
        var userInfo = new TokenUserInfo
        {
            UserId = Guid.NewGuid(),
            UserName = "test_user",
            Role = "Doctor",
            UserType = "user"
        };

        _tokenValidator.ValidateTokenAsync(token)
            .Returns(Task.FromResult(new TokenValidationResult
            {
                IsValid = true,
                UserInfo = userInfo
            }));

        // Act
        var result = await _authService.ValidateTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue("有效Token应验证成功");
        result.Data.Should().NotBeNull();
        result.Data!.IsValid.Should().BeTrue();
        result.Data.Username.Should().Be("test_user");
        result.Data.Role.Should().Be("Doctor");
    }

    /// <summary>
    /// 测试：无效Token验证失败
    /// </summary>
    [Fact]
    public async Task ValidateTokenAsync_InvalidToken_ReturnsFailure()
    {
        // Arrange
        var token = "invalid_test_token";

        _tokenValidator.ValidateTokenAsync(token)
            .Returns(Task.FromResult(new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token已过期"
            }));

        // Act
        var result = await _authService.ValidateTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse("无效Token应验证失败");
        result.Message.Should().Contain("Token已过期");
    }

    /// <summary>
    /// 测试：登录成功
    /// </summary>
    [Fact]
    public async Task LoginAsync_Success_ReturnsLoginResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "test_user",
            Password = "test_password"
        };

        var loginResponse = new LoginResponse
        {
            Token = "access_token",
            RefreshToken = "refresh_token",
            User = new UserDetailDto
            {
                Id = Guid.NewGuid(),
                UserName = "test_user",
                Role = UserRole.Doctor
            }
        };

        _authApi.LoginAsync(request)
            .Returns(Task.FromResult(new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = loginResponse,
                Message = "登录成功"
            }));

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue("登录应该成功");
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be("access_token");
        result.Data.User.UserName.Should().Be("test_user");
    }

    /// <summary>
    /// 测试：登录失败
    /// </summary>
    [Fact]
    public async Task LoginAsync_Failure_ReturnsError()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "test_user",
            Password = "wrong_password"
        };

        _authApi.LoginAsync(request)
            .Returns(Task.FromResult(new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = "用户名或密码错误"
            }));

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse("登录应该失败");
        result.Message.Should().Contain("用户名或密码错误");
    }

    /// <summary>
    /// 测试：登出成功并清除本地Token
    /// </summary>
    [Fact]
    public async Task LogoutAsync_Success_ClearsAuthentication()
    {
        // Arrange
        var loginResponse = new LoginResponse
        {
            Token = "access_token",
            RefreshToken = "refresh_token",
            User = new UserDetailDto
            {
                Id = Guid.NewGuid(),
                UserName = "test_user",
                Role = UserRole.Doctor
            }
        };

        _tokenStorage.GetLoginResponseAsync()
            .Returns(Task.FromResult<LoginResponse?>(loginResponse));

        _authApi.LogoutAsync(Arg.Any<LogoutRequest>())
            .Returns(Task.FromResult(new ApiResponse
            {
                Success = true,
                Message = "登出成功"
            }));

        // Act
        var result = await _authService.LogoutAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue("登出应该成功");

        // 验证调用了清除方法
        await _tokenStorage.Received(1).ClearAuthenticationAsync();
    }

    /// <summary>
    /// 测试：登出时服务器失败仍清除本地Token
    /// </summary>
    [Fact]
    public async Task LogoutAsync_ServerFails_StillClearsLocalToken()
    {
        // Arrange
        var loginResponse = new LoginResponse
        {
            Token = "access_token",
            RefreshToken = "refresh_token",
            User = new UserDetailDto
            {
                Id = Guid.NewGuid(),
                UserName = "test_user",
                Role = UserRole.Doctor
            }
        };

        _tokenStorage.GetLoginResponseAsync()
            .Returns(Task.FromResult<LoginResponse?>(loginResponse));

        _authApi.LogoutAsync(Arg.Any<LogoutRequest>())
            .Returns(Task.FromResult(new ApiResponse
            {
                Success = false,
                Message = "服务器错误"
            }));

        // Act
        var result = await _authService.LogoutAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue("即使服务器失败，本地登出也应成功");

        // 验证调用了清除方法
        await _tokenStorage.Received(1).ClearAuthenticationAsync();
    }

    /// <summary>
    /// 测试：获取当前用户信息
    /// </summary>
    [Fact]
    public async Task GetCurrentUserAsync_ReturnsUser()
    {
        // Arrange
        var loginResponse = new LoginResponse
        {
            Token = "access_token",
            RefreshToken = "refresh_token",
            User = new UserDetailDto
            {
                Id = Guid.NewGuid(),
                UserName = "test_user",
                Role = UserRole.Doctor
            }
        };

        _tokenStorage.GetLoginResponseAsync()
            .Returns(Task.FromResult<LoginResponse?>(loginResponse));

        // Act
        var user = await _authService.GetCurrentUserAsync();

        // Assert
        user.Should().NotBeNull();
        user!.UserName.Should().Be("test_user");
        user.Role.Should().Be(UserRole.Doctor);
    }

    /// <summary>
    /// 测试：没有登录时获取当前用户返回null
    /// </summary>
    [Fact]
    public async Task GetCurrentUserAsync_NotLoggedIn_ReturnsNull()
    {
        // Arrange
        _tokenStorage.GetLoginResponseAsync()
            .Returns(Task.FromResult<LoginResponse?>(null));

        // Act
        var user = await _authService.GetCurrentUserAsync();

        // Assert
        user.Should().BeNull("未登录时应返回null");
    }

    /// <summary>
    /// 测试：检查连接成功
    /// </summary>
    [Fact]
    public async Task CheckConnectionAsync_Success_ReturnsTrue()
    {
        // Arrange
        _tokenStorage.IsTokenExpiredAsync()
            .Returns(Task.FromResult(false));

        _authApi.HealthCheckAsync()
            .Returns(Task.FromResult(new HealthCheckResponse
            {
                Status = "Healthy"
            }));

        // Act
        var result = await _authService.CheckConnectionAsync();

        // Assert
        result.Should().BeTrue("健康检查成功应返回true");
    }

    /// <summary>
    /// 测试：Token过期时检查连接返回false
    /// </summary>
    [Fact]
    public async Task CheckConnectionAsync_TokenExpired_ReturnsFalse()
    {
        // Arrange
        _tokenStorage.IsTokenExpiredAsync()
            .Returns(Task.FromResult(true));

        // Act
        var result = await _authService.CheckConnectionAsync();

        // Assert
        result.Should().BeFalse("Token过期时应返回false");
    }

    /// <summary>
    /// 测试：服务器不可用时检查连接返回false
    /// </summary>
    [Fact]
    public async Task CheckConnectionAsync_ServerUnavailable_ReturnsFalse()
    {
        // Arrange
        _tokenStorage.IsTokenExpiredAsync()
            .Returns(Task.FromResult(false));

        _authApi.HealthCheckAsync()
            .Returns(Task.FromException<HealthCheckResponse>(new HttpRequestException("Connection refused")));

        // Act
        var result = await _authService.CheckConnectionAsync();

        // Assert
        result.Should().BeFalse("服务器不可用时应返回false");
    }

    /// <summary>
    /// 测试：IsLoggedInAsync - 有Token返回true
    /// </summary>
    [Fact]
    public async Task IsLoggedInAsync_HasToken_ReturnsTrue()
    {
        // Arrange
        _tokenStorage.GetTokenAsync()
            .Returns(Task.FromResult<string?>("valid_token"));

        // Act
        var result = await _authService.IsLoggedInAsync();

        // Assert
        result.Should().BeTrue("有Token时应返回true");
    }

    /// <summary>
    /// 测试：IsLoggedInAsync - 无Token返回false
    /// </summary>
    [Fact]
    public async Task IsLoggedInAsync_NoToken_ReturnsFalse()
    {
        // Arrange
        _tokenStorage.GetTokenAsync()
            .Returns(Task.FromResult(null as string));

        // Act
        var result = await _authService.IsLoggedInAsync();

        // Assert
        result.Should().BeFalse("无Token时应返回false");
    }

    /// <summary>
    /// 测试：ClearAuthInfo清除认证信息
    /// </summary>
    [Fact]
    public void ClearAuthInfo_Success()
    {
        // Arrange
        _tokenStorage.ClearAuthenticationAsync()
            .Returns(Task.CompletedTask);

        // Act
        var act = () => _authService.ClearAuthInfo();

        // Assert
        act.Should().NotThrow("清除认证信息不应抛异常");
    }
}
