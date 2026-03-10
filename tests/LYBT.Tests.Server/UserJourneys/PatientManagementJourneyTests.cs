using System.Net;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.UserJourneys;

/// <summary>
/// Patient management journey: create, update, toggle status, check references, delete.
/// </summary>
[Collection("Clinical")]
public sealed class PatientManagementJourneyTests : JourneyTestBase<ClinicalFixture>
{
    public PatientManagementJourneyTests(ClinicalFixture fixture) : base(fixture) { }

    [Fact]
    public async Task PatientManagement_Full_Journey()
    {
        // Step 1: Setup
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Step 2: Create patient
        var patientInput = new PatientInputDto
        {
            Name = UniqueName("王五"),
            Gender = Gender.Male,
            BirthDate = new DateTime(1975, 8, 10),
            PhoneNumber = UniquePhone(),
            IdNumber = $"11010119750810{Random.Shared.Next(1000, 9999)}",
            Address = "上海市浦东新区"
        };

        var (createResponse, createdPatient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients", patientInput);
        createResponse.IsSuccessStatusCode.Should().BeTrue($"创建患者应成功, 实际: {createResponse.StatusCode}");
        var patientId = createdPatient!.Id;

        // Step 3: Update patient
        var updateInput = new PatientInputDto
        {
            Id = patientId,
            Name = UniqueName("王五改"),
            Gender = Gender.Male,
            BirthDate = new DateTime(1975, 8, 10),
            PhoneNumber = UniquePhone(),
            IdNumber = patientInput.IdNumber,
            Address = "上海市浦东新区新地址"
        };

        var (updateResponse, _) = await PutAsync<PatientDetailDto>(admin, $"/api/v1/patients/{patientId}", updateInput);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 4: Toggle status
        var toggleResponse = await admin.PostAsync($"/api/v1/patients/{patientId}/toggle-status", null);
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 5: Check no references
        var checkRefResponse = await admin.GetAsync($"/api/v1/patients/{patientId}/check-reference");
        checkRefResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 6: Delete patient
        var deleteResponse = await admin.DeleteAsync($"/api/v1/patients/{patientId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var (listResponse, listData) = await GetAsync<PagedResult<PatientDetailDto>>(
            admin, "/api/v1/patients?pageSize=100");
        listData!.Items.Should().NotContain(p => p.Id == patientId);
    }
}
