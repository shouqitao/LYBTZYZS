using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features.Auth;

/// <summary>
/// Auto-login endpoint tests.
/// POST /api/v1/auth/auto-login
/// </summary>
[Collection("AuthUsers")]
public sealed class US_Auth_AutoLoginTests : IntegrationTestBase<AuthUsersFixture>
{
    public US_Auth_AutoLoginTests(AuthUsersFixture fixture) : base(fixture) { }

    [Fact]
    public async Task AutoLogin_InvalidToken_ReturnsUnauthorized()
    {
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/auto-login", new
        {
            UserName = "admin",
            AutoLoginToken = "invalid-token"
        });

        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task AutoLogin_EmptyToken_ReturnsUnauthorized()
    {
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/auto-login", new
        {
            UserName = "admin",
            AutoLoginToken = ""
        });

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AutoLogin_ValidAutoLoginToken_ReturnsNewToken()
    {
        var loginRequest = new LoginRequest { UserName = "admin", Password = "TestAdmin2025@" };
        var loginResp = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginData = await loginResp.ShouldBeSuccessWithDataAsync<LoginResponse>();

        var autoLoginRequest = new
        {
            UserName = "admin",
            AutoLoginToken = loginData.Token
        };
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/auto-login", autoLoginRequest);

        var data = await response.ShouldBeSuccessWithDataAsync<LoginResponse>(
            "Auto-login with valid token should return new token");
        data.Token.Should().NotBeNullOrWhiteSpace("JWT token must be present");
        data.User.Should().NotBeNull("user info must be returned");
        data.User.UserName.Should().Be("admin");
    }
}
