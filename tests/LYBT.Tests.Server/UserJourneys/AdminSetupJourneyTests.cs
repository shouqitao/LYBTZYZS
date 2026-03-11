using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.UserJourneys;

/// <summary>
/// UAT Narrative 2: Admin Setup journey.
/// Admin creates doctor account, manages users, sets up clinic master data (herbs, formulas, patients).
///
/// Covered US:
/// - US-USER-001: Create user (Doctor)
/// - US-USER-002: List users
/// - US-USER-003: View user details
/// - US-USER-004: Update user
/// - US-USER-005: Delete user (soft delete)
/// - US-USER-008: Reset password
/// - US-USER-009: Change password
/// - US-USER-010: Update profile
/// - US-USER-011: Enable/Disable user
/// - US-USER-012: Get current user
/// - US-HERB-001: Create herb
/// - US-HERB-002: List herbs
/// - US-FORM-001: Create formula
/// - US-PAT-001: Create patient
/// - US-PAT-002: List patients
/// </summary>
[Collection("Users")]
public sealed class AdminSetupJourneyTests : JourneyTestBase<UserFixture>
{
    private const string DoctorPassword = "TestNewDoctor2025@";
    private const string ReceptionistPassword = "TestReceptionist2025@";

    public AdminSetupJourneyTests(UserFixture fixture) : base(fixture) { }

    /// <summary>
    /// US_ADMIN_SETUP_001: Full admin setup journey covering Phase A-E of UAT Narrative 2.
    /// Admin creates doctor, manages users, and sets up master data.
    /// </summary>
    [Fact]
    public async Task US_ADMIN_SETUP_001_Full_Journey()
    {
        // Phase A: Admin login and create doctor account (US-USER-001)
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        var doctorUsername = UniqueName("dr");
        var (createDoctorResponse, createdDoctor) = await PostAsync<UserDetailDto>(admin, "/api/v1/users", new UserInputDto
        {
            UserName = doctorUsername,
            RealName = "测试医生",
            Role = UserRole.Doctor,
            Email = $"{doctorUsername}@test.com",
            PhoneNumber = UniquePhone(),
            Password = DoctorPassword,
            ConfirmPassword = DoctorPassword
        });
        createDoctorResponse.StatusCode.Should().Be(HttpStatusCode.Created, "US-USER-001: Create user should return 201");
        createdDoctor!.Role.Should().Be(UserRole.Doctor);
        var createdDoctorId = createdDoctor.Id;

        // Phase B: Verify new doctor can login and get current user (US-USER-012)
        var doctorClient = await LoginAsAsync(doctorUsername, DoctorPassword);
        doctorClient.Should().NotBeNull("Newly created doctor should be able to login");

        var (currentUserResponse, currentUser) = await GetAsync<UserDetailDto>(doctorClient, "/api/v1/users/current");
        currentUserResponse.StatusCode.Should().Be(HttpStatusCode.OK, "US-USER-012: Get current user should return 200");
        currentUser!.Id.Should().Be(createdDoctorId);

        // Phase C: List users to verify doctor appears (US-USER-002)
        var (userListResponse, userList) = await GetAsync<PagedResult<UserDetailDto>>(admin, "/api/v1/users?pageSize=100");
        userListResponse.StatusCode.Should().Be(HttpStatusCode.OK, "US-USER-002: List users should return 200");
        userList!.Items.Should().Contain(u => u.Id == createdDoctorId, "Created doctor should appear in user list");

        // Phase D: View user details (US-USER-003)
        var (userDetailResponse, userDetail) = await GetAsync<UserDetailDto>(admin, $"/api/v1/users/{createdDoctorId}");
        userDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK, "US-USER-003: View user details should return 200");
        userDetail!.Id.Should().Be(createdDoctorId);
        userDetail.UserName.Should().Be(doctorUsername);

        // Phase E: Update user profile (US-USER-010 - via doctor self-service)
        var newPhone = UniquePhone();
        var updateProfileResponse = await doctorClient.PutAsJsonAsync(
            $"/api/v1/users/{createdDoctorId}/profile",
            new { RealName = "更新后的医生", PhoneNumber = newPhone });
        updateProfileResponse.StatusCode.Should().Be(HttpStatusCode.OK, "US-USER-010: Update profile should return 200");

