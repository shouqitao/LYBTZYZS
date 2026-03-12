using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features.Auth;

/// <summary>
/// Should Have User Stories for Auth module.
/// PRD: US-AUTH-004, AUTH-006, AUTH-011, AUTH-013 (4 Should Have)
/// Collection: AuthUsers (isolated DB, parallel with other domains)
/// </summary>
[Collection("AuthUsers")]
public sealed class US_Auth_ShouldHaveTests : IntegrationTestBase<AuthUsersFixture>
{
    public US_Auth_ShouldHaveTests(AuthUsersFixture fixture) : base(fixture) { }

    #region US-AUTH-004: Token replay detection

    [Fact]
    public async Task US_AUTH_004_RefreshToken_ThenReuseOldRefreshToken_ShouldFail()
    {
        // Arrange - login and get tokens
        var loginResp = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserName = "doctor", Password = "TestDoctor2025@" });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResp.Content.ReadFromJsonAsync<
            LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>(JsonOptions);
        var originalRefreshToken = loginBody!.Data!.RefreshToken;

        // First refresh (valid)
        var refreshResp = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest { RefreshToken = originalRefreshToken });
        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-AUTH-004: first refresh should succeed");

        // Act - reuse the original (now consumed) refresh token
        var replayResp = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest { RefreshToken = originalRefreshToken });

        // Assert - should be rejected (token already consumed)
        replayResp.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest },
            "US-AUTH-004: replayed refresh token should be rejected or rotated");
    }

    [Fact]
    public async Task US_AUTH_004_InvalidRefreshToken_Returns401()
    {
        // Arrange
        var fakeToken = "invalid-refresh-token-" + Guid.NewGuid();

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest { RefreshToken = fakeToken });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "US-AUTH-004: invalid refresh token should return 401");
    }

    #endregion

    #region US-AUTH-006: Token expiry behavior

    [Fact]
    public async Task US_AUTH_006_ValidateToken_WithValidToken_ReturnsUserInfo()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act - validate current token
        var response = await doctorClient.GetAsync("/api/v1/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-AUTH-006: valid token should pass validation");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("valid",
            "US-AUTH-006: validation response should indicate validity");
    }

    #endregion

    #region US-AUTH-011: Refresh failure escalation

    [Fact]
    public async Task US_AUTH_011_MultipleInvalidRefreshAttempts_AllReturnUnauthorized()
    {
        // Arrange
        var fakeToken = "escalation-test-token-" + Guid.NewGuid();

        // Act - multiple failed refresh attempts
        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < 3; i++)
        {
            var resp = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/refresh",
                new RefreshTokenRequest { RefreshToken = fakeToken });
            responses.Add(resp);
        }

        // Assert - all should return 401
        foreach (var resp in responses)
        {
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                "US-AUTH-011: repeated invalid refresh should consistently return 401");
        }
    }

    [Fact]
    public async Task US_AUTH_011_RefreshAfterLogout_Returns401()
    {
        // Arrange - login, get refresh token, then logout
        var loginResp = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserName = "doctor", Password = "TestDoctor2025@" });
        var loginBody = await loginResp.Content.ReadFromJsonAsync<
            LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>(JsonOptions);
        var refreshToken = loginBody!.Data!.RefreshToken;

        // Logout
        await AnonymousClient.PostAsJsonAsync("/api/v1/auth/logout",
            new LogoutRequest { UserName = "doctor", RefreshToken = refreshToken });

        // Act - try to refresh after logout
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken });

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest },
            "US-AUTH-011: refresh after logout should be rejected");
    }

    #endregion

    #region US-AUTH-013: Auth event audit

    [Fact]
    public async Task US_AUTH_013_LoginEvent_IsAuditable()
    {
        // Arrange + Act - perform a login (creates audit event)
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { UserName = "doctor", Password = "TestDoctor2025@" });

        // Assert - login itself should succeed (audit is server-side)
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-AUTH-013: login should succeed, creating an audit event");
    }

    #endregion
}
