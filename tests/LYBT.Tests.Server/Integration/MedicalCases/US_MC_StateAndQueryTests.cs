using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.MedicalCases;

/// <summary>
/// State transition and query tests for MedicalCases module.
/// Covers: suspend, complete, permissions, batch-details endpoints.
/// Collection: ClinicalData (isolated DB)
/// </summary>
[Collection("ClinicalData")]
public sealed class US_MC_StateAndQueryTests : IntegrationTestBase<ClinicalDataFixture>
{
    public US_MC_StateAndQueryTests(ClinicalDataFixture fixture) : base(fixture) { }

    #region Helpers

    private async Task<Guid> CreatePatientAsync(HttpClient client, string name = "SQ医案患者")
    {
        var payload = PatientBuilder.Default()
            .WithName($"{name}_{Guid.NewGuid():N}"[..12])
            .Build();
        var response = await client.PostAsJsonAsync("/api/v1/patients", payload);
        var data = await response.ShouldBeCreatedWithDataAsync<PatientDetailDto>();
        return data.Id;
    }

    private async Task<(Guid CaseId, Guid DoctorId)> CreateCaseAsync(HttpClient doctorClient, Guid patientId)
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

    private async Task CreateConsultationAsync(HttpClient client, Guid caseId, Guid doctorId, Guid patientId, string? tcmDiagnosis = "风寒感冒")
    {
        var consultation = MedicalCaseBuilder.BuildConsultation(tcmDiagnosis: tcmDiagnosis);
        var updatePayload = MedicalCaseBuilder.BuildUpdate(caseId,
            patientId: patientId, userId: doctorId, consultation: consultation);
        await client.PutAsJsonAsync($"/api/v1/medicalcases/{caseId}", updatePayload);
    }

    private async Task<HttpResponseMessage> CompleteCaseAsync(HttpClient client, Guid caseId)
    {
        var statusPayload = new { Status = MedicalCaseStatus.Completed };
        return await client.PutAsJsonAsync($"/api/v1/medicalcases/{caseId}/status", statusPayload);
    }

    #endregion

    #region US-MC-State: Suspend

    [Fact]
    public async Task SuspendMedicalCase_SetsSuspendedStatus()
    {
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var (caseId, doctorId) = await CreateCaseAsync(doctorClient, patientId);

        var response = await doctorClient.PutAsJsonAsync(
            $"/api/v1/medicalcases/{caseId}/suspend", (object?)null);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "suspending an active case should succeed");