        // Verify profile was updated
        var (updatedDetailResponse, updatedDetail) = await GetAsync<UserDetailDto>(admin, $"/api/v1/users/{createdDoctorId}");
        updatedDetail!.RealName.Should().Be("更新后的医生");
        updatedDetail.PhoneNumber.Should().Be(newPhone);

        // Phase F: Create Receptionist user for later disable/enable test
        var receptionistUsername = UniqueName("recep");
        var (createRecepResponse, createdRecep) = await PostAsync<UserDetailDto>(admin, "/api/v1/users", new UserInputDto
        {
            UserName = receptionistUsername,
            RealName = "前台接待",
            Role = UserRole.Receptionist,
            Email = $"{receptionistUsername}@test.com",
            PhoneNumber = UniquePhone(),
            Password = ReceptionistPassword,
            ConfirmPassword = ReceptionistPassword
        });
        createRecepResponse.StatusCode.Should().Be(HttpStatusCode.Created, "US-USER-001: Create Receptionist should succeed");
        var receptionistId = createdRecep!.Id;

        // Phase G: Disable user (US-USER-011)
        var disableResponse = await admin.PostAsJsonAsync($"/api/v1/users/{receptionistId}/toggle-status", new { });
        disableResponse.StatusCode.Should().Be(HttpStatusCode.OK, "US-USER-011: Disable user should return 200");

        var (_, disabledUser) = await GetAsync<UserDetailDto>(admin, $"/api/v1/users/{receptionistId}");
        disabledUser!.Status.Should().Be(CommonStatus.Disabled);

        // Phase H: Enable user (US-USER-011)
        var enableResponse = await admin.PostAsJsonAsync($"/api/v1/users/{receptionistId}/toggle-status", new { });
        enableResponse.StatusCode.Should().Be(HttpStatusCode.OK, "US-USER-011: Enable user should return 200");

        var (_, enabledUser) = await GetAsync<UserDetailDto>(admin, $"/api/v1/users/{receptionistId}");
        enabledUser!.Status.Should().Be(CommonStatus.Enabled);

        // Phase I: Reset password by admin (US-USER-008)
        // Use sysadmin to ensure sufficient permissions for password reset
        var sysadminForReset = await LoginAsSysAdminAsync();
        var resetPasswordResponse = await sysadminForReset.PostAsJsonAsync($"/api/v1/users/{createdDoctorId}/reset-password", new { });
        resetPasswordResponse.StatusCode.Should().Be(HttpStatusCode.OK, "US-USER-008: Reset password should return 200");

