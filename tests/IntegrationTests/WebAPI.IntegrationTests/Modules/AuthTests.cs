using FluentAssertions;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.WebAPI.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using static LYBT.WebAPI.IntegrationTests.Infrastructure.TestHelpers;

namespace LYBT.WebAPI.IntegrationTests.Modules;

/// <summary>
/// Auth 模块集成测试
/// </summary>
/// <remarks>
/// 测试范围：
/// - 用户登录（正常/失败）
/// - 用户登出
/// - Token 验证（GET/POST）
/// - 密码修改（管理员权限）
/// </remarks>
public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    #region 登录测试

    /// <summary>
    /// 测试：正常登录成功
    /// </summary>
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange - 创建测试用户
        await _factory.Seeder.SeedDefaultUsersAsync();

        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "Admin123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeNullOrWhiteSpace();
        result.Data.User.Should().NotBeNull();
        result.Data.User.UserName.Should().Be("admin");
    }

    /// <summary>
    /// 测试：错误密码登录失败
    /// </summary>
    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsFail()
    {
        // Arrange
        await _factory.Seeder.SeedDefaultUsersAsync();

        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// 测试：不存在的用户登录失败
    /// </summary>
    [Fact]
    public async Task Login_WithNonexistentUser_ReturnsFail()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "nonexistent_user",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    /// <summary>
    /// 测试：空用户名登录失败
    /// </summary>
    [Fact]
    public async Task Login_WithEmptyUsername_ReturnsValidationError()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("用户名");
    }

    /// <summary>
    /// 测试：空密码登录失败
    /// </summary>
    [Fact]
    public async Task Login_WithEmptyPassword_ReturnsValidationError()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("密码");
    }

    #endregion

    #region 登出测试

    /// <summary>
    /// 测试：登录后登出成功
    /// </summary>
    [Fact]
    public async Task Logout_WithValidToken_ReturnsSuccess()
    {
        // Arrange - 创建用户并登录
        await _factory.Seeder.SeedDefaultUsersAsync();
        await _client.LoginAndSetAuthorizationAsync("admin", "Admin123!");

        var logoutRequest = new LogoutRequest
        {
            Username = "admin"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    /// <summary>
    /// 测试：未授权登出失败
    /// </summary>
    [Fact]
    public async Task Logout_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var logoutRequest = new LogoutRequest
        {
            Username = "admin"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Token 验证测试

    /// <summary>
    /// 测试：有效 Token 验证成功（GET 方式）
    /// </summary>
    [Fact]
    public async Task ValidateToken_Get_WithValidToken_ReturnsSuccess()
    {
        // Arrange - 登录获取 Token
        await _factory.Seeder.SeedDefaultUsersAsync();
        var token = await _client.LoginAndGetTokenAsync("admin", "Admin123!");
        _client.SetAuthorizationHeader(token);

        // Act
        var response = await _client.GetAsync("/api/v1/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    /// <summary>
    /// 测试：无效 Token 验证失败（GET 方式）
    /// </summary>
    [Fact]
    public async Task ValidateToken_Get_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.SetAuthorizationHeader("invalid_token_12345");

        // Act
        var response = await _client.GetAsync("/api/v1/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// 测试：缺少 Token 验证失败（GET 方式）
    /// </summary>
    [Fact]
    public async Task ValidateToken_Get_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// 测试：有效 Token 验证成功（POST 方式）
    /// </summary>
    [Fact]
    public async Task ValidateToken_Post_WithValidToken_ReturnsSuccess()
    {
        // Arrange - 登录获取 Token
        await _factory.Seeder.SeedDefaultUsersAsync();
        var token = await _client.LoginAndGetTokenAsync("admin", "Admin123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/validate", token);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    /// <summary>
    /// 测试：无效 Token 验证失败（POST 方式）
    /// </summary>
    [Fact]
    public async Task ValidateToken_Post_WithInvalidToken_ReturnsFail()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/validate", "invalid_token_12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    #endregion

    #region 基础端点测试

    /// <summary>
    /// 测试：GET /auth 返回 405 Method Not Allowed
    /// </summary>
    [Fact]
    public async Task Auth_GetBase_ReturnsMethodNotAllowed()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/auth");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    #endregion
}
