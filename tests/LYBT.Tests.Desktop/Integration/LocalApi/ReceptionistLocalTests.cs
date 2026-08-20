using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.LocalApi;

[Trait("Category", "LocalApi")]
[Collection("LocalApi")]
public class ReceptionistLocalTests : LocalApiTestBase
{
    [Fact]
    [Trait("US", "US-PAT-003")]
    public async Task Receptionist_CreatePatient_Succeeds()
    {
        var token = await GetReceptionistTokenAsync();
        SetAuthHeader(token);

        var input = new PatientInputDto
        {
            Name = UniqueName("E2E患者"),
            Gender = Gender.Female,
            PhoneNumber = UniquePhone()
        };

        var response = await Client.PostAsJsonAsync("/api/v1/patients", input);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-REG-002")]
    public async Task Receptionist_CreateRegistration_Succeeds()
    {
        // Need doctor id — fetch via admin token before switching to receptionist
        var adminToken = await GetAdminTokenAsync();
        SetAuthHeader(adminToken);
        var usersResp = await Client.GetAsync("/api/v1/users");
        usersResp.EnsureSuccessStatusCode();
        var usersJson = await usersResp.Content.ReadFromJsonAsync<JsonElement>(Json);
        // users endpoint returns paged wrapper: data.items or direct list — handle both
        Guid doctorId = Guid.Empty;
        string doctorName = "医生";
        if (usersJson.TryGetProperty("data", out var dataEl))
        {
            JsonElement items = dataEl;
            if (dataEl.TryGetProperty("items", out var itemsEl))
                items = itemsEl;
            if (items.ValueKind == JsonValueKind.Array)
            {
                foreach (var u in items.EnumerateArray())
                {
                    string? roleVal = null;
                    if (u.TryGetProperty("role", out var r)) roleVal = r.ValueKind == JsonValueKind.Number ? r.GetInt32().ToString() : r.GetString();
                    // Doctor role = 1 (enum)
                    if (roleVal == "1" || roleVal == "Doctor")
                    {
                        doctorId = u.GetProperty("id").GetGuid();
                        doctorName = u.TryGetProperty("realName", out var rn) ? rn.GetString() ?? doctorName : doctorName;
                        if (u.TryGetProperty("username", out var un) && un.GetString() == "doctor")
                        {
                            doctorId = u.GetProperty("id").GetGuid();
                            break;
                        }
                    }
                }

                if (doctorId == Guid.Empty)
                {
                    foreach (var u in items.EnumerateArray())
                    {
                        if (u.TryGetProperty("username", out var un) && un.GetString() == "doctor")
                        {
                            doctorId = u.GetProperty("id").GetGuid();
                            break;
                        }
                    }
                }
            }
        }

        // Fallback: if not found, use login to get doctor id via /api/v1/users/current
        if (doctorId == Guid.Empty)
        {
            var doctorToken = await GetDoctorTokenAsync();
            SetAuthHeader(doctorToken);
            var currentResp = await Client.GetAsync("/api/v1/users/current");
            if (currentResp.IsSuccessStatusCode)
            {
                var curJson = await currentResp.Content.ReadFromJsonAsync<JsonElement>(Json);
                if (curJson.TryGetProperty("data", out var curData) && curData.TryGetProperty("id", out var idEl))
                    doctorId = idEl.GetGuid();
            }
        }

        doctorId.Should().NotBe(Guid.Empty, "doctor user must exist");

        // Now switch to receptionist and create patient + registration
        var receptionistToken = await GetReceptionistTokenAsync();
        SetAuthHeader(receptionistToken);

        var patientInput = new PatientInputDto
        {
            Name = UniqueName("E2E患者"),
            Gender = Gender.Male,
            PhoneNumber = UniquePhone()
        };
        var patientResp = await Client.PostAsJsonAsync("/api/v1/patients", patientInput);
        patientResp.EnsureSuccessStatusCode();
        var patientJson = await patientResp.Content.ReadFromJsonAsync<JsonElement>(Json);
        var patientId = patientJson.GetProperty("data").GetProperty("id").GetGuid();
        var patientName = patientJson.GetProperty("data").GetProperty("name").GetString()!;

        var regInput = new RegistrationInputDto
        {
            PatientId = patientId,
            PatientName = patientName,
            DoctorId = doctorId,
            DoctorName = doctorName
        };

        var response = await Client.PostAsJsonAsync("/api/v1/registrations", regInput);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-REG-003")]
    public async Task Receptionist_GetRegistrations_ReturnsPaged()
    {
        var token = await GetReceptionistTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/v1/registrations?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-PAT-005")]
    public async Task Receptionist_GetPatientByIdNumber()
    {
        var token = await GetReceptionistTokenAsync();
        SetAuthHeader(token);

        var idNumber = UniqueIdNumber();
        var input = new PatientInputDto
        {
            Name = UniqueName("E2E患者"),
            Gender = Gender.Male,
            PhoneNumber = UniquePhone(),
            IdNumber = idNumber
        };
        var created = await Client.PostAsJsonAsync("/api/v1/patients", input);
        created.EnsureSuccessStatusCode();

        var response = await Client.GetAsync($"/api/v1/patients/by-id-number/{idNumber}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }
}
