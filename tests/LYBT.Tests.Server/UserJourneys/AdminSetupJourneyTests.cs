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
/// Admin setup journey: create doctor, herbs, formulas, patients, verify all queryable.
/// </summary>
[Collection("Users")]
public sealed class AdminSetupJourneyTests : JourneyTestBase<UserFixture>
{
    private const string DoctorPassword = "TestNewDoctor2025@";

    public AdminSetupJourneyTests(UserFixture fixture) : base(fixture) { }

    [Fact]
    public async Task AdminSetup_Full_Journey()
    {
        // Step 1: Reset and admin login
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Step 2: Create doctor account
        var doctorUsername = $"dr_{Guid.NewGuid():N}"[..12];
        var userInput = new UserInputDto
        {
            UserName = doctorUsername,
            RealName = "测试医生",
            Role = UserRole.Doctor,
            Email = $"{doctorUsername}@test.com",
            PhoneNumber = UniquePhone(),
            Password = DoctorPassword,
            ConfirmPassword = DoctorPassword
        };

        var (createUserResponse, createdUser) = await PostAsync<UserDetailDto>(admin, "/api/v1/users", userInput);
        createUserResponse.IsSuccessStatusCode.Should().BeTrue($"创建用户应成功, 实际: {createUserResponse.StatusCode}");
        createdUser.Should().NotBeNull();
        createdUser!.Id.Should().NotBeEmpty();
        var createdDoctorId = createdUser.Id;

        // Step 3: New doctor can login
        var doctorClient = await LoginAsAsync(doctorUsername, DoctorPassword);
        doctorClient.Should().NotBeNull("newly created doctor should be able to login");

        // Step 4: Create herb
        var herbInput = new HerbInputDto
        {
            Name = UniqueName("当归"),
            Unit = "克",
            Price = 15.5m
        };

        var (createHerbResponse, createdHerb) = await PostAsync<HerbDetailDto>(admin, "/api/v1/herbs", herbInput);
        createHerbResponse.IsSuccessStatusCode.Should().BeTrue($"创建药材应成功, 实际: {createHerbResponse.StatusCode}");
        createdHerb!.Id.Should().NotBeEmpty();
        var createdHerbId = createdHerb.Id;

        // Step 5: Herb is queryable
        var (herbListResponse, herbList) = await GetAsync<PagedResult<HerbDetailDto>>(
            admin, "/api/v1/herbs?pageSize=100");
        herbListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        herbList!.Items.Should().Contain(h => h.Id == createdHerbId);

        // Step 6: Create formula with herb
        var formulaInput = new FormulaInputDto
        {
            Name = UniqueName("四物汤"),
            Effect = "补血调经",
            Usage = "水煎服",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new()
                {
                    HerbId = createdHerbId,
                    HerbName = "当归",
                    Dosage = 12,
                    Unit = "克"
                }
            }
        };

        var (createFormulaResponse, createdFormula) = await PostAsync<FormulaDetailDto>(admin, "/api/v1/formulas", formulaInput);
        createFormulaResponse.IsSuccessStatusCode.Should().BeTrue($"创建验方应成功, 实际: {createFormulaResponse.StatusCode}");
        createdFormula!.Id.Should().NotBeEmpty();
        var createdFormulaId = createdFormula.Id;

        // Step 7: Formula contains herb
        var (formulaDetailResponse, formulaDetail) = await GetAsync<FormulaDetailDto>(
            admin, $"/api/v1/formulas/{createdFormulaId}");
        formulaDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        formulaDetail!.Herbs.Should().NotBeEmpty();
        formulaDetail.Herbs.Should().Contain(h => h.HerbId == createdHerbId);

        // Step 8: Create patient
        var patientInput = new PatientInputDto
        {
            Name = UniqueName("张三"),
            Gender = Gender.Male,
            BirthDate = new DateTime(1985, 3, 15),
            PhoneNumber = UniquePhone(),
            IdNumber = $"11010119850315{Random.Shared.Next(1000, 9999)}",
            Address = "北京市朝阳区"
        };

        var (createPatientResponse, createdPatient) = await PostAsync<PatientDetailDto>(admin, "/api/v1/patients", patientInput);
        createPatientResponse.IsSuccessStatusCode.Should().BeTrue($"创建患者应成功, 实际: {createPatientResponse.StatusCode}");
        createdPatient!.Id.Should().NotBeEmpty();
        var createdPatientId = createdPatient.Id;

        // Step 9: Patient is queryable
        var (patientListResponse, patientList) = await GetAsync<PagedResult<PatientDetailDto>>(
            admin, "/api/v1/patients?pageSize=100");
        patientListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        patientList!.Items.Should().Contain(p => p.Id == createdPatientId);

        // Step 10: Admin can view all users
        var (userListResponse, userList) = await GetAsync<PagedResult<UserDetailDto>>(
            admin, "/api/v1/users?pageSize=100");
        userListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        userList!.Items.Should().Contain(u => u.Id == createdDoctorId);
    }
}
