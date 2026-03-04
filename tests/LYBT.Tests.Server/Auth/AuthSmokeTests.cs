using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Auth;

/// <summary>
/// Smoke tests that validate the entire ServerFixture infrastructure works end-to-end:
/// database creation, migration, Respawn reset, user seeding, real login, authenticated access.
/// </summary>
public sealed class AuthSmokeTests : IntegrationTestBase
{
    public AuthSmokeTests(ServerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Login_WithValidAdmin_ShouldReturnToken()
    {
        // Act: real login through full HTTP pipeline
        var client = await LoginAsAdminAsync();

        // Assert: use the token to access a protected endpoint
        var response = await client.GetAsync("/api/v1/auth/validate");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturn401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            UserName = "admin",
            Password = "WrongPassword123@"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturn401()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
