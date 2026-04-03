using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features.Health;

/// <summary>
/// Must Have User Stories for Health module.
/// PRD: US-HEALTH-001 (basic health check endpoints)
/// Collection: SystemOps (isolated DB, parallel with other domains)
/// </summary>
[Collection("SystemOps")]
public sealed class US_Health_MustHaveTests : IntegrationTestBase<SystemOpsFixture>
{
    public US_Health_MustHaveTests(SystemOpsFixture fixture) : base(fixture) { }

    #region US-HEALTH-001: Health check endpoints

    [Fact]
    public async Task US_HEALTH_001_GetHealth_Anonymous_ReturnsHealthyStatus()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-HEALTH-001: health endpoint should be publicly accessible");
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        content.GetProperty("data").GetProperty("status").GetString()
            .Should().Be("Healthy", "US-HEALTH-001: health status should be Healthy");
        content.GetProperty("data").GetProperty("timestamp").GetString()
            .Should().NotBeNullOrWhiteSpace("US-HEALTH-001: timestamp must be present");
    }

    [Fact]
    public async Task US_HEALTH_001_Ping_Anonymous_ReturnsPong()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/health/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-HEALTH-001: ping endpoint should be publicly accessible");
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        content.GetProperty("data").GetProperty("message").GetString()
            .Should().Be("pong", "US-HEALTH-001: ping should respond with pong");
        content.GetProperty("data").GetProperty("timestamp").GetString()
            .Should().NotBeNullOrWhiteSpace("US-HEALTH-001: timestamp must be present");
    }

    [Fact]
    public async Task US_HEALTH_001_Details_Authenticated_ReturnsDatabaseHealth()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();

        // Act
        var response = await adminClient.GetAsync("/api/v1/health/details");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-HEALTH-001: details endpoint should return 200 for authenticated user");
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = content.GetProperty("data");
        data.GetProperty("status").GetString()
            .Should().NotBeNullOrWhiteSpace("US-HEALTH-001: health status must be present");
        data.GetProperty("timestamp").GetString()
            .Should().NotBeNullOrWhiteSpace("US-HEALTH-001: timestamp must be present");
        data.GetProperty("database").ValueKind
            .Should().NotBe(JsonValueKind.Undefined, "US-HEALTH-001: database info must be present");
    }

    [Fact]
    public async Task US_HEALTH_001_Details_Anonymous_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/health/details");

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task US_HEALTH_001_GetHealth_Authenticated_StillWorks()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-HEALTH-001: authenticated users should also access health endpoint");
    }

    [Fact]
    public async Task US_HEALTH_001_Ping_Authenticated_StillWorks()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.GetAsync("/api/v1/health/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-HEALTH-001: authenticated users should also access ping endpoint");
    }

    #endregion
}
