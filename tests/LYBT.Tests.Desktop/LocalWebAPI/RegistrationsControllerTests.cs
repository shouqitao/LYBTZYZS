using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Entities.Registrations;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// Integration tests for RegistrationsController (GET/POST/PUT/DELETE /api/registrations/*).
/// All endpoints require [Authorize].
/// </summary>
public class RegistrationsControllerTests : LocalWebApiControllerTestBase
{
    private async Task AuthenticateAsync()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);
    }

    private async Task<JsonElement> CreateTestRegistrationAsync()
    {
        // Get admin user ID to use as doctor
        var usersResponse = await Client.GetAsync("/api/users");
        usersResponse.EnsureSuccessStatusCode();
        var users = await usersResponse.Content.ReadFromJsonAsync<List<JsonElement>>(Json);
        var adminId = users!.First(u => u.GetProperty("username").GetString() == "admin").GetProperty("id").GetGuid();

        // Create a patient
        var patient = new
        {
            Name = $"RegPatient_{Guid.NewGuid():N}",
            Gender = 1,
            Status = 1
        };
        var patientResponse = await Client.PostAsJsonAsync("/api/patients", patient);
        patientResponse.EnsureSuccessStatusCode();
        var patientJson = await patientResponse.Content.ReadFromJsonAsync<JsonElement>(Json);
        var patientId = patientJson.GetProperty("id").GetGuid();

        // Create registration
        var registration = new Registration
        {
            PatientId = patientId,
            PatientName = patient.Name,
            DoctorId = adminId,
            DoctorName = "Admin",
            Source = RegistrationSource.Receptionist,
            Status = RegistrationStatus.Waiting,
            CreatedBy = adminId
        };

        var response = await Client.PostAsJsonAsync("/api/registrations", registration);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    [Fact]
    public async Task GetRegistrations_Returns_Ok()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/registrations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var registrations = await response.Content.ReadFromJsonAsync<List<JsonElement>>(Json);
        registrations.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateRegistration_Works()
    {
        await AuthenticateAsync();

        var created = await CreateTestRegistrationAsync();

        created.GetProperty("id").GetGuid().Should().NotBe(Guid.Empty);
        created.GetProperty("status").GetInt32().Should().Be((int)RegistrationStatus.Waiting);
    }

    [Fact]
    public async Task GetQueue_Returns_Ok()
    {
        await AuthenticateAsync();

        // Create a waiting registration to ensure queue is non-empty
        await CreateTestRegistrationAsync();

        var response = await Client.GetAsync("/api/registrations/queue");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var queue = await response.Content.ReadFromJsonAsync<List<JsonElement>>(Json);
        queue.Should().NotBeNull();
        queue!.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task StartVisit_Returns_NotFound_For_Invalid_Id()
    {
        await AuthenticateAsync();

        var response = await Client.PutAsJsonAsync($"/api/registrations/{Guid.NewGuid()}/start-visit", (object?)null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_Returns_NotFound_For_Invalid_Id()
    {
        await AuthenticateAsync();

        var response = await Client.PutAsJsonAsync($"/api/registrations/{Guid.NewGuid()}/cancel", (object?)null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRegistration_Soft_Deletes()
    {
        await AuthenticateAsync();

        var created = await CreateTestRegistrationAsync();
        var regId = created.GetProperty("id").GetGuid();

        // Delete
        var deleteResponse = await Client.DeleteAsync($"/api/registrations/{regId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // GET should return NotFound after soft delete
        var getResponse = await Client.GetAsync($"/api/registrations/{regId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
