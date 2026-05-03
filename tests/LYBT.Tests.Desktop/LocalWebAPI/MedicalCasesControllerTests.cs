using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// Integration tests for MedicalCasesController (GET/POST/PUT/DELETE /api/medicalcases/*).
/// All endpoints require [Authorize].
/// </summary>
public class MedicalCasesControllerTests : LocalWebApiControllerTestBase
{
    private async Task AuthenticateAsync()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);
    }

    /// <summary>
    /// Helper: create a patient first, then create a medical case for that patient.
    /// </summary>
    private async Task<(Guid PatientId, Guid UserId, JsonElement MedicalCase)> CreateTestMedicalCaseAsync()
    {
        // Get the admin user ID
        var usersResponse = await Client.GetAsync("/api/users");
        usersResponse.EnsureSuccessStatusCode();
        var users = await usersResponse.Content.ReadFromJsonAsync<List<JsonElement>>(Json);
        var adminId = users!.First(u => u.GetProperty("username").GetString() == "admin").GetProperty("id").GetGuid();

        // Create a patient
        var patient = new
        {
            Name = $"MCPatient_{Guid.NewGuid():N}",
            Gender = 1, // Male
            Status = 1  // Enabled
        };
        var patientResponse = await Client.PostAsJsonAsync("/api/patients", patient);
        patientResponse.EnsureSuccessStatusCode();
        var patientJson = await patientResponse.Content.ReadFromJsonAsync<JsonElement>(Json);
        var patientId = patientJson.GetProperty("id").GetGuid();

        // Create a medical case
        var mc = new MedicalCase
        {
            PatientId = patientId,
            PatientName = patient.Name,
            UserId = adminId,
            DoctorName = "Admin",
            CaseStatus = MedicalCaseStatus.Active,
            CreatedBy = adminId
        };
        var mcResponse = await Client.PostAsJsonAsync("/api/medicalcases", mc);
        mcResponse.EnsureSuccessStatusCode();
        var mcJson = await mcResponse.Content.ReadFromJsonAsync<JsonElement>(Json);

        return (patientId, adminId, mcJson);
    }

    [Fact]
    public async Task GetMedicalCases_Returns_Ok()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/medicalcases");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cases = await response.Content.ReadFromJsonAsync<List<JsonElement>>(Json);
        cases.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateMedicalCase_Works()
    {
        await AuthenticateAsync();

        var (_, _, mc) = await CreateTestMedicalCaseAsync();

        mc.GetProperty("id").GetGuid().Should().NotBe(Guid.Empty);
        mc.GetProperty("caseStatus").GetInt32().Should().Be((int)MedicalCaseStatus.Active);
    }

    [Fact]
    public async Task GetMedicalCase_Returns_NotFound_For_Invalid_Id()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync($"/api/medicalcases/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_Returns_Empty_When_No_Match()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/medicalcases/search?patientName=NonExistentPatientName12345XYZ");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        result.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetByStatus_Returns_Filtered()
    {
        await AuthenticateAsync();

        // Create a case (status = Active)
        await CreateTestMedicalCaseAsync();

        var response = await Client.GetAsync("/api/medicalcases/by-status/Active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cases = await response.Content.ReadFromJsonAsync<List<JsonElement>>(Json);
        cases.Should().NotBeNull();
        cases!.Should().HaveCountGreaterThanOrEqualTo(1);
        // All returned cases should be Active
        foreach (var c in cases!)
        {
            c.GetProperty("caseStatus").GetInt32().Should().Be((int)MedicalCaseStatus.Active);
        }
    }

    [Fact]
    public async Task GetPendingCases_Returns_Ok()
    {
        await AuthenticateAsync();

        // Create an active case to ensure there's at least one pending
        await CreateTestMedicalCaseAsync();

        var response = await Client.GetAsync("/api/medicalcases/pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cases = await response.Content.ReadFromJsonAsync<List<JsonElement>>(Json);
        cases.Should().NotBeNull();
    }
}
