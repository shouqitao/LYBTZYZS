using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.LocalApi;

[Trait("Category", "LocalApi")]
[Collection("LocalApi")]
public class AdminLocalTests : LocalApiTestBase
{
    [Fact]
    [Trait("US", "US-PAT-001")]
    public async Task Admin_GetPatients_ReturnsPaged()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/v1/patients?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-PAT-003")]
    public async Task Admin_CreatePatient_Succeeds()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var input = new PatientInputDto
        {
            Name = UniqueName("E2E患者"),
            Gender = Gender.Male,
            PhoneNumber = UniquePhone(),
            IdNumber = UniqueIdNumber()
        };

        var response = await Client.PostAsJsonAsync("/api/v1/patients", input);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-PAT-004")]
    public async Task Admin_UpdatePatient_Succeeds()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var input = new PatientInputDto
        {
            Name = UniqueName("E2E患者"),
            Gender = Gender.Female,
            PhoneNumber = UniquePhone()
        };
        var createResponse = await Client.PostAsJsonAsync("/api/v1/patients", input);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(Json);
        var id = created.GetProperty("data").GetProperty("id").GetGuid();

        var updated = new PatientInputDto
        {
            Id = id,
            Name = UniqueName("E2E更新"),
            Gender = Gender.Female,
            PhoneNumber = UniquePhone()
        };
        var response = await Client.PutAsJsonAsync($"/api/v1/patients/{id}", updated);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-HERB-001")]
    public async Task Admin_GetHerbs_ReturnsPaged()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/v1/herbs?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-HERB-003")]
    public async Task Admin_CreateHerb_Succeeds()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var input = new HerbInputDto
        {
            Name = UniqueName("E2E药材"),
            PinYinCode = "EYYC",
            Category = "补气药",
            Unit = "g",
            Price = 10m
        };

        var response = await Client.PostAsJsonAsync("/api/v1/herbs", input);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-FORM-001")]
    public async Task Admin_GetFormulas_ReturnsPaged()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/v1/formulas?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-REG-001")]
    public async Task Admin_GetRegistrations_ReturnsPaged()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/v1/registrations?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }
}
