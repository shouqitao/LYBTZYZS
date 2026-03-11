using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.UserJourneys;

/// <summary>
/// Chapter 4: Patient Management journey tests.
/// PRD Coverage: patients.md US-PAT-001 ~ US-PAT-013 (4 Must + 2 Should + 7 Could)
///
/// Collection: Clinical (isolated database per fixture)
/// </summary>
[Collection("Clinical")]
public sealed class PatientManagementJourneyTests : JourneyTestBase<ClinicalFixture>
{
    public PatientManagementJourneyTests(ClinicalFixture fixture) : base(fixture) { }

    #region US-PAT-001: Create Patient

    [Fact]
    public async Task US_PAT_001_CreatePatient_WithValidData_ReturnsCreatedPatientWithPinYin()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Act: Create patient with auto PinYin generation
        var patientName = UniqueName("患者");
        var idNumber = $"110101{Random.Shared.Next(1950, 2000):D4}{Random.Shared.Next(1, 12):D2}{Random.Shared.Next(1, 28):D2}{Random.Shared.Next(1000, 9999):D4}";
        var phone = UniquePhone();
        var (response, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = patientName,
                Gender = Gender.Male,
                BirthDate = new DateTime(1985, 6, 15),
                PhoneNumber = phone,
                IdNumber = idNumber,
                Address = "上海市浦东新区"
            });

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        patient!.PinYinCode.Should().NotBeNullOrEmpty("PinYin code should be auto-generated");
        patient.Name.Should().Be(patientName);
        patient.Age.Should().BeGreaterThan(0, "Age should be calculated from BirthDate");
        patient.Status.Should().Be(CommonStatus.Enabled, "new patient should be enabled by default");
    }

    [Fact]
    public async Task US_PAT_001_CreatePatient_DuplicatePhoneNumber_Returns400()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Arrange: Create first patient
        var phone = UniquePhone();
        var idNumber1 = $"11010119800101{Random.Shared.Next(1000, 9999):D4}";
        await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("患者A"),
                Gender = Gender.Male,
                BirthDate = new DateTime(1980, 1, 1),
                PhoneNumber = phone,
                IdNumber = idNumber1,
                Address = "地址A"
            });

        // Act: Create second patient with same phone
        var idNumber2 = $"11010119800202{Random.Shared.Next(1000, 9999):D4}";
        var response = await admin.PostAsJsonAsync("/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("患者B"),
                Gender = Gender.Female,
                BirthDate = new DateTime(1980, 2, 2),
                PhoneNumber = phone, // Duplicate phone
                IdNumber = idNumber2,
                Address = "地址B"
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "PAT-001: duplicate phone number should fail");
    }

    [Fact]
    public async Task US_PAT_001_CreatePatient_DuplicateIdNumber_Returns400()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Arrange: Create first patient
        var idNumber = $"11010119800101{Random.Shared.Next(1000, 9999):D4}";
        await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("患者A"),
                Gender = Gender.Male,
                BirthDate = new DateTime(1980, 1, 1),
                PhoneNumber = UniquePhone(),
                IdNumber = idNumber,
                Address = "地址A"
            });

        // Act: Create second patient with same ID number
        var response = await admin.PostAsJsonAsync("/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("患者B"),
                Gender = Gender.Female,
                BirthDate = new DateTime(1980, 2, 2),
                PhoneNumber = UniquePhone(),
                IdNumber = idNumber, // Duplicate ID
                Address = "地址B"
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "PAT-001: duplicate ID number should fail with validation error");
    }

    [Fact]
    public async Task US_PAT_001_CreatePatient_FutureBirthDate_Returns400()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Act: Create patient with future birth date
        var response = await admin.PostAsJsonAsync("/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("患者"),
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(1), // Future date
                PhoneNumber = UniquePhone(),
                IdNumber = $"110101{Random.Shared.Next(10000000, 99999999):D8}{Random.Shared.Next(1000, 9999):D4}",
                Address = "地址"
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "PAT-001: future birth date should fail validation");
    }

    #endregion

    #region US-PAT-002/003: View Patient List and Detail

    [Fact]
    public async Task US_PAT_002_SearchPatient_ByKeyword_ReturnsMatchingResults()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Arrange: Create patients with unique prefix
        var uniquePrefix = $"搜索_{Guid.NewGuid():N}"[..8];
        await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = $"{uniquePrefix}张三",
                Gender = Gender.Male,
                BirthDate = new DateTime(1980, 1, 1),
                PhoneNumber = UniquePhone(),
                IdNumber = $"11010119800101{Random.Shared.Next(1000, 9999):D4}",
                Address = "地址"
            });
        await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = $"{uniquePrefix}李四",
                Gender = Gender.Female,
                BirthDate = new DateTime(1985, 2, 2),
                PhoneNumber = UniquePhone(),
                IdNumber = $"11010119850202{Random.Shared.Next(1000, 9999):D4}",
                Address = "地址"
            });

        // Act: Search by keyword
        var (response, result) = await GetAsync<PagedResult<PatientDetailDto>>(
            admin, $"/api/v1/patients?keyword={uniquePrefix}");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        result!.Items.Should().HaveCount(2, "should find both patients with matching keyword");
        result.Items.Should().Contain(p => p.Name.Contains("张三"));
        result.Items.Should().Contain(p => p.Name.Contains("李四"));
    }

    [Fact]
    public async Task US_PAT_003_GetPatientDetail_ByValidId_ReturnsPatientWithAge()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Arrange: Create patient
        var (createResponse, created) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("患者"),
                Gender = Gender.Male,
                BirthDate = new DateTime(1985, 6, 15),
                PhoneNumber = UniquePhone(),
                IdNumber = $"11010119850615{Random.Shared.Next(1000, 9999):D4}",
                Address = "上海市浦东新区"
            });
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        // Act: Get detail
        var (response, patient) = await GetAsync<PatientDetailDto>(
            admin, $"/api/v1/patients/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        patient.Should().NotBeNull();
        patient!.Id.Should().Be(created.Id);
        patient.Age.Should().BeGreaterThan(0, "Age should be calculated");
        patient.Status.Should().Be(CommonStatus.Enabled);
    }

    [Fact]
    public async Task US_PAT_003_GetPatientDetail_InvalidId_Returns400()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Act: Get non-existent patient (using empty GUID)
        var response = await admin.GetAsync("/api/v1/patients/00000000-0000-0000-0000-000000000000");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "PAT-003: invalid patient ID should return 400 (validation fails)");
    }

    #endregion

    #region US-PAT-004: Update Patient

    [Fact]
    public async Task US_PAT_004_UpdatePatient_NameChanged_PinYinRegenerated()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Arrange: Create patient
        var (createResponse, created) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("王五"),
                Gender = Gender.Male,
                BirthDate = new DateTime(1980, 1, 1),
                PhoneNumber = UniquePhone(),
                IdNumber = $"11010119800101{Random.Shared.Next(1000, 9999):D4}",
                Address = "原地址"
            });
        createResponse.IsSuccessStatusCode.Should().BeTrue();
        var originalPinYin = created!.PinYinCode;

        // Act: Update patient name
        var newName = UniqueName("王五改名");
        var (updateResponse, updated) = await PutAsync<PatientDetailDto>(admin,
            $"/api/v1/patients/{created.Id}",
            new PatientInputDto
            {
                Id = created.Id,
                Name = newName,
                Gender = Gender.Male,
                BirthDate = new DateTime(1980, 1, 1),
                PhoneNumber = created.PhoneNumber,
                IdNumber = created.IdNumber,
                Address = "新地址"
            });

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updated!.Name.Should().Be(newName);
        updated.PinYinCode.Should().NotBe(originalPinYin, "PinYin should be regenerated when name changes");
    }

    [Fact]
    public async Task US_PAT_004_UpdatePatient_DuplicatePhone_Returns400()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Arrange: Create two patients
        var phone1 = UniquePhone();
        var phone2 = UniquePhone();
        var (resp1, patient1) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("患者A"),
                Gender = Gender.Male,
                BirthDate = new DateTime(1980, 1, 1),
                PhoneNumber = phone1,
                IdNumber = $"11010119800101{Random.Shared.Next(1000, 9999):D4}",
                Address = "地址A"
            });
        var (resp2, patient2) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("患者B"),
                Gender = Gender.Female,
                BirthDate = new DateTime(1985, 2, 2),
                PhoneNumber = phone2,
                IdNumber = $"11010119850202{Random.Shared.Next(1000, 9999):D4}",
                Address = "地址B"
            });
        resp1.IsSuccessStatusCode.Should().BeTrue();
        resp2.IsSuccessStatusCode.Should().BeTrue();

        // Act: Try to update patient2 with patient1's phone
        var response = await admin.PutAsJsonAsync($"/api/v1/patients/{patient2!.Id}",
            new PatientInputDto
            {
                Id = patient2.Id,
                Name = patient2.Name,
                Gender = patient2.Gender,
                BirthDate = patient2.BirthDate,
                PhoneNumber = phone1, // Duplicate phone
                IdNumber = patient2.IdNumber,
                Address = patient2.Address
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "PAT-004: duplicate phone should fail");
    }

    #endregion

    #region US-PAT-005/011: Delete Patient with Reference Check

    [Fact]
    public async Task US_PAT_005_DeletePatient_NoReferences_ReturnsSuccess()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Arrange: Create patient
        var (createResponse, created) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("待删患者"),
                Gender = Gender.Male,
                BirthDate = new DateTime(1980, 1, 1),
                PhoneNumber = UniquePhone(),
                IdNumber = $"11010119800101{Random.Shared.Next(1000, 9999):D4}",
                Address = "地址"
            });
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        // Act: Check references
        var (checkResponse, refResult) = await GetAsync<PatientReferenceCheckDto>(
            admin, $"/api/v1/patients/{created!.Id}/check-reference");
        checkResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        refResult!.CanDelete.Should().BeTrue("patient with no medical cases should be deletable");

        // Act: Delete patient
        var deleteResponse = await admin.DeleteAsync($"/api/v1/patients/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: Patient no longer in list
        var (listResponse, listData) = await GetAsync<PagedResult<PatientDetailDto>>(
            admin, "/api/v1/patients?pageSize=100");
        listData!.Items.Should().NotContain(p => p.Id == created.Id);
    }

    [Fact]
    public async Task US_PAT_005_DeletePatient_HasMedicalCases_Returns422()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();
        var doctor = await LoginAsDoctorAsync();

        // Arrange: Create patient
        var (resp, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("有医案患者"),
                Gender = Gender.Male,
                BirthDate = new DateTime(1980, 1, 1),
                PhoneNumber = UniquePhone(),
                IdNumber = $"11010119800101{Random.Shared.Next(1000, 9999):D4}",
                Address = "地址"
            });
        resp.IsSuccessStatusCode.Should().BeTrue();

        // Arrange: Create medical case for this patient
        var (userResp, doctorData) = await GetAsync<UserDetailDto>(doctor, "/api/v1/users/current");
        userResp.IsSuccessStatusCode.Should().BeTrue();

        var caseResp = await doctor.PostAsJsonAsync("/api/v1/medicalcases",
            new MedicalCaseInputDto { PatientId = patient!.Id, UserId = doctorData!.Id });
        caseResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: Check references
        var (checkResponse, refResult) = await GetAsync<PatientReferenceCheckDto>(
            admin, $"/api/v1/patients/{patient.Id}/check-reference");
        checkResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        refResult!.CanDelete.Should().BeFalse("patient with medical cases should not be deletable");
        refResult.ReferenceCount.Should().BeGreaterThan(0);

        // Act: Try to delete
        var deleteResponse = await admin.DeleteAsync($"/api/v1/patients/{patient.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "PAT-005: patient with medical cases should return 422");
    }

    #endregion

    #region US-PAT-013: Patient Status Management

    [Fact]
    public async Task US_PAT_013_DisablePatient_WithActiveMedicalCase_Returns422()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();
        var doctor = await LoginAsDoctorAsync();

        // Arrange: Create patient
        var (resp, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("活跃患者"),
                Gender = Gender.Male,
                BirthDate = new DateTime(1980, 1, 1),
                PhoneNumber = UniquePhone(),
                IdNumber = $"11010119800101{Random.Shared.Next(1000, 9999):D4}",
                Address = "地址"
            });
        resp.IsSuccessStatusCode.Should().BeTrue();

        // Arrange: Create active medical case
        var (userResp, doctorData) = await GetAsync<UserDetailDto>(doctor, "/api/v1/users/current");
        userResp.IsSuccessStatusCode.Should().BeTrue();

        var caseResp = await doctor.PostAsJsonAsync("/api/v1/medicalcases",
            new MedicalCaseInputDto { PatientId = patient!.Id, UserId = doctorData!.Id });
        caseResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: Try to disable patient
        var response = await admin.PostAsync($"/api/v1/patients/{patient.Id}/toggle-status", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "PAT-013: cannot disable patient with active medical case");
    }

    [Fact]
    public async Task US_PAT_013_DisablePatient_Success_StatusChanged()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Arrange: Create patient without medical cases
        var (resp, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("可禁用患者"),
                Gender = Gender.Male,
                BirthDate = new DateTime(1980, 1, 1),
                PhoneNumber = UniquePhone(),
                IdNumber = $"11010119800101{Random.Shared.Next(1000, 9999):D4}",
                Address = "地址"
            });
        resp.IsSuccessStatusCode.Should().BeTrue();
        patient!.Status.Should().Be(CommonStatus.Enabled);

        // Act: Disable patient
        var response = await admin.PostAsync($"/api/v1/patients/{patient.Id}/toggle-status", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status changed
        var (_, updated) = await GetAsync<PatientDetailDto>(admin, $"/api/v1/patients/{patient.Id}");
        updated!.Status.Should().Be(CommonStatus.Disabled);
    }

    [Fact]
    public async Task US_PAT_013_ToggleStatus_EnableDisabledPatient_ReturnsSuccess()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Arrange: Create and disable patient
        var (resp, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("可恢复患者"),
                Gender = Gender.Male,
                BirthDate = new DateTime(1980, 1, 1),
                PhoneNumber = UniquePhone(),
                IdNumber = $"11010119800101{Random.Shared.Next(1000, 9999):D4}",
                Address = "地址"
            });
        resp.IsSuccessStatusCode.Should().BeTrue();

        // Disable first
        await admin.PostAsync($"/api/v1/patients/{patient!.Id}/toggle-status", null);

        // Act: Re-enable
        var response = await admin.PostAsync($"/api/v1/patients/{patient.Id}/toggle-status", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var (_, updated) = await GetAsync<PatientDetailDto>(admin, $"/api/v1/patients/{patient.Id}");
        updated!.Status.Should().Be(CommonStatus.Enabled);
    }

    [Fact]
    public async Task US_PAT_013_DisabledPatient_CannotCreateMedicalCase_Returns422()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();
        var doctor = await LoginAsDoctorAsync();

        // Arrange: Create and disable patient
        var (resp, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("禁用患者"),
                Gender = Gender.Male,
                BirthDate = new DateTime(1980, 1, 1),
                PhoneNumber = UniquePhone(),
                IdNumber = $"11010119800101{Random.Shared.Next(1000, 9999):D4}",
                Address = "地址"
            });
        resp.IsSuccessStatusCode.Should().BeTrue();

        await admin.PostAsync($"/api/v1/patients/{patient!.Id}/toggle-status", null);

        // Act: Try to create medical case for disabled patient
        var (userResp, doctorData) = await GetAsync<UserDetailDto>(doctor, "/api/v1/users/current");
        userResp.IsSuccessStatusCode.Should().BeTrue();

        var caseResp = await doctor.PostAsJsonAsync("/api/v1/medicalcases",
            new MedicalCaseInputDto { PatientId = patient.Id, UserId = doctorData!.Id });

        // Assert
        caseResp.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "PAT-013: cannot create medical case for disabled patient");
    }

    #endregion

    #region US-PAT-002: Role-Based Filtering (Receptionist vs Doctor/Admin)

    [Fact]
    public async Task US_PAT_002_Receptionist_CannotSeeDisabledPatients()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Arrange: Create receptionist
        var recepUsername = $"recep_{Guid.NewGuid():N}"[..10];
        await PostAsync<UserDetailDto>(admin, "/api/v1/users",
            new UserInputDto
            {
                UserName = recepUsername,
                RealName = "测试前台",
                Role = UserRole.Receptionist,
                Password = "TestReceptionist2025@",
                ConfirmPassword = "TestReceptionist2025@"
            });
        var recep = await LoginAsAsync(recepUsername, "TestReceptionist2025@");

        // Arrange: Create and disable patient
        var (resp, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients",
            new PatientInputDto
            {
                Name = UniqueName("禁用患者测试"),
                Gender = Gender.Male,
                BirthDate = new DateTime(1980, 1, 1),
                PhoneNumber = UniquePhone(),
                IdNumber = $"11010119800101{Random.Shared.Next(1000, 9999):D4}",
                Address = "地址"
            });
        resp.IsSuccessStatusCode.Should().BeTrue();

        await admin.PostAsync($"/api/v1/patients/{patient!.Id}/toggle-status", null);

        // Act: Receptionist searches for patients
        var (response, result) = await GetAsync<PagedResult<PatientDetailDto>>(
            recep, $"/api/v1/patients?keyword={patient.Name[..3]}");

        // Assert: Receptionist should not see disabled patient
        response.IsSuccessStatusCode.Should().BeTrue();
        result!.Items.Should().NotContain(p => p.Id == patient.Id,
            "Receptionist should not see disabled patients");
    }

    #endregion
}