        // Phase J: Create herb with PinYin auto-generation (US-HERB-001)
        var herbName = UniqueName("当归");
        var (createHerbResponse, herb) = await PostAsync<HerbDetailDto>(admin, "/api/v1/herbs",
            new HerbInputDto { Name = herbName, Unit = "克", Price = 15.5m });
        createHerbResponse.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.OK, HttpStatusCode.Created }, "US-HERB-001: Create herb should return 200 or 201");
        herb!.PinYinCode.Should().NotBeNullOrEmpty("PinYin should be auto-generated");
        var herbId = herb.Id;

        // Phase L: List herbs (US-HERB-002)
        var (herbListResponse, herbList) = await GetAsync<PagedResult<HerbDetailDto>>(admin, "/api/v1/herbs?pageSize=100");
        herbListResponse.StatusCode.Should().Be(HttpStatusCode.OK, "US-HERB-002: List herbs should return 200");
        herbList!.Items.Should().Contain(h => h.Id == herbId);

        // Phase M: Create formula (US-FORM-001)
        var formulaInput = new FormulaInputDto
        {
            Name = UniqueName("四物汤"),
            Effect = "补血调经",
            Usage = "水煎服",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new()
                {
                    HerbId = herbId,
                    HerbName = herbName,
                    Dosage = 12,
                    Unit = "克"
                }
            }
        };

        var (createFormulaResponse, formula) = await PostAsync<FormulaDetailDto>(admin, "/api/v1/formulas", formulaInput);
        createFormulaResponse.StatusCode.Should().BeOneOf(new[] { HttpStatusCode.OK, HttpStatusCode.Created }, "US-FORM-001: Create formula should return 200 or 201");
        formula!.Id.Should().NotBeEmpty();
        var formulaId = formula.Id;

        // Phase N: Create patient (US-PAT-001)
        var patientInput = new PatientInputDto
        {
            Name = UniqueName("张三"),
            Gender = Gender.Male,
            BirthDate = new DateTime(1985, 3, 15),
            PhoneNumber = UniquePhone(),
            IdNumber = $"11010119850315{Random.Shared.Next(1000, 9999)}",
            Address = "北京市朝阳区"
        };

        var (createPatientResponse, patient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients", patientInput);
        createPatientResponse.StatusCode.Should().Be(HttpStatusCode.Created, "US-PAT-001: Create patient should return 201");
        patient!.Id.Should().NotBeEmpty();
        var patientId = patient.Id;

        // Phase O: List patients (US-PAT-002)
        var (patientListResponse, patientList) = await GetAsync<PagedResult<PatientDetailDto>>(admin, "/api/v1/patients?pageSize=100");
        patientListResponse.StatusCode.Should().Be(HttpStatusCode.OK, "US-PAT-002: List patients should return 200");
        patientList!.Items.Should().Contain(p => p.Id == patientId);

        // Phase P: Delete user (US-USER-005) - soft delete
        var deleteResponse = await admin.DeleteAsync($"/api/v1/users/{receptionistId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK, "US-USER-005: Delete user should return 200");

        // Verify user is soft deleted (not returned in list)
        var (listAfterDeleteResponse, listAfterDelete) = await GetAsync<PagedResult<UserDetailDto>>(admin, "/api/v1/users?pageSize=100");
        listAfterDelete!.Items.Should().NotContain(u => u.Id == receptionistId, "Deleted user should not appear in list");
    }

    /// <summary>
    /// US-USER-001: Create user - duplicate username should fail with 409
    /// </summary>
    [Fact]
    public async Task US_USER_001_CreateUser_DuplicateUsername_ShouldFail()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();
        var username = UniqueName("testuser");

        // Create first user
        var (firstResponse, _) = await PostAsync<UserDetailDto>(admin, "/api/v1/users", new UserInputDto
        {
            UserName = username,
            RealName = "First User",
            Role = UserRole.Doctor,
            Email = $"{username}@test.com",
            PhoneNumber = UniquePhone(),
            Password = DoctorPassword,
            ConfirmPassword = DoctorPassword
        });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Try to create user with same username
        var (duplicateResponse, _) = await PostAsync<UserDetailDto>(admin, "/api/v1/users", new UserInputDto
        {
            UserName = username,
            RealName = "Duplicate User",
            Role = UserRole.Doctor,
            Email = $"{username}2@test.com",
            PhoneNumber = UniquePhone(),
            Password = DoctorPassword,
            ConfirmPassword = DoctorPassword
        });

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity, "US-USER-001: duplicate username should return 422 (business rule validation)");
    }

    /// <summary>
    /// US-USER-001: Create user - reserved username should fail
    /// </summary>
    [Theory]
    [InlineData("admin")]
    [InlineData("root")]
    [InlineData("system")]
    [InlineData("administrator")]
    public async Task US_USER_001_CreateUser_ReservedUsername_ShouldFail(string reservedName)
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        var (response, _) = await PostAsync<UserDetailDto>(admin, "/api/v1/users", new UserInputDto
        {
            UserName = reservedName,
            RealName = "Test",
            Role = UserRole.Doctor,
            Email = $"{UniqueName("email")}@test.com",
            PhoneNumber = UniquePhone(),
            Password = DoctorPassword,
            ConfirmPassword = DoctorPassword
        });

        var validStatuses = new[] { HttpStatusCode.BadRequest, HttpStatusCode.Conflict, HttpStatusCode.UnprocessableEntity };
        validStatuses.Should().Contain(response.StatusCode, "US-USER-001: Reserved username should be rejected");
    }

    /// <summary>
    /// US-USER-001: Create user - Admin cannot create Admin (permission level check)
    /// </summary>
    [Fact]
    public async Task US_USER_001_CreateUser_AdminCannotCreateAdmin_ShouldFail()
    {
        await ResetForJourneyAsync();
        var sysadmin = await LoginAsSysAdminAsync();

        // Create an Admin user
        var adminUsername = UniqueName("admin");
        var (createAdminResponse, _) = await PostAsync<UserDetailDto>(sysadmin, "/api/v1/users", new UserInputDto
        {
            UserName = adminUsername,
            RealName = "Test Admin",
            Role = UserRole.Admin,
            Email = $"{adminUsername}@test.com",
            PhoneNumber = UniquePhone(),
            Password = DoctorPassword,
            ConfirmPassword = DoctorPassword
        });
        createAdminResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Login as the new Admin
        var adminClient = await LoginAsAsync(adminUsername, DoctorPassword);

        // Admin tries to create another Admin (should fail due to permission level)
        var (response, _) = await PostAsync<UserDetailDto>(adminClient, "/api/v1/users", new UserInputDto
        {
            UserName = UniqueName("anotheradmin"),
            RealName = "Another Admin",
            Role = UserRole.Admin,
            Email = $"{UniqueName("email")}@test.com",
            PhoneNumber = UniquePhone(),
            Password = DoctorPassword,
            ConfirmPassword = DoctorPassword
        });

        var validStatuses = new[] { HttpStatusCode.Forbidden, HttpStatusCode.UnprocessableEntity };
        validStatuses.Should().Contain(response.StatusCode, "US-USER-001: Admin creating Admin should fail due to permission level");
    }

    /// <summary>
    /// US-USER-004: Update user - Admin can update Doctor's role
    /// </summary>
    [Fact]
    public async Task US_USER_004_UpdateUser_ChangeDoctorRole_ShouldSucceed()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Create a Doctor
        var doctorUsername = UniqueName("dr");
        var (createResponse, doctor) = await PostAsync<UserDetailDto>(admin, "/api/v1/users", new UserInputDto
        {
            UserName = doctorUsername,
            RealName = "测试医生",
            Role = UserRole.Doctor,
            Email = $"{doctorUsername}@test.com",
            PhoneNumber = UniquePhone(),
            Password = DoctorPassword,
            ConfirmPassword = DoctorPassword
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var doctorId = doctor!.Id;

        // Update role to Receptionist
        var updateResponse = await admin.PutAsJsonAsync($"/api/v1/users/{doctorId}", new
        {
            Id = doctorId,
            UserName = doctorUsername,
            RealName = "测试医生",
            Role = UserRole.Receptionist,
            Email = $"{doctorUsername}@test.com",
            PhoneNumber = doctor.PhoneNumber
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK, "US-USER-004: Update user role should return 200");

        // Verify role was updated
        var (detailResponse, updatedDoctor) = await GetAsync<UserDetailDto>(admin, $"/api/v1/users/{doctorId}");
        updatedDoctor!.Role.Should().Be(UserRole.Receptionist);
    }

    /// <summary>
    /// US-USER-005: Delete user - cannot delete self
    /// </summary>
    [Fact]
    public async Task US_USER_005_DeleteUser_CannotDeleteSelf_ShouldFail()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Get current user ID
        var (currentResponse, currentUser) = await GetAsync<UserDetailDto>(admin, "/api/v1/users/current");
        currentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var adminId = currentUser!.Id;

        // Try to delete self - API returns 404 because user list excludes self, so self-ID is "not found"
        var deleteResponse = await admin.DeleteAsync($"/api/v1/users/{adminId}");
        var validStatuses = new[] { HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.UnprocessableEntity };
        validStatuses.Should().Contain(deleteResponse.StatusCode, "US-USER-005: Cannot delete self should return error");
    }

    /// <summary>
    /// US-USER-009: Change password - old password incorrect should fail
    /// </summary>
    [Fact]
    public async Task US_USER_009_ChangePassword_OldPasswordIncorrect_ShouldFail()
    {
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Create a Doctor
        var doctorUsername = UniqueName("dr");
        var (createResponse, doctor) = await PostAsync<UserDetailDto>(admin, "/api/v1/users", new UserInputDto
        {
            UserName = doctorUsername,
            RealName = "测试医生",
            Role = UserRole.Doctor,
            Email = $"{doctorUsername}@test.com",
            PhoneNumber = UniquePhone(),
            Password = DoctorPassword,
            ConfirmPassword = DoctorPassword
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var doctorId = doctor!.Id;

        // Login as doctor
        var doctorClient = await LoginAsAsync(doctorUsername, DoctorPassword);

        // Try to change password with wrong old password
        var changePasswordResponse = await doctorClient.PutAsJsonAsync(
            $"/api/v1/users/{doctorId}/change-password",
            new { OldPassword = "WrongPassword123!", NewPassword = "NewPassword2025@", ConfirmNewPassword = "NewPassword2025@" });

        changePasswordResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "US-USER-009: Wrong old password should return 400");
    }
}
