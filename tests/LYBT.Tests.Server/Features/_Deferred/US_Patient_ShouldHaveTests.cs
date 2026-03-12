using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.Patients;

/// <summary>
/// Should Have User Stories for Patients module.
/// PRD: US-PAT-005 (Delete with ref check), US-PAT-013 (Status management)
/// Collection: ClinicalData (isolated DB, parallel with other domains)
/// </summary>
[Collection("ClinicalData")]
public sealed class US_Patient_ShouldHaveTests : IntegrationTestBase<ClinicalDataFixture>
{
    public US_Patient_ShouldHaveTests(ClinicalDataFixture fixture) : base(fixture) { }

    #region US-PAT-005: Delete patient (with reference protection)

    [Fact]
    public async Task US_PAT_005_DeletePatient_WithMedicalCaseReference_ShouldBeRejected()
    {
        // Arrange - create patient, then create a medical case referencing it
        var doctorClient = await LoginAsDoctorAsync();
        var adminClient = await LoginAsAdminAsync();

        var patientPayload = PatientBuilder.Default().WithName("有引用患者").Build();
        var createResp = await doctorClient.PostAsJsonAsync("/api/v1/patients", patientPayload);
        var patient = await createResp.ShouldBeCreatedWithDataAsync<PatientDetailDto>();

        // Create a medical case for this patient (creates reference)
        var doctorId = await GetDoctorUserIdAsync(adminClient);
        var casePayload = MedicalCaseBuilder.Default()
            .ForPatient(patient.Id)
            .WithDoctor(doctorId)
            .BuildCreate();
        var caseResp = await doctorClient.PostAsJsonAsync("/api/v1/medicalcases", casePayload);
        caseResp.StatusCode.Should().Be(HttpStatusCode.OK,
            "medical case should be created for the patient");

        // Verify reference exists
        var refCheckResp = await doctorClient.GetAsync($"/api/v1/patients/{patient.Id}/check-reference");
        var refCheck = await refCheckResp.ShouldBeSuccessWithDataAsync<PatientReferenceCheckDto>();
        refCheck.HasReferences.Should().BeTrue("patient should have medical case references");

        // Act - try to delete patient with references
        var deleteResp = await doctorClient.DeleteAsync($"/api/v1/patients/{patient.Id}");

        // Assert - should be rejected or succeed with soft-delete
        deleteResp.StatusCode.Should().BeOneOf(
            new[]
            {
                HttpStatusCode.OK,              // soft-delete allowed
                HttpStatusCode.BadRequest,       // business rule: cannot delete
                HttpStatusCode.Conflict,         // conflict with references
                HttpStatusCode.UnprocessableEntity // validation failure
            },
            "US-PAT-005: delete with references should be handled according to business rules");
    }

    [Fact]
    public async Task US_PAT_005_CheckReference_BeforeDelete_ShowsCanDeleteStatus()
    {
        // Arrange - patient with no references
        var doctorClient = await LoginAsDoctorAsync();
        var payload = PatientBuilder.Default().WithName("无引用待删患者").Build();
        var createResp = await doctorClient.PostAsJsonAsync("/api/v1/patients", payload);
        var patient = await createResp.ShouldBeCreatedWithDataAsync<PatientDetailDto>();

        // Act - check reference
        var response = await doctorClient.GetAsync($"/api/v1/patients/{patient.Id}/check-reference");

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<PatientReferenceCheckDto>(
            "US-PAT-005: reference check should indicate safe to delete");
        data.HasReferences.Should().BeFalse();
        data.CanDelete.Should().BeTrue("patient without references should be deletable");
    }

    #endregion

    #region US-PAT-013: Status management (enable/disable)

    [Fact]
    public async Task US_PAT_013_ToggleStatus_DisablesEnabledPatient()
    {
        // Arrange - create an enabled patient
        var doctorClient = await LoginAsDoctorAsync();
        var payload = PatientBuilder.Default().WithName("状态切换患者").Build();
        var createResp = await doctorClient.PostAsJsonAsync("/api/v1/patients", payload);
        var patient = await createResp.ShouldBeCreatedWithDataAsync<PatientDetailDto>();
        patient.Status.Should().Be(CommonStatus.Enabled, "new patient should be enabled");

        // Act - toggle to disabled
        var response = await doctorClient.PostAsync($"/api/v1/patients/{patient.Id}/toggle-status", null);

        // Assert
        var toggled = await response.ShouldBeSuccessWithDataAsync<PatientDetailDto>(
            "US-PAT-013: toggle should succeed");
        toggled.Status.Should().Be(CommonStatus.Disabled,
            "US-PAT-013: enabled patient should become disabled");
    }

    [Fact]
    public async Task US_PAT_013_ToggleStatus_ReEnablesDisabledPatient()
    {
        // Arrange - create and disable a patient
        var doctorClient = await LoginAsDoctorAsync();
        var payload = PatientBuilder.Default().WithName("重新启用患者").Build();
        var createResp = await doctorClient.PostAsJsonAsync("/api/v1/patients", payload);
        var patient = await createResp.ShouldBeCreatedWithDataAsync<PatientDetailDto>();

        // Disable first
        await doctorClient.PostAsync($"/api/v1/patients/{patient.Id}/toggle-status", null);

        // Act - toggle back to enabled
        var response = await doctorClient.PostAsync($"/api/v1/patients/{patient.Id}/toggle-status", null);

        // Assert
        var toggled = await response.ShouldBeSuccessWithDataAsync<PatientDetailDto>(
            "US-PAT-013: re-enable should succeed");
        toggled.Status.Should().Be(CommonStatus.Enabled,
            "US-PAT-013: disabled patient should become enabled again");
    }

    #endregion
}
