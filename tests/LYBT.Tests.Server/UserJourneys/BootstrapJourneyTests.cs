using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.UserJourneys;

/// <summary>
/// UAT Narrative 0: System bootstrap journey.
/// SysAdmin creates users (Admin/Doctor/Receptionist), verifies role-based permissions,
/// creates herbs with PinYin auto-generation, creates formulas, and validates health endpoint.
/// </summary>
[Collection("Users")]
public sealed class BootstrapJourneyTests : JourneyTestBase<UserFixture>
{
    private const string TestPassword = "TestBootstrap2025@";

    public BootstrapJourneyTests(UserFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Bootstrap_Full_Journey()
    {
        // Step 1: SysAdmin login and verify identity
        await ResetForJourneyAsync();
        var sysadmin = await LoginAsSysAdminAsync();

        var (currentResponse, currentUser) = await GetAsync<UserDetailDto>(sysadmin, "/api/v1/users/current");
        currentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        currentUser.Should().NotBeNull();

        // Step 2: Create Admin user
        var adminUsername = UniqueName("admin");
        var (createAdminResponse, adminUser) = await PostAsync<UserDetailDto>(sysadmin, "/api/v1/users", new UserInputDto
        {
            UserName = adminUsername,
            RealName = "王主任",
            Role = UserRole.Admin,
            Email = $"{adminUsername}@test.com",
            PhoneNumber = UniquePhone(),
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });
        createAdminResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        adminUser!.Role.Should().Be(UserRole.Admin);

        // Step 3: Create Doctor user
        var doctorUsername = UniqueName("doctor");
        var (createDoctorResponse, doctorUser) = await PostAsync<UserDetailDto>(sysadmin, "/api/v1/users", new UserInputDto
        {
            UserName = doctorUsername,
            RealName = "李医生",
            Role = UserRole.Doctor,
            Email = $"{doctorUsername}@test.com",
            PhoneNumber = UniquePhone(),
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });
        createDoctorResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        doctorUser!.Role.Should().Be(UserRole.Doctor);

        // Step 4: Create Receptionist user
        var receptionistUsername = UniqueName("recep");
        var (createRecepResponse, recepUser) = await PostAsync<UserDetailDto>(sysadmin, "/api/v1/users", new UserInputDto
        {
            UserName = receptionistUsername,
            RealName = "小张",
            Role = UserRole.Receptionist,
            Email = $"{receptionistUsername}@test.com",
            PhoneNumber = UniquePhone(),
            Password = TestPassword,
            ConfirmPassword = TestPassword
        });
        createRecepResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        recepUser!.Role.Should().Be(UserRole.Receptionist);

        // Step 5: Role-based permission verification
        var adminClient = await LoginAsAsync(adminUsername, TestPassword);
        var doctorClient = await LoginAsAsync(doctorUsername, TestPassword);
        var recepClient = await LoginAsAsync(receptionistUsername, TestPassword);

        // Admin can list users and herbs
        var adminUsersResponse = await adminClient.GetAsync("/api/v1/users");
        adminUsersResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Admin should list users");

        var adminHerbsResponse = await adminClient.GetAsync("/api/v1/herbs");
        adminHerbsResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Admin should list herbs");

        // Doctor can view medical cases but cannot create users
        var doctorCasesResponse = await doctorClient.GetAsync("/api/v1/medicalcases");
        doctorCasesResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Doctor should view cases");

        var doctorCreateUserResponse = await doctorClient.PostAsJsonAsync("/api/v1/users", new UserInputDto
        {
            UserName = "should_fail", RealName = "X", Role = UserRole.Doctor,
            Password = TestPassword, ConfirmPassword = TestPassword
        });
        doctorCreateUserResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Doctor cannot create users");

        // Receptionist can view patients but cannot view medical cases (DoctorOrAdmin policy)
        var recepPatientsResponse = await recepClient.GetAsync("/api/v1/patients");
        recepPatientsResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Receptionist should view patients");

        var recepCasesResponse = await recepClient.GetAsync("/api/v1/medicalcases");
        recepCasesResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Receptionist cannot view cases");

        // Step 6: Create herbs with PinYin auto-generation
        var (createHerb1Response, herb1) = await PostAsync<HerbDetailDto>(adminClient, "/api/v1/herbs",
            new HerbInputDto { Name = UniqueName("黄芪"), Unit = "克", Price = 0.5m });
        createHerb1Response.IsSuccessStatusCode.Should().BeTrue();
        herb1!.PinYinCode.Should().NotBeNullOrEmpty("PinYin should be auto-generated");
        var herb1Id = herb1.Id;

        var (createHerb2Response, herb2) = await PostAsync<HerbDetailDto>(adminClient, "/api/v1/herbs",
            new HerbInputDto { Name = UniqueName("当归"), Unit = "克", Price = 0.8m });
        createHerb2Response.IsSuccessStatusCode.Should().BeTrue();
        herb2!.PinYinCode.Should().NotBeNullOrEmpty();
        var herb2Id = herb2.Id;

        // Step 7: Herb search by keyword and PinYin
        var (herbSearchResponse, herbSearchResult) = await GetAsync<PagedResult<HerbDetailDto>>(
            adminClient, $"/api/v1/herbs?keyword={Uri.EscapeDataString("黄")}");
        herbSearchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        herbSearchResult!.Items.Should().Contain(h => h.Id == herb1Id);

        // Step 8: Create formula with herbs
        var formulaInput = new FormulaInputDto
        {
            Name = UniqueName("四君子汤"),
            Effect = "补气",
            Usage = "水煎服",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbId = herb1Id, HerbName = herb1.Name, Dosage = 15, Unit = "克" },
                new() { HerbId = herb2Id, HerbName = herb2.Name, Dosage = 10, Unit = "克" }
            }
        };

        var (createFormulaResponse, formula) = await PostAsync<FormulaDetailDto>(
            doctorClient, "/api/v1/formulas", formulaInput);
        createFormulaResponse.IsSuccessStatusCode.Should().BeTrue();
        formula!.Id.Should().NotBeEmpty();
        formula.Herbs.Should().HaveCount(2);

        var (getFormulaResponse, fetchedFormula) = await GetAsync<FormulaDetailDto>(
            doctorClient, $"/api/v1/formulas/{formula.Id}");
        getFormulaResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        fetchedFormula!.Herbs.Should().HaveCount(2);

        // Step 9: Anonymous health check
        var healthResponse = await AnonymousClient.GetAsync("/health");
        healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
