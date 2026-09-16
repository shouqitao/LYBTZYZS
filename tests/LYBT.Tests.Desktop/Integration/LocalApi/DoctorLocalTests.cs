using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.LocalApi;

[Trait("Category", "LocalApi")]
[Collection("LocalApi")]
public class DoctorLocalTests : LocalWebApiTestBase
{
    [Fact]
    [Trait("US", "US-MC-001")]
    public async Task Doctor_CreateMedicalCase_Succeeds()
    {
        var token = await GetDoctorTokenAsync();
        SetAuthHeader(token);

        // Doctor needs a patient first (admin creates, but doctor can also create patients)
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

        var input = new MedicalCaseInputDto
        {
            PatientId = patientId
        };

        var response = await Client.PostAsJsonAsync("/api/v1/medicalcases", input);
        // 2026-09-16 契约对齐：只读资源创建统一返回 201（此前 Local 为 200，与 Remote 的 CreatedAtAction 不一致）
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-MC-007")]
    public async Task Doctor_GetMedicalCases_ReturnsPaged()
    {
        var token = await GetDoctorTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/v1/medicalcases?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-MC-012")]
    public async Task Doctor_GetPendingCases_ReturnsData()
    {
        var token = await GetDoctorTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/v1/medicalcases/pending");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-PAT-002")]
    public async Task Doctor_SearchPatients_ByKeyword()
    {
        var token = await GetDoctorTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/v1/patients?page=1&pageSize=10&keyword=%E5%BC%A0");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-HERB-002")]
    public async Task Doctor_SearchHerbs_ByKeyword()
    {
        var token = await GetDoctorTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/v1/herbs?page=1&pageSize=10&keyword=%E5%8F%82");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-FORM-002")]
    public async Task Doctor_SearchFormulas_ByKeyword()
    {
        var token = await GetDoctorTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/v1/formulas?page=1&pageSize=10&keyword=%E6%B1%A4");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("US", "US-PAT-001")]
    public async Task Doctor_GetPatients_ReturnsPaged()
    {
        var token = await GetDoctorTokenAsync();
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/v1/patients?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
    }
}
