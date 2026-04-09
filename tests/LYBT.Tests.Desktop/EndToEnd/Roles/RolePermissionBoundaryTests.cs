using FluentAssertions;
using System.Threading;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Roles;

[Collection("E2E")]
public class RolePermissionBoundaryTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;
    private readonly E2ECollectionFixture _fixture;

    public RolePermissionBoundaryTests(E2ECollectionFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    private async Task LoginAsRoleAsync(string role)
    {
        var (username, password) = role switch
        {
            E2ECollectionFixture.Roles.SysAdmin => ("sysadmin", "DevPass123!"),
            E2ECollectionFixture.Roles.Admin => ("e2e_admin", "AdminPass123!"),
            E2ECollectionFixture.Roles.Doctor => ("e2e_doctor", "DoctorPass123!"),
            E2ECollectionFixture.Roles.Receptionist => ("e2e_receptionist", "ReceptionistPass123!"),
            _ => throw new ArgumentException($"Unknown role: {role}")
        };
        await LoginAsAsync(username, password);
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Permission")]
    [Trait("Role", "Receptionist")]
    public async Task Receptionist_CannotAccessUserManagement()
    {
        await LoginAsRoleAsync(E2ECollectionFixture.Roles.Receptionist);

        await E2EAssertionHelpers.AssertForbidden(async () => await UserApi.GetUsersAsync());
        _output.WriteLine("Receptionist correctly denied access to user management (AdminOnly)");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Permission")]
    [Trait("Role", "Receptionist")]
    public async Task Receptionist_CannotCreateHerb()
    {
        await LoginAsRoleAsync(E2ECollectionFixture.Roles.Receptionist);

        var herb = new HerbInputDto
        {
            Name = "测试药材_权限", Unit = "克", Price = 10m
        };

        await E2EAssertionHelpers.AssertForbidden(async () => await HerbApi.CreateHerbAsync(herb));
        _output.WriteLine("Receptionist correctly denied herb creation (DoctorOrAdmin)");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Permission")]
    [Trait("Role", "Receptionist")]
    public async Task Receptionist_CannotCreateMedicalCase()
    {
        await LoginAsRoleAsync(E2ECollectionFixture.Roles.Receptionist);

        var caseInput = new MedicalCaseInputDto
        {
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "测试", TcmDiagnosis = "测试"
            }
        };

        await E2EAssertionHelpers.AssertForbidden(
            async () => await MedicalCaseApi.CreateMedicalCaseAsync(caseInput));
        _output.WriteLine("Receptionist correctly denied medical case creation (DoctorOrAdmin)");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Permission")]
    [Trait("Role", "Receptionist")]
    public async Task Receptionist_CanAccessPatients()
    {
        await LoginAsRoleAsync(E2ECollectionFixture.Roles.Receptionist);

        var response = await PatientApi.GetPatientsAsync();

        response.Success.Should().BeTrue("Receptionist should access patient list (no [Authorize] on PatientsController)");
        _output.WriteLine($"Receptionist can access patients: {response.Data?.Items.Count ?? 0} found");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Permission")]
    [Trait("Role", "Doctor")]
    public async Task Doctor_CannotManageUsers()
    {
        await LoginAsRoleAsync(E2ECollectionFixture.Roles.Doctor);

        await E2EAssertionHelpers.AssertForbidden(async () => await UserApi.GetUsersAsync());
        _output.WriteLine("Doctor correctly denied access to user management (AdminOnly)");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Permission")]
    [Trait("Role", "Doctor")]
    public async Task Doctor_CanCreateHerb()
    {
        await LoginAsRoleAsync(E2ECollectionFixture.Roles.Doctor);

        var herb = new HerbInputDto
        {
            Name = $"医生药材_{Guid.NewGuid():N}".Substring(0, 20),
            Unit = "克",
            Price = 12.5m
        };

        var response = await HerbApi.CreateHerbAsync(herb);
        response.Success.Should().BeTrue("Doctor should create herbs (DoctorOrAdmin includes Doctor)");
        _output.WriteLine($"Doctor created herb: {response.Data}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Permission")]
    [Trait("Role", "Doctor")]
    public async Task Doctor_CanCreateMedicalCase()
    {
        await LoginAsRoleAsync(E2ECollectionFixture.Roles.Doctor);

        var patientResponse = await CreateTestPatientAsync();
        var loginResponse = _fixture.GetLoginResponseForRole(E2ECollectionFixture.Roles.Doctor);

        var caseInput = new MedicalCaseInputDto
        {
            PatientId = patientResponse.Id,
            UserId = loginResponse.User.Id,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "头痛两日", TcmDiagnosis = "风寒头痛"
            }
        };

        var response = await MedicalCaseApi.CreateMedicalCaseAsync(caseInput);
        response.Success.Should().BeTrue("Doctor should create medical cases (DoctorOrAdmin)");
        _output.WriteLine($"Doctor created medical case: {response.Data?.Id}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Permission")]
    [Trait("Role", "Admin")]
    public async Task Admin_CanManageUsers()
    {
        await LoginAsRoleAsync(E2ECollectionFixture.Roles.Admin);

        var response = await UserApi.GetUsersAsync();

        response.Success.Should().BeTrue("Admin should access user management (AdminOnly includes Admin)");
        _output.WriteLine($"Admin can manage users: {response.Data?.Items.Count ?? 0} found");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Permission")]
    [Trait("Role", "Admin")]
    public async Task Admin_CannotPerformSuperAdminOnlyActions()
    {
        await LoginAsRoleAsync(E2ECollectionFixture.Roles.Admin);

        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/diagnostics/logging/status");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden,
            "Admin should be denied SuperAdminOnly diagnostics endpoints");
        _output.WriteLine($"Admin correctly denied SuperAdminOnly action: {response.StatusCode}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Permission")]
    [Trait("Role", "Admin")]
    public async Task Admin_CanManageHerbs()
    {
        await LoginAsRoleAsync(E2ECollectionFixture.Roles.Admin);

        var herb = new HerbInputDto
        {
            Name = $"管理员药材_{Guid.NewGuid():N}".Substring(0, 20),
            Unit = "克",
            Price = 15.0m
        };

        var response = await HerbApi.CreateHerbAsync(herb);
        response.Success.Should().BeTrue("Admin should create herbs (DoctorOrAdmin includes Admin)");
        _output.WriteLine($"Admin created herb: {response.Data}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Permission")]
    [Trait("Role", "CrossRole")]
    public async Task Doctor_CannotDeleteUsers()
    {
        await LoginAsRoleAsync(E2ECollectionFixture.Roles.Doctor);

        await E2EAssertionHelpers.AssertForbidden(
            async () => await UserApi.DeleteUserAsync(Guid.NewGuid()));
        _output.WriteLine("Doctor correctly denied user deletion (AdminOnly)");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Permission")]
    [Trait("Role", "CrossRole")]
    public async Task Receptionist_CanCreateRegistration()
    {
        await LoginAsRoleAsync(E2ECollectionFixture.Roles.Receptionist);

        var patientResponse = await CreateTestPatientAsync();
        var doctorLogin = _fixture.GetLoginResponseForRole(E2ECollectionFixture.Roles.Doctor);

        var registration = new Shared.Models.Contracts.Registration.RegistrationInputDto
        {
            PatientId = patientResponse.Id,
            PatientName = patientResponse.Name,
            DoctorId = doctorLogin.User.Id,
            DoctorName = doctorLogin.User.RealName ?? "Doctor"
        };

        var response = await RegistrationApi.CreateAsync(registration);
        response.Success.Should().BeTrue(
            "Receptionist should create registrations (no [Authorize] on RegistrationController)");
        _output.WriteLine($"Receptionist created registration: {response.Data?.Id}");
    }

    private async Task<(Guid Id, string Name)> CreateTestPatientAsync()
    {
        var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
        var patient = new PatientInputDto
        {
            Name = $"权限测试_{suffix}",
            Gender = Gender.Male,
            IdNumber = GenerateIdNumber(),
            PinYinCode = $"QXCS{suffix}",
            Address = "北京市测试区测试街道1号",
            PhoneNumber = $"138{Random.Shared.Next(10000000, 99999999)}"
        };

        var response = await PatientApi.CreatePatientAsync(patient);
        response.Success.Should().BeTrue("Test patient creation should succeed");
        return (response.Data!.Id, patient.Name);
    }

    private static int _counter = 0;

    private static string GenerateIdNumber()
    {
        var unique = Interlocked.Increment(ref _counter);
        var day = 10 + (unique % 18);
        var seq = 100 + (unique % 899);
        var body = $"110101199001{day:D2}{seq:D3}";
        int[] weights = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
        char[] checkDigits = { '1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };
        var sum = 0;
        for (var i = 0; i < 17; i++) sum += (body[i] - '0') * weights[i];
        return body + checkDigits[sum % 11];
    }
}
