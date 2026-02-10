using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Integration.Fixtures;

namespace LYBT.Tests.Server.Integration.Auth;

/// <summary>
/// 认证模块集成测试。
/// 验证完整HTTP管线: Controller -> AuthService -> JwtService -> DB。
/// 不Mock任何组件，测试真实认证流程。
/// </summary>
[Collection("ServerIntegration")]
public class AuthIntegrationTests
{
    private readonly WebApiFixture _fixture;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AuthIntegrationTests(WebApiFixture fixture)
    {
        _fixture = fixture;
    }

    #region Login - 成功场景

    [Fact]
    public async Task Login_ValidAdminCredentials_ReturnsTokenAndUserInfo()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "admin",
            Password = WebApiFixture.AdminPassword
        };

        // Act
        var response = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Token.Should().NotBeNullOrWhiteSpace("登录成功应返回JWT Token");
        body.Data.RefreshToken.Should().NotBeNullOrWhiteSpace("登录成功应返回RefreshToken");
        body.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow, "Token过期时间应在当前时间之后");
        body.Data.User.Should().NotBeNull("登录成功应返回用户信息");
    }

    [Fact]
    public async Task Login_ValidDoctorCredentials_ReturnsToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "doctor",
            Password = WebApiFixture.DoctorPassword
        };

        // Act
        var response = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Token.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Login - 失败场景

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "admin",
            Password = "wrong_password_123"
        };

        // Act
        var response = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "nonexistent_user_xyz",
            Password = "any_password"
        };

        // Act
        var response = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_EmptyUsername_Returns400()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "",
            Password = "some_password"
        };

        // Act
        var response = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert - Controller直接返回ValidationFail(400)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_EmptyPassword_Returns400()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "admin",
            Password = ""
        };

        // Act
        var response = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Authorization - 权限控制

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        // Arrange & Act - GET /api/v1/users 需要AdminOnly策略
        var response = await _fixture.AnonymousClient
            .GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithAdminToken_Returns200()
    {
        // Arrange & Act
        var response = await _fixture.AdminClient
            .GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminOnlyEndpoint_WithDoctorToken_Returns403()
    {
        // Arrange & Act
        // UsersController标记[Authorize(Policy="AdminOnly")]
        // Doctor角色不在AdminOnly策略("SuperAdmin","Admin")中
        var response = await _fixture.DoctorClient
            .GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Token验证

    [Fact]
    public async Task ValidateToken_WithLoginToken_ReturnsSuccess()
    {
        // Arrange: 通过真实登录获取Token (确保DB中有会话记录)
        var loginRequest = new LoginRequest
        {
            UserName = "admin",
            Password = WebApiFixture.AdminPassword
        };
        var loginResponse = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content
            .ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        var token = loginBody!.Data!.Token;

        // Act: 使用登录返回的Token调用validate端点
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValidateToken_WithoutToken_Returns401()
    {
        // Arrange & Act
        var response = await _fixture.AnonymousClient
            .GetAsync("/api/v1/auth/validate");

        // Assert - [Authorize]中间件拦截
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Token刷新

    [Fact]
    public async Task RefreshToken_AfterLogin_ReturnsNewTokens()
    {
        // Arrange: 登录获取RefreshToken
        var loginRequest = new LoginRequest
        {
            UserName = "admin",
            Password = WebApiFixture.AdminPassword
        };
        var loginResponse = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content
            .ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        var originalToken = loginBody!.Data!.Token;
        var refreshToken = loginBody.Data.RefreshToken;
        refreshToken.Should().NotBeNullOrWhiteSpace();

        // Act: 使用RefreshToken刷新
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        };
        var refreshResponse = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshBody = await refreshResponse.Content
            .ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        refreshBody!.Success.Should().BeTrue();
        refreshBody.Data!.Token.Should().NotBeNullOrWhiteSpace("刷新应返回新Token");
        refreshBody.Data.Token.Should().NotBe(originalToken, "新Token应与原Token不同");
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_Returns401()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalid_refresh_token_" + Guid.NewGuid()
        };

        // Act
        var response = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithEmptyToken_Returns400()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = ""
        };

        // Act
        var response = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert - Controller验证空Token返回400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_WithRefreshToken_Succeeds()
    {
        // Arrange: 登录获取RefreshToken
        var loginRequest = new LoginRequest
        {
            UserName = "doctor",
            Password = WebApiFixture.DoctorPassword
        };
        var loginResponse = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content
            .ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        var refreshToken = loginBody!.Data!.RefreshToken;

        // Act: 登出
        var logoutRequest = new LogoutRequest
        {
            RefreshToken = refreshToken,
            UserName = "doctor"
        };
        var logoutResponse = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_WithoutRefreshTokenOrUsername_Returns400()
    {
        // Arrange - 既不提供RefreshToken也不提供UserName
        var request = new LogoutRequest();

        // Act
        var response = await _fixture.AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/logout", request);

        // Assert - Controller验证"必须提供RefreshToken或用户名"
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