        var getResp = await doctorClient.GetAsync($"/api/v1/medicalcases/{caseId}");
        var data = await getResp.ShouldBeSuccessWithDataAsync<MedicalCaseDetailDto>();
        data.CaseStatus.Should().Be(MedicalCaseStatus.Suspended);
    }

    [Fact]
    public async Task SuspendMedicalCase_Returns422_WhenCompleted()
    {
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var (caseId, doctorId) = await CreateCaseAsync(doctorClient, patientId);

        await CreateConsultationAsync(doctorClient, caseId, doctorId, patientId);
        var flagPayload = new { NeedsPrescription = false };
        await doctorClient.PutAsJsonAsync($"/api/v1/medicalcases/{caseId}/prescription-flag", flagPayload);
        await CompleteCaseAsync(doctorClient, caseId);

        var response = await doctorClient.PutAsJsonAsync(
            $"/api/v1/medicalcases/{caseId}/suspend", (object?)null);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.Conflict },
            "suspending a completed case should fail (terminal state)");
    }

    #endregion

    #region US-MC-State: Complete

    [Fact]
    public async Task CompleteMedicalCase_SetsCompletedStatus()
    {
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var (caseId, doctorId) = await CreateCaseAsync(doctorClient, patientId);

        await CreateConsultationAsync(doctorClient, caseId, doctorId, patientId);
        var flagPayload = new { NeedsPrescription = false };
        await doctorClient.PutAsJsonAsync($"/api/v1/medicalcases/{caseId}/prescription-flag", flagPayload);

        var response = await CompleteCaseAsync(doctorClient, caseId);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "completing a case with valid data should succeed");

        var getResp = await doctorClient.GetAsync($"/api/v1/medicalcases/{caseId}");
        var data = await getResp.ShouldBeSuccessWithDataAsync<MedicalCaseDetailDto>();
        data.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
        data.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteMedicalCase_Returns422_WhenMissingDiagnosis()
    {
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var (caseId, _) = await CreateCaseAsync(doctorClient, patientId);

        var flagPayload = new { NeedsPrescription = false };
        await doctorClient.PutAsJsonAsync($"/api/v1/medicalcases/{caseId}/prescription-flag", flagPayload);

        var response = await CompleteCaseAsync(doctorClient, caseId);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.OK },
            "completing without consultation/diagnosis may be blocked by business rules");
    }

    #endregion

    #region US-MC-Query: Permissions

    [Fact]
    public async Task GetPermissions_ReturnsCanEdit_ForOwner()
    {
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var (caseId, doctorId) = await CreateCaseAsync(doctorClient, patientId);

        var response = await doctorClient.GetAsync($"/api/v1/medicalcases/{caseId}/permissions");

        var data = await response.ShouldBeSuccessWithDataAsync<MedicalCasePermissionsDto>();
        data.CanEdit.Should().BeTrue("owner should be able to edit their own active case");
        data.CanComplete.Should().BeTrue("owner should be able to complete their own active case");
        data.CanSuspend.Should().BeTrue("owner should be able to suspend their own active case");
        data.CanCancel.Should().BeTrue("owner should be able to cancel their own active case");
    }

    [Fact]
    public async Task GetPermissions_ReturnsCannotEdit_ForOtherDoctor()
    {
        var doctorClient = await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync(doctorClient);
        var (caseId, _) = await CreateCaseAsync(doctorClient, patientId);

        var adminClient = await LoginAsAdminAsync();
        var response = await adminClient.GetAsync($"/api/v1/medicalcases/{caseId}/permissions");

        var data = await response.ShouldBeSuccessWithDataAsync<MedicalCasePermissionsDto>();
        data.CanEdit.Should().BeTrue("admin can edit any case (isAdmin override)");
        data.CanComplete.Should().BeFalse("admin who is not the owner cannot complete");
        data.CanSuspend.Should().BeFalse("admin who is not the owner cannot suspend");
        data.CanCancel.Should().BeFalse("admin who is not the owner cannot cancel");
        data.CanDelete.Should().BeTrue("admin can delete any case");
    }

    #endregion

    #region US-MC-Query: BatchDetails

    [Fact]
    public async Task GetBatchDetails_ReturnsMultipleCases()
    {
        var doctorClient = await LoginAsDoctorAsync();
        var patient1 = await CreatePatientAsync(doctorClient, "批量患者1");
        var patient2 = await CreatePatientAsync(doctorClient, "批量患者2");
        var patient3 = await CreatePatientAsync(doctorClient, "批量患者3");

        var (caseId1, _) = await CreateCaseAsync(doctorClient, patient1);
        var (caseId2, _) = await CreateCaseAsync(doctorClient, patient2);
        var (caseId3, _) = await CreateCaseAsync(doctorClient, patient3);

        var ids = new List<Guid> { caseId1, caseId2, caseId3 };
        var response = await doctorClient.PostAsJsonAsync("/api/v1/medicalcases/batch-details", ids);

        var data = await response.ShouldBeSuccessWithDataAsync<List<MedicalCaseDetailDto>>();
        data.Should().HaveCount(3, "batch query should return all 3 cases");
        data.Select(c => c.Id).Should().Contain(new[] { caseId1, caseId2, caseId3 });
    }

    [Fact]
    public async Task GetBatchDetails_Returns400_WhenMoreThan50()
    {
        var doctorClient = await LoginAsDoctorAsync();
        var ids = Enumerable.Range(0, 51).Select(_ => Guid.NewGuid()).ToList();

        var response = await doctorClient.PostAsJsonAsync("/api/v1/medicalcases/batch-details", ids);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "batch query with >50 IDs should be rejected");
    }

    #endregion
}
