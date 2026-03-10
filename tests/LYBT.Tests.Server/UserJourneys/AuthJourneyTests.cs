using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.UserJourneys;

/// <summary>
/// Auth journey: login, token validation, refresh, logout, anonymous access denial.
/// </summary>
[Collection("Auth")]
public sealed class AuthJourneyTests : JourneyTestBase<AuthFixture>
{
    public AuthJourneyTests(AuthFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Auth_Full_Journey()
    {
        // Step 1: Reset database
        await ResetForJourneyAsync();

        // Step 2: Admin login returns token
        var loginRequest = new LoginRequest { UserName = "admin", Password = "TestAdmin2025@" };
        var loginResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        loginBody!.Success.Should().BeTrue();
        loginBody.Data!.Token.Should().NotBeNullOrEmpty();
        loginBody.Data.RefreshToken.Should().NotBeNullOrEmpty();
        loginBody.Data.User.Should().NotBeNull();

        var adminToken = loginBody.Data.Token;

        // Step 3: Token can access protected endpoint
        var adminClient = await LoginAsAdminAsync();
        var validateResponse = await adminClient.GetAsync("/api/v1/auth/validate");
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 4: Doctor login returns token
        var doctorLoginRequest = new LoginRequest { UserName = "doctor", Password = "TestDoctor2025@" };
        var doctorLoginResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", doctorLoginRequest);
        doctorLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var doctorBody = await doctorLoginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        doctorBody!.Data!.User.Should().NotBeNull();

        // Step 5: Wrong password returns 401
        var badRequest = new LoginRequest { UserName = "admin", Password = "WrongPassword!" };
        var badResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", badRequest);
        badResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Step 6: Refresh token returns new token
        var freshLogin = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var freshBody = await freshLogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        var freshToken = freshBody!.Data!.Token;
        var freshRefreshToken = freshBody.Data.RefreshToken;
        freshRefreshToken.Should().NotBeNullOrEmpty();

        var refreshRequest = new RefreshTokenRequest { RefreshToken = freshRefreshToken };
        var refreshResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        refreshBody!.Data!.Token.Should().NotBe(freshToken, "should return a new token");
        refreshBody.Data.RefreshToken.Should().NotBeNullOrEmpty("should return a new refresh token");

        // Step 7: Logout succeeds
        var logoutClient = await LoginAsAdminAsync();
        var logoutRequest = new LogoutRequest { RefreshToken = freshRefreshToken };
        var logoutResponse = await logoutClient.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);
        ((int)logoutResponse.StatusCode).Should().BeLessThan(300);

        // Step 8: Anonymous cannot access protected endpoint
        var anonResponse = await AnonymousClient.GetAsync("/api/v1/users/current");
        anonResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
