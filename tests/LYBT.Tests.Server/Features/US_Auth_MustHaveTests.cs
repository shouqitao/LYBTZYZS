using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features.Auth;

/// <summary>
/// Must Have User Stories for Auth module.
/// PRD: US-AUTH-001 ~ US-AUTH-010 (8 Must Have)
/// Collection: Auth (isolated DB, parallel with other domains)
/// </summary>
[Collection("Auth")]
public sealed class US_Auth_MustHaveTests : IntegrationTestBase<AuthFixture>
{
    public US_Auth_MustHaveTests(AuthFixture fixture) : base(fixture) { }

    #region US-AUTH-001: User login with username and password

    [Fact]
    public async Task US_AUTH_001_LoginWithValidCredentials_ReturnsTokenAndUserInfo()
    {
        // Arrange
        var request = new LoginRequest { UserName = "admin", Password = "TestAdmin2025@" };

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert - business data validation, not just HTTP 200
        var data = await response.ShouldBeSuccessWithDataAsync<LoginResponse>(
            "US-AUTH-001: valid credentials should return token");
        data.Token.Should().NotBeNullOrWhiteSpace("JWT token must be present");
        data.RefreshToken.Should().NotBeNullOrWhiteSpace("refresh token must be present");
        data.ExpiresAt.Should().BeAfter(DateTime.UtcNow, "token must not be expired");
        data.User.Should().NotBeNull("user info must be returned");
        data.User.UserName.Should().Be("admin");
    }

