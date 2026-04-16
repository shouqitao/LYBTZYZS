using System.Net;
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Auth;
using Refit;

namespace LYBT.Tests.Integration.Flows;

/// <summary>
/// Authentication flow integration tests.
/// Tests the full chain: Refit IAuthApi -> Server AuthController -> JWT token.
/// </summary>
[Collection("Integration")]
public class AuthFlowTests : IntegrationTestBase
{
    public AuthFlowTests(IntegrationFixture fixture) : base(fixture) { }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var authApi = Fixture.CreateApi<IAuthApi>(AnonymousClient);
        var request = new LoginRequest
        {
            UserName = "admin",
            Password = "TestAdmin2025@"
        };

        // Act
        var response = await authApi.LoginAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_Returns401()
    {
        // Arrange
        var authApi = Fixture.CreateApi<IAuthApi>(AnonymousClient);
        var request = new LoginRequest
        {
            UserName = "admin",
            Password = "WrongPassword123!"
        };

        // Act & Assert - server returns 401 which Refit throws as ApiException
        var ex = await Assert.ThrowsAsync<ApiException>(
            () => authApi.LoginAsync(request));

        ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginAsync_NonExistentUser_Returns401()
    {
        // Arrange
        var authApi = Fixture.CreateApi<IAuthApi>(AnonymousClient);
        var request = new LoginRequest
        {
            UserName = "nonexistent_user",
            Password = "SomePassword123!"
        };

        // Act & Assert - server returns 401 which Refit throws as ApiException
        var ex = await Assert.ThrowsAsync<ApiException>(
            () => authApi.LoginAsync(request));

        ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedEndpoint_WithValidToken_Returns200()
    {
        // Arrange - login to get authenticated client
        var client = await Fixture.LoginAsAdminAsync();

        // Act - access an authenticated endpoint (patients list)
        var response = await client.GetAsync("/api/v1/patients?page=1&pageSize=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthenticatedEndpoint_WithoutToken_Returns401()
    {
        // Act - access protected endpoint without token
        var response = await AnonymousClient.GetAsync("/api/v1/patients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Arrange
        var authApi = Fixture.CreateApi<IAuthApi>(AnonymousClient);

        // Act
        var response = await authApi.HealthCheckAsync();

        // Assert
        response.Should().NotBeNull();
        response.Data!.Status.Should().Be("Healthy");
    }
}
