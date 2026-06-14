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
/// Must Have User Stories for Patients module.
/// PRD: US-PAT-001 ~ US-PAT-004 (4 Must Have)
/// Collection: ClinicalData (isolated DB, parallel with other domains)
/// </summary>
[Collection("ClinicalData")]
public sealed class US_Patient_MustHaveTests : IntegrationTestBase<ClinicalDataFixture>
{
    public US_Patient_MustHaveTests(ClinicalDataFixture fixture) : base(fixture) { }

    #region US-PAT-001: Create patient

    [Fact]
    public async Task US_PAT_001_CreatePatient_WithValidData_ReturnsCreatedPatient()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var payload = PatientBuilder.Default()
            .WithName("张三")
            .WithGender(Gender.Male)
            .WithBirthDate(new DateTime(1985, 3, 15))
            .WithPhone($"138{Random.Shared.Next(10000000, 99999999)}")
            .Build();

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/patients", payload);

        // Assert
        var data = await response.ShouldBeCreatedWithDataAsync<PatientDetailDto>(
            "US-PAT-001: doctor should create patient successfully");
        data.Name.Should().Be("张三");
        data.Gender.Should().Be(Gender.Male);
        data.Id.Should().NotBeEmpty();
        data.Status.Should().Be(CommonStatus.Enabled, "new patient should be enabled");
        data.PinYinCode.Should().NotBeNullOrWhiteSpace("PinYin should be auto-generated");
    }

    [Fact]
    public async Task US_PAT_001_CreatePatient_WithAllRequiredFields_Succeeds()
    {
        // Arrange - Name, IdNumber, Address are required by validator
        var doctorClient = await LoginAsDoctorAsync();
        var payload = PatientBuilder.Default()
            .WithName("必填字段患者")
            .WithAddress("上海市浦东新区")
            .Build();

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/patients", payload);

        // Assert
        var data = await response.ShouldBeCreatedWithDataAsync<PatientDetailDto>(
            "US-PAT-001: patient with all required fields should succeed");
        data.Name.Should().Be("必填字段患者");
        data.Address.Should().Be("上海市浦东新区");
    }

    [Fact]
    public async Task US_PAT_001_CreatePatient_DuplicatePhone_ReturnsError()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var phone = $"138{Random.Shared.Next(10000000, 99999999)}";
        var payload1 = PatientBuilder.Default().WithPhone(phone).Build();
        var payload2 = PatientBuilder.Default().WithPhone(phone).Build();

        // Act
        var resp1 = await doctorClient.PostAsJsonAsync("/api/v1/patients", payload1);
        resp1.StatusCode.Should().Be(HttpStatusCode.Created);

        var resp2 = await doctorClient.PostAsJsonAsync("/api/v1/patients", payload2);

        // Assert
        resp2.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.Conflict, HttpStatusCode.UnprocessableEntity },
            "US-PAT-001: duplicate phone number should be rejected");
    }

    [Fact]
    public async Task US_PAT_001_CreatePatient_WithoutName_Returns400()
    {
        // Arrange - empty name should fail validation
        var doctorClient = await LoginAsDoctorAsync();
        var payload = new
        {
            Name = "",
            Gender = Gender.Male,
            IdNumber = "110101199001010001",
            Address = "测试地址"
        };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/patients", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "US-PAT-001: empty name should fail validation");
    }

    #endregion

    #region US-PAT-002: Update patient

    [Fact]
    public async Task US_PAT_002_UpdatePatient_ModifiesFields()
    {
        // Arrange - create a patient first
        var doctorClient = await LoginAsDoctorAsync();
        var createPayload = PatientBuilder.Default().WithName("待更新患者").Build();
        var createResp = await doctorClient.PostAsJsonAsync("/api/v1/patients", createPayload);
        var created = await createResp.ShouldBeCreatedWithDataAsync<PatientDetailDto>();

        // Act - update
        var updatePayload = PatientBuilder.Default()
            .WithName("已更新患者")
            .WithAddress("北京市朝阳区")
            .WithAllergyHistory("青霉素过敏")
            .WithPhone(created.PhoneNumber ?? $"139{Random.Shared.Next(10000000, 99999999)}")
            .Build();
        var response = await doctorClient.PutAsJsonAsync($"/api/v1/patients/{created.Id}", updatePayload);

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<PatientDetailDto>(
            "US-PAT-002: update should return modified patient");
        data.Name.Should().Be("已更新患者");
        data.Address.Should().Be("北京市朝阳区");
        data.AllergyHistory.Should().Be("青霉素过敏");
        data.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task US_PAT_002_UpdatePatient_NonexistentId_Returns404()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var fakeId = Guid.NewGuid();
        var payload = PatientBuilder.Default().Build();

        // Act
        var response = await doctorClient.PutAsJsonAsync($"/api/v1/patients/{fakeId}", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "US-PAT-002: update non-existent patient should return 404");
    }

    #endregion

    #region US-PAT-003: Search patients (name/phone/pinyin)

    [Fact]
    public async Task US_PAT_003_SearchByName_ReturnsMatchingPatients()
    {
        // Arrange - create patients with known names
        var doctorClient = await LoginAsDoctorAsync();
        var uniquePrefix = $"搜索_{Guid.NewGuid():N}"[..8];
        var payload1 = PatientBuilder.Default().WithName($"{uniquePrefix}甲").Build();
        var payload2 = PatientBuilder.Default().WithName($"{uniquePrefix}乙").Build();

        await doctorClient.PostAsJsonAsync("/api/v1/patients", payload1);
        await doctorClient.PostAsJsonAsync("/api/v1/patients", payload2);

        // Act
        var response = await doctorClient.GetAsync(
            $"/api/v1/patients?keyword={uniquePrefix}&page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<PatientListDto>(
            expectedMinCount: 2,
            because: "US-PAT-003: keyword search should match created patients");
    }

    [Fact]
    public async Task US_PAT_003_SearchByPinyin_ReturnsMatchingPatient()
    {
        // Arrange - create patient with a unique name whose pinyin is searchable
        var doctorClient = await LoginAsDoctorAsync();
        var uniqueName = $"拼音搜索_{Guid.NewGuid():N}"[..8];
        var payload = PatientBuilder.Default().WithName(uniqueName).Build();
        var createResp = await doctorClient.PostAsJsonAsync("/api/v1/patients", payload);
        var created = await createResp.ShouldBeCreatedWithDataAsync<PatientDetailDto>();

        // Act - search by the pinyin code (auto-generated from name)
        var pinyin = created.PinYinCode;
        pinyin.Should().NotBeNullOrWhiteSpace("pinyin should be auto-generated");

        var response = await doctorClient.GetAsync(
            $"/api/v1/patients?keyword={pinyin}&page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<PatientListDto>(
            expectedMinCount: 1,
            because: "US-PAT-003: pinyin search should find the patient");
    }

    [Fact]
    public async Task US_PAT_003_PaginationWorks_RespectsPageSize()
    {
        // Arrange - create multiple patients
        var doctorClient = await LoginAsDoctorAsync();
        for (var i = 0; i < 3; i++)
        {
            var payload = PatientBuilder.Default().Build();
            await doctorClient.PostAsJsonAsync("/api/v1/patients", payload);
        }

        // Act
        var response = await doctorClient.GetAsync("/api/v1/patients?page=1&pageSize=2");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<PatientListDto>(
            because: "US-PAT-003: pagination should work");
        paged.Items.Should().HaveCountLessThanOrEqualTo(2, "page size should be respected");
        paged.TotalCount.Should().BeGreaterOrEqualTo(3);
    }

    #endregion

    #region US-PAT-004: Delete patient (with reference check)

    [Fact]
    public async Task US_PAT_004_DeletePatient_WithoutReferences_Succeeds()
    {
        // Arrange - create a patient with no medical cases
        var doctorClient = await LoginAsDoctorAsync();
        var payload = PatientBuilder.Default().WithName("待删除无引用患者").Build();
        var createResp = await doctorClient.PostAsJsonAsync("/api/v1/patients", payload);
        var created = await createResp.ShouldBeCreatedWithDataAsync<PatientDetailDto>();

        // Act
        var response = await doctorClient.DeleteAsync($"/api/v1/patients/{created.Id}");

        // Assert
        await response.ShouldBeSuccessAsync(
            "US-PAT-004: delete unreferenced patient should succeed");

        // Verify - patient should be gone
        var getResp = await doctorClient.GetAsync($"/api/v1/patients/{created.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task US_PAT_004_DeletePatient_NonexistentId_Returns404()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var fakeId = Guid.NewGuid();

        // Act
        var response = await doctorClient.DeleteAsync($"/api/v1/patients/{fakeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "US-PAT-004: deleting non-existent patient should return 404");
    }

    [Fact]
    public async Task US_PAT_004_CheckReference_ReturnsReferenceStatus()
    {
        // Arrange - create a patient
        var doctorClient = await LoginAsDoctorAsync();
        var payload = PatientBuilder.Default().WithName("引用检查患者").Build();
        var createResp = await doctorClient.PostAsJsonAsync("/api/v1/patients", payload);
        var created = await createResp.ShouldBeCreatedWithDataAsync<PatientDetailDto>();

        // Act - check references (should have none)
        var response = await doctorClient.GetAsync($"/api/v1/patients/{created.Id}/check-reference");

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<PatientReferenceCheckDto>(
            "US-PAT-004: reference check should return status");
        data.PatientId.Should().Be(created.Id);
        data.HasReferences.Should().BeFalse("new patient should have no references");
        data.ReferenceCount.Should().Be(0);
    }

    #endregion
}
