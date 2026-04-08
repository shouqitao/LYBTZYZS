using FluentAssertions;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Microsoft.Extensions.Logging;
using Refit;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Foundation;

/// <summary>
/// Authentication E2E Tests - 验证登录和 Token 管理
/// 
/// 测试顺序:
/// 1. Login - 获取 JWT Token
/// 2. Token Validation - 验证 Token 可用
/// 3. Refresh Token - 刷新 Token
/// </summary>
[Collection("AuthTests")]
public class AuthTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public AuthTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Test: POST /api/v1/auth/login
    /// 预期: 200 OK, 返回有效 JWT Token
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Auth")]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var username = Configuration["TestCredentials:Username"]!;
        var password = Configuration["TestCredentials:Password"]!;
        
        Logger.LogInformation("Testing login with user: {Username}", username);

        // Act
        var response = await AuthApi.LoginAsync(new LoginRequest
        {
            UserName = username,
            Password = password
        });

        // Log response
        _output.WriteLine("Login Response: Success={0}, Message={1}", response.Success, response.Message);
        if (response.Data != null)
        {
            _output.WriteLine("Token expires at: {0}", response.Data.ExpiresAt);
        }

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue("登录应该成功");
        response.Data.Should().NotBeNull("应该返回 Token 数据");
        response.Data!.Token.Should().NotBeNullOrEmpty("应该返回 Access Token");
        response.Data.RefreshToken.Should().NotBeNullOrEmpty("应该返回 Refresh Token");
        response.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow, "Token 应该在将来过期");
        
        Logger.LogInformation("Login test passed for user: {Username}", username);
    }

    /// <summary>
    /// Test: POST /api/v1/auth/login (invalid credentials)
    /// 预期: 401 Unauthorized
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Auth")]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "invalid_user",
            Password = "wrong_password"
        };

        try
        {
            var response = await AuthApi.LoginAsync(request);
            response.Success.Should().BeFalse("无效凭证应该登录失败");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        Logger.LogInformation("Invalid login test passed - correctly rejected");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Auth")]
    public async Task ValidateToken_AfterLogin_ShouldReturnUserInfo()
    {
        await LoginAsSysadminAsync();
        var authenticatedAuthApi = CreateAuthenticatedAuthApi();
        
        Logger.LogInformation("Testing token validation after login...");

        var response = await authenticatedAuthApi.ValidateTokenFromHeaderAsync();

        // Log response
        _output.WriteLine("Current User Response: {0}", System.Text.Json.JsonSerializer.Serialize(response));

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();

        Logger.LogInformation("Token validation test passed");
    }

    #region Token Management

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Authentication")]
    public async Task ValidateToken_FromHeader_ShouldReturnUserInfo()
    {
        await LoginAsSysadminAsync();
        var authenticatedAuthApi = CreateAuthenticatedAuthApi();

        var response = await authenticatedAuthApi.ValidateTokenFromHeaderAsync();

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Authentication")]
    public async Task Login_WithAutoToken_ShouldReturnAutoLoginToken()
    {
        var password = Configuration["TestCredentials:Password"]!;

        var response = await AuthApi.LoginAsync(new LoginRequest
        {
            UserName = "sysadmin",
            Password = password,
            RememberMe = true
        });

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.AutoLoginToken.Should().NotBeNullOrEmpty();
        _output.WriteLine($"AutoLogin token: {response.Data.AutoLoginToken}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Authentication")]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewToken()
    {
        var password = Configuration["TestCredentials:Password"]!;

        var loginResponse = await AuthApi.LoginAsync(new LoginRequest
        {
            UserName = "sysadmin",
            Password = password
        });
        loginResponse.Success.Should().BeTrue();
        var refreshToken = loginResponse.Data!.RefreshToken;

        var response = await AuthApi.RefreshTokenAsync(new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        });
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Token.Should().NotBeNullOrEmpty();
        _output.WriteLine($"Refreshed token: {response.Data.Token[..20]}...");
    }

    #endregion

    /// <summary>
    /// Test: POST /api/v1/auth/logout
    /// 预期: 200 OK
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Auth")]
    public async Task Logout_AfterLogin_ShouldSucceed()
    {
        // Arrange - Login first
        var loginResponse = await LoginAsSysadminAsync();
        
        Logger.LogInformation("Testing logout...");

        // Act
        var response = await AuthApi.LogoutAsync(new LogoutRequest
        {
            RefreshToken = loginResponse.RefreshToken
        });

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue("登出应该成功");
        
        Logger.LogInformation("Logout test passed");
    }
}
