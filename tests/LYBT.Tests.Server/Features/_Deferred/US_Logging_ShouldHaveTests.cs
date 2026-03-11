using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.Infrastructure;

/// <summary>
/// Should Have User Stories for Logging module.
/// PRD: US-LOG-001 (Structured logging), US-LOG-002 (Audit logging),
///      US-LOG-007 (API request logging)
/// Collection: Infrastructure (isolated DB, parallel with other domains)
/// </summary>
[Collection("Infrastructure")]
public sealed class US_Logging_ShouldHaveTests : IntegrationTestBase<InfraFixture>
{
    public US_Logging_ShouldHaveTests(InfraFixture fixture) : base(fixture) { }

    #region US-LOG-001: Structured logging status

    [Fact]
    public async Task US_LOG_001_LoggingStatus_RequiresSuperAdmin()
    {
        // Arrange - doctor should not access diagnostics
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.GetAsync("/api/v1/diagnostics/logging/status");

        // Assert - should be forbidden for non-SuperAdmin
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "US-LOG-001: logging status should require SuperAdmin role");
    }

    [Fact]
    public async Task US_LOG_001_LoggingStatus_SuperAdmin_ReturnsStatus()
    {
        // Arrange
        var sysAdminClient = await LoginAsSysAdminAsync();

        // Act
        var response = await sysAdminClient.GetAsync("/api/v1/diagnostics/logging/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-LOG-001: SuperAdmin should access logging status");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
        content.ToLowerInvariant().Should().Contain("level",
            "US-LOG-001: logging status should include level information");
    }

    #endregion

    #region US-LOG-002: Audit logging via medical case audit logs

    [Fact]
    public async Task US_LOG_002_MedicalCaseAuditLogs_ReturnsLogs()
    {
        // Arrange - create a medical case to generate audit logs
        var doctorClient = await LoginAsDoctorAsync();
        var adminClient = await LoginAsAdminAsync();

        var patientPayload = PatientBuilder.Default().WithName("审计日志患者").Build();
        var patientResp = await doctorClient.PostAsJsonAsync("/api/v1/patients", patientPayload);
        var patient = await patientResp.ShouldBeCreatedWithDataAsync<PatientDetailDto>();

        var doctorId = await GetDoctorUserIdAsync(adminClient);
        var casePayload = MedicalCaseBuilder.Default()
            .ForPatient(patient.Id)
            .WithDoctor(doctorId)
            .BuildCreate();
        var caseResp = await doctorClient.PostAsJsonAsync("/api/v1/medicalcases", casePayload);
        caseResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var caseBody = await caseResp.Content.ReadFromJsonAsync<
            LYBT.Shared.Models.Contracts.Common.ApiResponse<
                LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseDetailDto>>(JsonOptions);
        var caseId = caseBody!.Data!.Id;

        // Act - get audit logs
        var response = await doctorClient.GetAsync(
            $"/api/v1/medicalcases/{caseId}/audit-logs?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-LOG-002: audit logs endpoint should return 200");
    }

    #endregion

    #region US-LOG-007: API request logging (via CorrelationId)

    [Fact]
    public async Task US_LOG_007_ApiResponse_IncludesCorrelationMetadata()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act - any API call
        var response = await doctorClient.GetAsync("/api/v1/patients?page=1&pageSize=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check response body for requestId/correlationId
        var content = await response.Content.ReadAsStringAsync();
        var hasTracking = content.Contains("requestId", StringComparison.OrdinalIgnoreCase) ||
                          content.Contains("correlationId", StringComparison.OrdinalIgnoreCase);
        var hasTrackingHeader = response.Headers.Contains("X-Correlation-ID");

        (hasTracking || hasTrackingHeader).Should().BeTrue(
            "US-LOG-007: API responses should include request tracking metadata");
    }

    #endregion
}
