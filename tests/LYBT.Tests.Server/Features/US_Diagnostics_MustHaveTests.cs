using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features.Diagnostics;

/// <summary>
/// Must Have User Stories for Diagnostics module.
/// PRD: US-DIAG-001 (logging management endpoints)
/// Collection: SystemOps (isolated DB, parallel with other domains)
/// </summary>
[Collection("SystemOps")]
public sealed class US_Diagnostics_MustHaveTests : IntegrationTestBase<SystemOpsFixture>
{
    public US_Diagnostics_MustHaveTests(SystemOpsFixture fixture) : base(fixture) { }

    #region US-DIAG-001: Logging status and management

    [Fact]
    public async Task US_DIAG_001_GetLoggingStatus_AsSuperAdmin_ReturnsStatus()
    {
        // Arrange
        var sysAdminClient = await LoginAsSysAdminAsync();

        // Act
        var response = await sysAdminClient.GetAsync("/api/v1/diagnostics/logging/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-DIAG-001: SuperAdmin should access logging status");
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = content.GetProperty("data");
        data.GetProperty("currentLevel").GetString()
            .Should().NotBeNullOrWhiteSpace("US-DIAG-001: current log level must be present");
        data.GetProperty("defaultLevel").GetString()
            .Should().NotBeNullOrWhiteSpace("US-DIAG-001: default log level must be present");
    }

    [Fact]
    public async Task US_DIAG_001_GetLoggingStatus_Anonymous_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/diagnostics/logging/status");

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task US_DIAG_001_GetLoggingStatus_AsDoctor_Returns403()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.GetAsync("/api/v1/diagnostics/logging/status");

        // Assert
        response.ShouldBeForbidden();
    }

    [Fact]
    public async Task US_DIAG_001_EnableDebugMode_AsSuperAdmin_ReturnsDebugInfo()
    {
        // Arrange
        var sysAdminClient = await LoginAsSysAdminAsync();

        // Act
        var response = await sysAdminClient.PostAsJsonAsync(
            "/api/v1/diagnostics/logging/debug/enable",
            new { Level = "debug", DurationMinutes = 5 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-DIAG-001: SuperAdmin should enable debug mode");
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = content.GetProperty("data");
        data.GetProperty("message").GetString()
            .Should().NotBeNullOrWhiteSpace("US-DIAG-001: response message must be present");
        data.GetProperty("currentLevel").GetString()
            .Should().NotBeNullOrWhiteSpace("US-DIAG-001: current level must be present");
    }

    [Fact]
    public async Task US_DIAG_001_EnableDebugMode_WithoutBody_UsesDefaults()
    {
        // Arrange
        var sysAdminClient = await LoginAsSysAdminAsync();

        // Act
        var response = await sysAdminClient.PostAsync(
            "/api/v1/diagnostics/logging/debug/enable",
            JsonContent.Create<object?>(null));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-DIAG-001: enable debug with no body should use defaults");
    }

    [Fact]
    public async Task US_DIAG_001_DisableDebugMode_AsSuperAdmin_ReturnsStatus()
    {
        // Arrange
        var sysAdminClient = await LoginAsSysAdminAsync();

        // Act
        var response = await sysAdminClient.PostAsync(
            "/api/v1/diagnostics/logging/debug/disable", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-DIAG-001: SuperAdmin should disable debug mode");
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = content.GetProperty("data");
        data.GetProperty("message").GetString()
            .Should().NotBeNullOrWhiteSpace("US-DIAG-001: response message must be present");
    }

    [Fact]
    public async Task US_DIAG_001_SetLoggingLevel_ValidLevel_ReturnsSuccess()
    {
        // Arrange
        var sysAdminClient = await LoginAsSysAdminAsync();

        // Act
        var response = await sysAdminClient.PostAsJsonAsync(
            "/api/v1/diagnostics/logging/level",
            new { Level = "Warning" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-DIAG-001: SuperAdmin should set logging level");
        var content = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var data = content.GetProperty("data");
        data.GetProperty("currentLevel").GetString()
            .Should().NotBeNullOrWhiteSpace("US-DIAG-001: current level must be present");
    }

    [Fact]
    public async Task US_DIAG_001_SetLoggingLevel_EmptyLevel_ReturnsBadRequest()
    {
        // Arrange
        var sysAdminClient = await LoginAsSysAdminAsync();

        // Act
        var response = await sysAdminClient.PostAsJsonAsync(
            "/api/v1/diagnostics/logging/level",
            new { Level = "" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "US-DIAG-001: empty level should return 400");
    }

    [Fact]
    public async Task US_DIAG_001_SetLoggingLevel_InvalidLevel_ReturnsBadRequest()
    {
        // Arrange
        var sysAdminClient = await LoginAsSysAdminAsync();

        // Act
        var response = await sysAdminClient.PostAsJsonAsync(
            "/api/v1/diagnostics/logging/level",
            new { Level = "NotAValidLevel" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "US-DIAG-001: invalid level should return 400");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("error",
            "US-DIAG-001: bad request should include error description");
    }

    [Fact]
    public async Task US_DIAG_001_EnableDebugMode_AsDoctor_Returns403()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.PostAsJsonAsync(
            "/api/v1/diagnostics/logging/debug/enable",
            new { Level = "debug", DurationMinutes = 5 });

        // Assert
        response.ShouldBeForbidden();
    }

    #endregion
}
