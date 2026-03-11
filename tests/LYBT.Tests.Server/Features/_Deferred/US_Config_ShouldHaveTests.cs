using System.Net;
using System.Text.Json;
using FluentAssertions;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features.Infrastructure;

/// <summary>
/// Should Have User Stories for Configuration module.
/// PRD: US-CFG-003 (Environment config), US-CFG-004 (Startup validation)
/// Collection: Infrastructure (isolated DB, parallel with other domains)
/// </summary>
[Collection("Infrastructure")]
public sealed class US_Config_ShouldHaveTests : IntegrationTestBase<InfraFixture>
{
    public US_Config_ShouldHaveTests(InfraFixture fixture) : base(fixture) { }

    #region US-CFG-003: Environment config via diagnostics

    [Fact]
    public async Task US_CFG_003_HealthDetails_ReturnsEnvironmentInfo()
    {
        // Arrange - health details requires authentication
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.GetAsync("/api/v1/health/details");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-CFG-003: health details should return 200 for authenticated user");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();

        // Should contain database info
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        var hasStatus = root.TryGetProperty("status", out _) ||
                        root.TryGetProperty("data", out _);
        hasStatus.Should().BeTrue(
            "US-CFG-003: health details should include status information");
    }

    #endregion

    #region US-CFG-004: Startup validation via health check

    [Fact]
    public async Task US_CFG_004_HealthPing_ReturnsHealthy()
    {
        // Arrange - anonymous access to ping
        var client = AnonymousClient;

        // Act
        var response = await client.GetAsync("/api/v1/health/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-CFG-004: health ping should return 200");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("pong",
            "US-CFG-004: ping should return pong");
    }

    [Fact]
    public async Task US_CFG_004_HealthEndpoint_ReturnsHealthStatus()
    {
        // Arrange
        var client = AnonymousClient;

        // Act
        var response = await client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-CFG-004: main health endpoint should return 200");
        var content = await response.Content.ReadAsStringAsync();
        content.ToLowerInvariant().Should().Contain("healthy",
            "US-CFG-004: health status should indicate Healthy");
    }

    #endregion
}
