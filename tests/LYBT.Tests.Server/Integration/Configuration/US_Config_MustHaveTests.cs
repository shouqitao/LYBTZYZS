using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features;

/// <summary>
/// Must Have User Stories for Config/Infrastructure module.
/// PRD: US-CFG-001 ~ US-CFG-002 (2 Must Have)
/// Collection: SystemOps (isolated DB, parallel with other domains)
/// </summary>
[Collection("SystemOps")]
public sealed class US_Config_MustHaveTests : IntegrationTestBase<SystemOpsFixture>
{
    public US_Config_MustHaveTests(SystemOpsFixture fixture) : base(fixture) { }

    #region US-CFG-001: Health check endpoint

    [Fact]
    public async Task US_CFG_001_HealthCheck_ReturnsHealthyStatus()
    {
        // Act - health check is anonymous
        var response = await AnonymousClient.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-CFG-001: health check should return 200");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();

        var json = JsonDocument.Parse(content);
        if (json.RootElement.TryGetProperty("Data", out var data))
        {
            if (data.TryGetProperty("status", out var statusProp))
                statusProp.GetString().Should().Be("Healthy",
                    "US-CFG-001: system should be healthy");
        }
        else if (json.RootElement.TryGetProperty("status", out var statusProp))
        {
            statusProp.GetString().Should().Be("Healthy",
                "US-CFG-001: system should be healthy");
        }
    }

    [Fact]
    public async Task US_CFG_001_HealthPing_ReturnsOk()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/health/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-CFG-001: ping should return 200");
    }

    [Fact]
    public async Task US_CFG_001_HealthDetails_ReturnsDatabaseInfo()
    {
        // Arrange - detailed health check requires authentication
        var client = await LoginAsAdminAsync();

        // Act
        var response = await client.GetAsync("/api/v1/health/details");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-CFG-001: detailed health check should return 200");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        if (json.RootElement.TryGetProperty("Data", out var dataElement))
        {
            if (dataElement.TryGetProperty("database", out var dbProp))
            {
                if (dbProp.TryGetProperty("status", out var dbStatus))
                    dbStatus.GetString().Should().Be("Healthy",
                        "US-CFG-001: database should be healthy");
            }
        }
        else if (json.RootElement.TryGetProperty("database", out var dbProp))
        {
            if (dbProp.TryGetProperty("status", out var dbStatus))
                dbStatus.GetString().Should().Be("Healthy",
                    "US-CFG-001: database should be healthy");
        }
    }

    #endregion

    #region US-CFG-002: Diagnostics endpoint (SuperAdmin only)

    [Fact]
    public async Task US_CFG_002_Diagnostics_SuperAdminCanAccess()
    {
        // Arrange
        var sysAdminClient = await LoginAsSysAdminAsync();

        // Act
        var response = await sysAdminClient.GetAsync("/api/v1/diagnostics/logging/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-CFG-002: sysadmin should access diagnostics");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task US_CFG_002_Diagnostics_DoctorCannotAccess_Returns403()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.GetAsync("/api/v1/diagnostics/logging/status");

        // Assert
        response.ShouldBeForbidden();
    }

    [Fact]
    public async Task US_CFG_002_Diagnostics_AdminCannotAccess_Returns403()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();

        // Act
        var response = await adminClient.GetAsync("/api/v1/diagnostics/logging/status");

        // Assert
        response.ShouldBeForbidden();
    }

    [Fact]
    public async Task US_CFG_002_Diagnostics_AnonymousReturns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/diagnostics/logging/status");

        // Assert
        response.ShouldBeUnauthorized();
    }

    #endregion
}