    [Fact]
    public async Task US_AUTH_001_LoginWithInvalidPassword_Returns401()
    {
        // Arrange
        var request = new LoginRequest { UserName = "admin", Password = "WrongPassword1!" };

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task US_AUTH_001_LoginWithNonexistentUser_Returns401()
    {
        // Arrange
        var request = new LoginRequest { UserName = "nobody_exists", Password = "Test2025@" };

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task US_AUTH_001_LoginWithEmptyCredentials_Returns400Or401()
    {
        // Arrange
        var request = new LoginRequest { UserName = "", Password = "" };

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert - either 400 (validation) or 401 (auth failure)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region US-AUTH-002: Token-based authentication for API access

    [Fact]
    public async Task US_AUTH_002_AuthenticatedRequest_CanAccessProtectedEndpoint()
    {
        // Arrange
        var client = await LoginAsAdminAsync();

        // Act
        var response = await client.GetAsync("/api/v1/users/current");

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<UserDetailDto>(
            "US-AUTH-002: authenticated user should access protected endpoint");
        data.UserName.Should().Be("admin");
    }

    [Fact]
    public async Task US_AUTH_002_UnauthenticatedRequest_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/users/current");

        // Assert
        response.ShouldBeUnauthorized();
    }

    #endregion

    #region US-AUTH-003: Token refresh mechanism

    [Fact]
    public async Task US_AUTH_003_RefreshWithValidToken_ReturnsNewTokenPair()
    {
        // Arrange - login to get refresh token
        var loginRequest = new LoginRequest { UserName = "admin", Password = "TestAdmin2025@" };
        var loginResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginData = await loginResponse.ShouldBeSuccessWithDataAsync<LoginResponse>();
        var refreshToken = loginData.RefreshToken;

        // Act
        var refreshRequest = new RefreshTokenRequest { RefreshToken = refreshToken };
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<LoginResponse>(
            "US-AUTH-003: valid refresh token should return new token pair");
        data.Token.Should().NotBeNullOrWhiteSpace();
        data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        data.Token.Should().NotBe(loginData.Token, "new token should differ from old");
    }

    [Fact]
    public async Task US_AUTH_003_RefreshWithInvalidToken_ReturnsError()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "invalid-refresh-token-value" };

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "US-AUTH-003: invalid refresh token should not succeed");
    }

    #endregion

    #region US-AUTH-005: Logout functionality

    [Fact]
    public async Task US_AUTH_005_Logout_InvalidatesRefreshToken()
    {
        // Arrange - login to get tokens
        var loginRequest = new LoginRequest { UserName = "doctor", Password = "TestDoctor2025@" };
        var loginResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginData = await loginResponse.ShouldBeSuccessWithDataAsync<LoginResponse>();

        // Act - logout with refresh token
        var logoutRequest = new LogoutRequest { RefreshToken = loginData.RefreshToken };
        var logoutResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert - refresh should fail after logout
        var refreshRequest = new RefreshTokenRequest { RefreshToken = loginData.RefreshToken };
        var refreshResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);
        refreshResponse.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "US-AUTH-005: refresh token should be invalid after logout");
    }

    [Fact]
    public async Task US_AUTH_005_Logout_WithInvalidRefreshToken_HandledGracefully()
    {
        // Arrange
        var logoutRequest = new LogoutRequest { RefreshToken = "invalid-token-value" };

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

        // Assert - should not crash, either OK (idempotent) or error
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity },
            "US-AUTH-005: logout with invalid token should not cause server error");
    }

    [Fact]
    public async Task US_AUTH_005_DoubleLogout_HandledGracefully()
    {
        // Arrange - login to get tokens
        var loginRequest = new LoginRequest { UserName = "doctor", Password = "TestDoctor2025@" };
        var loginResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginData = await loginResponse.ShouldBeSuccessWithDataAsync<LoginResponse>();

        // First logout
        var logoutRequest = new LogoutRequest { RefreshToken = loginData.RefreshToken };
        await AnonymousClient.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

        // Act - second logout with same token
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

        // Assert - idempotent: should not crash
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity },
            "US-AUTH-005: double logout should be handled gracefully");
    }

    #endregion

    #region US-AUTH-007: Token validation endpoint

    [Fact]
    public async Task US_AUTH_007_ValidateWithValidToken_ReturnsSuccess()
    {
        // Arrange
        var client = await LoginAsAdminAsync();

        // Act
        var response = await client.GetAsync("/api/v1/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-AUTH-007: valid JWT should pass validation");
    }

    [Fact]
    public async Task US_AUTH_007_ValidateWithoutToken_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/auth/validate");

        // Assert
        response.ShouldBeUnauthorized();
    }

    #endregion

    #region US-AUTH-008: Role-based access control (RBAC)

    [Fact]
    public async Task US_AUTH_008_DoctorCannotAccessAdminEndpoint()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act - try to access AdminOnly endpoint (user management)
        var response = await doctorClient.GetAsync("/api/v1/users?page=1&pageSize=10");

        // Assert
        response.ShouldBeForbidden();
    }

    [Fact]
    public async Task US_AUTH_008_AdminCanAccessAdminEndpoint()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();

        // Act
        var response = await adminClient.GetAsync("/api/v1/users?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-AUTH-008: admin should access admin endpoints");
    }

    [Fact]
    public async Task US_AUTH_008_DoctorCanAccessPatientEndpoint()
    {
        // Arrange - PatientAccess policy allows Doctor
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.GetAsync("/api/v1/patients?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-AUTH-008: doctor should access patient endpoints");
    }

    #endregion

    #region US-AUTH-009: Password policy enforcement

    [Fact]
    public async Task US_AUTH_009_LoginWithAllRoles_ReturnsCorrectRoleInfo()
    {
        // Arrange & Act - login as each role
        var adminLogin = new LoginRequest { UserName = "admin", Password = "TestAdmin2025@" };
        var adminResp = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", adminLogin);
        var adminData = await adminResp.ShouldBeSuccessWithDataAsync<LoginResponse>();

        var doctorLogin = new LoginRequest { UserName = "doctor", Password = "TestDoctor2025@" };
        var doctorResp = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", doctorLogin);
        var doctorData = await doctorResp.ShouldBeSuccessWithDataAsync<LoginResponse>();

        // Assert - each role has correct role info in response
        adminData.User.Role.Should().Be(Shared.Models.Enums.UserRole.Admin);
        doctorData.User.Role.Should().Be(Shared.Models.Enums.UserRole.Doctor);
    }

    #endregion

    #region US-AUTH-010: Auto-login token mechanism

    [Fact]
    public async Task US_AUTH_010_LoginWithRememberMe_ReturnsAutoLoginToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "admin",
            Password = "TestAdmin2025@",
            RememberMe = true
        };

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<LoginResponse>(
            "US-AUTH-010: RememberMe login should succeed");
        data.AutoLoginToken.Should().NotBeNullOrWhiteSpace(
            "US-AUTH-010: auto-login token should be returned when RememberMe=true");
    }

    [Fact]
    public async Task US_AUTH_010_LoginWithoutRememberMe_NoAutoLoginToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "admin",
            Password = "TestAdmin2025@",
            RememberMe = false
        };

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<LoginResponse>();
        data.AutoLoginToken.Should().BeNullOrWhiteSpace(
            "US-AUTH-010: no auto-login token without RememberMe");
    }

    [Fact]
    public async Task US_AUTH_010_AutoLoginWithValidToken_ReturnsNewSession()
    {
        // Arrange - login with RememberMe to get auto-login token
        var loginRequest = new LoginRequest
        {
            UserName = "admin",
            Password = "TestAdmin2025@",
            RememberMe = true
        };
        var loginResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginData = await loginResponse.ShouldBeSuccessWithDataAsync<LoginResponse>();

        // Act - auto-login with the token
        var autoLoginRequest = new AutoLoginRequest
        {
            UserName = "admin",
            AutoLoginToken = loginData.AutoLoginToken!
        };
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/auto-login", autoLoginRequest);

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<LoginResponse>(
            "US-AUTH-010: auto-login with valid token should succeed");
        data.Token.Should().NotBeNullOrWhiteSpace();
        data.User.UserName.Should().Be("admin");
    }

    #endregion
}
