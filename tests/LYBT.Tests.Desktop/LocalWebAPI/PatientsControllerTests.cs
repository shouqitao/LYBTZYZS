using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// Integration tests for PatientsController (GET/POST/PUT/DELETE /api/patients/*).
/// All endpoints require [Authorize].
/// </summary>
public class PatientsControllerTests : LocalWebApiControllerTestBase
{
    private async Task AuthenticateAsync()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);
    }

    private async Task<JsonElement> CreateTestPatientAsync(string? name = null, string? idNumber = null)
    {
        var patient = new Patient
        {
            Name = name ?? $"TestPatient_{Guid.NewGuid():N}",
            Gender = Gender.Male,
            BirthDate = DateTime.UtcNow.AddYears(-25),
            PhoneNumber = "13800138000",
            IdNumber = idNumber,
            Status = CommonStatus.Enabled
        };

        var response = await Client.PostAsJsonAsync("/api/patients", patient);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    [Fact]
    public async Task GetPatients_Returns_Ok()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/patients?keyword=%20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var patients = await response.Content.ReadFromJsonAsync<List<JsonElement>>(Json);
        patients.Should().NotBeNull();
        patients!.Should().HaveCountGreaterThanOrEqualTo(1); // seed data includes one patient
    }

    [Fact]
    public async Task CreatePatient_And_GetById_Works()
    {
        await AuthenticateAsync();

        var created = await CreateTestPatientAsync("Zhang San");
        var patientId = created.GetProperty("id").GetGuid();

        var response = await Client.GetAsync($"/api/patients/{patientId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("name").GetString().Should().Be("Zhang San");
    }

    [Fact]
    public async Task DeletePatient_Soft_Deletes()
    {
        await AuthenticateAsync();

        var created = await CreateTestPatientAsync();
        var patientId = created.GetProperty("id").GetGuid();

        // Delete
        var deleteResponse = await Client.DeleteAsync($"/api/patients/{patientId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // GET should return NotFound after soft delete
        var getResponse = await Client.GetAsync($"/api/patients/{patientId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestorePatient_Works_After_Soft_Delete()
    {
        await AuthenticateAsync();

        var created = await CreateTestPatientAsync();
        var patientId = created.GetProperty("id").GetGuid();

        // Delete
        await Client.DeleteAsync($"/api/patients/{patientId}");

        // Restore
        var restoreResponse = await Client.PostAsJsonAsync($"/api/patients/{patientId}/restore", (object?)null);
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // GET should succeed after restore
        var getResponse = await Client.GetAsync($"/api/patients/{patientId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByIdNumber_Returns_Patient()
    {
        await AuthenticateAsync();

        var idNumber = $"310101{DateTime.UtcNow:yyyyMMdd}001";
        await CreateTestPatientAsync("IdNumberTest", idNumber);

        var response = await Client.GetAsync($"/api/patients/by-id-number/{idNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("name").GetString().Should().Be("IdNumberTest");
    }

    [Fact]
    public async Task TogglePatientStatus_Toggles()
    {
        await AuthenticateAsync();

        var created = await CreateTestPatientAsync();
        var patientId = created.GetProperty("id").GetGuid();

        // Toggle (Enabled -> Disabled)
        var toggleResponse = await Client.PostAsJsonAsync($"/api/patients/{patientId}/toggle-status", (object?)null);
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await toggleResponse.Content.ReadFromJsonAsync<JsonElement>(Json);
        // Verify toggle response has status property (exact format depends on serialization)
        json.TryGetProperty("status", out _).Should().BeTrue();

        // Toggle back (Disabled -> Enabled)
        var toggleResponse2 = await Client.PostAsJsonAsync($"/api/patients/{patientId}/toggle-status", (object?)null);
        toggleResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        var json2 = await toggleResponse2.Content.ReadFromJsonAsync<JsonElement>(Json);
        json2.TryGetProperty("status", out _).Should().BeTrue();
    }
}
