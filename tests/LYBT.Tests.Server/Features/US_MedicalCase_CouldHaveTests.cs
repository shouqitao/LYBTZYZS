using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.MedicalCases;

/// <summary>
/// Could Have User Stories for MedicalCases module.
/// PRD: US-MC-012 (Audit log for medical case operations)
/// Collection: ClinicalData (isolated DB, parallel with other domains)
/// </summary>
[Collection("ClinicalData")]
public sealed class US_MedicalCase_CouldHaveTests : IntegrationTestBase<ClinicalDataFixture>
{
    public US_MedicalCase_CouldHaveTests(ClinicalDataFixture fixture) : base(fixture) { }

    #region Helpers

    private async Task<Guid> CreatePatientAsync(HttpClient client)
    {
        var payload = PatientBuilder.Default()
            .WithName($"CH医案患者_{Guid.NewGuid():N}"[..12])
            .Build();
        var response = await client.PostAsJsonAsync("/api/v1/patients", payload);
        var data = await response.ShouldBeCreatedWithDataAsync<PatientDetailDto>();
        return data.Id;
    }

    private async Task<Guid> CreateCaseAsync(HttpClient doctorClient, Guid patientId)
    {
        var adminClient = await LoginAsAdminAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);
        var payload = MedicalCaseBuilder.Default()
            .ForPatient(patientId)
            .WithDoctor(doctorId)
            .BuildCreate();
        var response = await doctorClient.PostAsJsonAsync("/api/v1/medicalcases", payload);
        var data = await response.ShouldBeSuccessWithDataAsync<MedicalCaseDetailDto>();
        return data.Id;
    }

    #endregion

    #region US-MC-012: Audit log for medical case operations

    [Fact]
    public async Task US_MC_012_AuditLogs_AfterCaseCreation_Returns200()
    {
        // Arrange - create a case to generate audit events
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var caseId = await CreateCaseAsync(doctorClient, patientId);

        // Act - retrieve audit logs for the case
        var response = await doctorClient.GetAsync(
            $"/api/v1/medicalcases/{caseId}/audit-logs?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-MC-012: audit logs endpoint should return 200 for a created case");
    }

    [Fact]
    public async Task US_MC_012_AuditLogs_AnonymousAccess_RequiresAuth()
    {
        // Arrange - use a placeholder GUID (no auth)
        var fakeId = Guid.NewGuid();

        // Act
        var response = await AnonymousClient.GetAsync(
            $"/api/v1/medicalcases/{fakeId}/audit-logs?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "US-MC-012: audit logs endpoint must require authentication");
    }

    [Fact]
    public async Task US_MC_012_AuditLogs_NonExistentCase_Returns404Or200Empty()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var fakeId = Guid.NewGuid();

        // Act
        var response = await doctorClient.GetAsync(
            $"/api/v1/medicalcases/{fakeId}/audit-logs?page=1&pageSize=10");

        // Assert - either 404 or 200 with empty list is acceptable
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.NotFound, HttpStatusCode.OK },
            "US-MC-012: non-existent case should return 404 or empty 200");
    }

    #endregion
}
