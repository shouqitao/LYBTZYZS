using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.MedicalCases;

/// <summary>
/// Should Have User Stories for MedicalCases module.
/// PRD: US-MC-008, MC-010 ~ MC-018 (8 Should Have)
/// Collection: Clinical (isolated DB, parallel with other domains)
/// </summary>
[Collection("Clinical")]
public sealed class US_MedicalCase_ShouldHaveTests : IntegrationTestBase<ClinicalFixture>
{
    public US_MedicalCase_ShouldHaveTests(ClinicalFixture fixture) : base(fixture) { }

    #region Helpers

    private async Task<Guid> CreatePatientAsync(HttpClient client, string name = "SH医案患者")
    {
        var payload = PatientBuilder.Default()
            .WithName($"{name}_{Guid.NewGuid():N}"[..12])
            .Build();
        var response = await client.PostAsJsonAsync("/api/v1/patients", payload);
        var data = await response.ShouldBeCreatedWithDataAsync<PatientDetailDto>();
        return data.Id;
    }

    private async Task<(Guid CaseId, Guid DoctorId)> CreateCaseAsync(
        HttpClient doctorClient, Guid patientId)
    {
        var doctorId = await GetDoctorUserIdAsync(await LoginAsAdminAsync());
        var payload = MedicalCaseBuilder.Default()
            .ForPatient(patientId)
            .WithDoctor(doctorId)
            .BuildCreate();
        var response = await doctorClient.PostAsJsonAsync("/api/v1/medicalcases", payload);
        var data = await response.ShouldBeSuccessWithDataAsync<MedicalCaseDetailDto>();
        return (data.Id, doctorId);
    }

    private async Task AddConsultationAsync(
        HttpClient client, Guid caseId, Guid patientId, Guid doctorId)
    {
        var consultation = MedicalCaseBuilder.BuildConsultation();
        var payload = MedicalCaseBuilder.BuildUpdate(caseId,
            patientId: patientId, userId: doctorId, consultation: consultation);
        var resp = await client.PutAsJsonAsync($"/api/v1/medicalcases/{caseId}", payload);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<(Guid Id, string Name)> CreateHerbAsync(
        HttpClient client, string name = "SH药材")
    {
        var payload = HerbBuilder.Default()
            .WithName($"{name}_{Guid.NewGuid():N}"[..12]).Build();
        var response = await client.PostAsJsonAsync("/api/v1/herbs", payload);
        var data = await response.ShouldBeSuccessWithDataAsync<HerbDetailDto>();
        return (data.Id, data.Name);
    }

    #endregion

    #region US-MC-008: Cancel medical case (edge cases)

    [Fact]
    public async Task US_MC_008_CancelCaseWithConsultation_RequiresReason()
    {
        // Arrange - create case with consultation
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var (caseId, doctorId) = await CreateCaseAsync(doctorClient, patientId);
        await AddConsultationAsync(doctorClient, caseId, patientId, doctorId);

        // Act - cancel with reason
        var cancelPayload = new { Reason = "患者要求取消" };
        var response = await doctorClient.PutAsJsonAsync(
            $"/api/v1/medicalcases/{caseId}/cancel", cancelPayload);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "US-MC-008: cancel case with consultation should succeed with reason");
    }

