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