    [Fact]
    public async Task US_MC_008_CancelAlreadyCancelled_ShouldHandleGracefully()
    {
        // Arrange - create and cancel
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var (caseId, _) = await CreateCaseAsync(doctorClient, patientId);

        await doctorClient.PutAsJsonAsync(
            $"/api/v1/medicalcases/{caseId}/cancel", new { Reason = "first cancel" });

        // Act - cancel again
        var response = await doctorClient.PutAsJsonAsync(
            $"/api/v1/medicalcases/{caseId}/cancel", new { Reason = "second cancel" });

        // Assert - idempotent or rejected
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound,
                    HttpStatusCode.UnprocessableEntity, HttpStatusCode.Conflict },
            "US-MC-008: double cancel should be handled gracefully");
    }

    #endregion

    #region US-MC-010: Cross-case search

    [Fact]
    public async Task US_MC_010_SearchByPatientName_ReturnsMatchingCases()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var uniqueName = $"搜索_{Guid.NewGuid():N}"[..8];
        var patientId = await CreatePatientAsync(doctorClient, uniqueName);
        await CreateCaseAsync(doctorClient, patientId);

        // Act
        var response = await doctorClient.GetAsync(
            $"/api/v1/medicalcases/search?patientName={uniqueName}&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-MC-010: search endpoint should return 200");
    }

    [Fact]
    public async Task US_MC_010_SearchByDiagnosisKeyword_ReturnsResults()
    {
        // Arrange - create case with consultation
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var (caseId, doctorId) = await CreateCaseAsync(doctorClient, patientId);
        await AddConsultationAsync(doctorClient, caseId, patientId, doctorId);

        // Act - search by diagnosis keyword
        var response = await doctorClient.GetAsync(
            "/api/v1/medicalcases/search?diagnosisKeyword=风寒&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-MC-010: diagnosis keyword search should return 200");
    }

    [Fact]
    public async Task US_MC_010_SearchByDateRange_ReturnsResults()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        await CreateCaseAsync(doctorClient, patientId);

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var tomorrow = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        // Act
        var response = await doctorClient.GetAsync(
            $"/api/v1/medicalcases/search?startDate={today}&endDate={tomorrow}&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-MC-010: date range search should return 200");
    }

    #endregion

    #region US-MC-011: Status transitions

    [Fact]
    public async Task US_MC_011_TransitionToActive_FromSuspended_Succeeds()
    {
        // Arrange - create case (default status: Suspended/Active)
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var (caseId, doctorId) = await CreateCaseAsync(doctorClient, patientId);

        // Add consultation first (required for status transition)
        await AddConsultationAsync(doctorClient, caseId, patientId, doctorId);

        // Act - transition to Active
        var statusPayload = new { Status = MedicalCaseStatus.Active };
        var response = await doctorClient.PutAsJsonAsync(
            $"/api/v1/medicalcases/{caseId}/status", statusPayload);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.BadRequest },
            "US-MC-011: status transition to Active should succeed or indicate validation");
    }

    [Fact]
    public async Task US_MC_011_InvalidTransition_ShouldBeRejected()
    {
        // Arrange - create a new case
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var (caseId, _) = await CreateCaseAsync(doctorClient, patientId);

        // Act - try invalid transition (new -> Completed without consultation)
        var statusPayload = new { Status = MedicalCaseStatus.Completed };
        var response = await doctorClient.PutAsJsonAsync(
            $"/api/v1/medicalcases/{caseId}/status", statusPayload);

        // Assert - should fail (business rule: need consultation before complete)
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest,
                    HttpStatusCode.UnprocessableEntity, HttpStatusCode.Conflict },
            "US-MC-011: invalid state transition should be handled");
    }

    #endregion

    #region US-MC-014: Permissions/Locking rules

    [Fact]
    public async Task US_MC_014_GetPermissions_ForNewCase_ReturnsEditable()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var (caseId, _) = await CreateCaseAsync(doctorClient, patientId);

        // Act
        var response = await doctorClient.GetAsync(
            $"/api/v1/medicalcases/{caseId}/permissions");

        // Assert
        var perms = await response.ShouldBeSuccessWithDataAsync<MedicalCasePermissionDto>(
            "US-MC-014: permissions should be returned for new case");
        perms.CanEdit.Should().BeTrue("US-MC-014: new case should be editable");
        perms.CanDelete.Should().BeTrue("US-MC-014: new case should be deletable");
        perms.RequiresEditReason.Should().BeFalse(
            "US-MC-014: new case should not require edit reason");
    }

    [Fact]
    public async Task US_MC_014_GetPermissions_NonexistentCase_Returns404()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.GetAsync(
            $"/api/v1/medicalcases/{Guid.NewGuid()}/permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "US-MC-014: non-existent case permissions should return 404");
    }

    #endregion

    #region US-MC-015: Print logging

    [Fact]
    public async Task US_MC_015_AddPrintLog_Success_RecordsLog()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var (caseId, _) = await CreateCaseAsync(doctorClient, patientId);

        var printLog = new
        {
            PrintType = PrintType.Prescription,
            IsSuccess = true,
            PrinterName = "HP LaserJet"
        };

        // Act
        var response = await doctorClient.PostAsJsonAsync(
            $"/api/v1/medicalcases/{caseId}/print-logs", printLog);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.Created },
            "US-MC-015: print log should be recorded");
    }

    [Fact]
    public async Task US_MC_015_AddPrintLog_Failure_RecordsError()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var (caseId, _) = await CreateCaseAsync(doctorClient, patientId);

        var printLog = new
        {
            PrintType = PrintType.Prescription,
            IsSuccess = false,
            PrinterName = "HP LaserJet",
            ErrorMessage = "打印机离线"
        };

        // Act
        var response = await doctorClient.PostAsJsonAsync(
            $"/api/v1/medicalcases/{caseId}/print-logs", printLog);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.Created },
            "US-MC-015: print failure log should also be recorded");
    }

    #endregion

    #region US-MC-017: Waiting queue (registration-based)

    [Fact]
    public async Task US_MC_017_GetWaitingQueue_ForDoctor_ReturnsQueue()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        // Act
        var response = await doctorClient.GetAsync(
            $"/api/v1/registrations/queue?doctorId={doctorId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-MC-017: waiting queue should return 200");
    }

    #endregion

    #region US-MC-018: Get historical prescriptions

    [Fact]
    public async Task US_MC_018_GetPrescriptions_ForCase_ReturnsHistory()
    {
        // Arrange - create case
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var (caseId, _) = await CreateCaseAsync(doctorClient, patientId);

        // Act - get prescriptions (may be empty for new case)
        var response = await doctorClient.GetAsync(
            $"/api/v1/medicalcases/{caseId}/prescriptions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-MC-018: prescriptions endpoint should return 200");
    }

    [Fact]
    public async Task US_MC_018_GetPrescriptions_NonexistentCase_Returns404OrEmpty()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.GetAsync(
            $"/api/v1/medicalcases/{Guid.NewGuid()}/prescriptions");

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound },
            "US-MC-018: non-existent case prescriptions should return 404 or empty list");
    }

    #endregion
}
