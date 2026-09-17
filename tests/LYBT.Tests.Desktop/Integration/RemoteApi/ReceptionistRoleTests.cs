using FluentAssertions;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.RemoteApi;

[Collection("RemoteApi")]
[Trait("Category", "RemoteApi")]
public class ReceptionistRoleTests : RemoteApiTestBase
{
    private string _doctorName = "";
    private string _receptionistName = "";
    private Guid _doctorId = Guid.Empty;

    protected override async Task SetupRoleAsync()
    {
        // sysadmin 创建 Admin → Admin 创建 Doctor + Receptionist → 登录 Receptionist
        var adminName = UniqueUsername("e2eadmin");
        var admin = await CreateUserAsync(adminName, RolePassword, UserRole.Admin, "E2E管理员");
        if (admin == null) throw new InvalidOperationException("sysadmin 创建 Admin 失败");

        await LoginAsAsync(adminName, RolePassword);
        _doctorName = UniqueUsername("e2edoctor");
        var doctor = await CreateUserAsync(_doctorName, RolePassword, UserRole.Doctor, "E2E医生");
        if (doctor == null) throw new InvalidOperationException("Admin 创建 Doctor 失败");
        _doctorId = doctor.Id;

        _receptionistName = UniqueUsername("e2erecp");
        var rec = await CreateUserAsync(_receptionistName, RolePassword, UserRole.Receptionist, "E2E前台");
        if (rec == null) throw new InvalidOperationException("Admin 创建 Receptionist 失败");

        await LoginAsAsync(_receptionistName, RolePassword);
    }

    [Fact(Skip = "PRE-EXISTING: Remote E2E requires live remote WebAPI (localhost:5000 / production host); not started locally")]
    [Trait("US", "US-PAT-003")]
    public async Task CreatePatient_AsReceptionist_Succeeds()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var input = new PatientInputDto
        {
            Name = UniqueName("E2E患者"),
            Gender = Gender.Female,
            PhoneNumber = UniquePhone()
        };
        var created = await PatientApi.CreatePatientAsync(input);
        created.Success.Should().BeTrue(created.Message);
        created.Data.Should().NotBeNull();
        if (created.Data != null)
        {
            CreatedPatientIds.Add(created.Data.Id); // 交由 DisposeAsync 清理
        }
    }

    [Fact(Skip = "PRE-EXISTING: Remote E2E requires live remote WebAPI (localhost:5000 / production host); not started locally")]
    [Trait("US", "US-REG-002")]
    public async Task CreateRegistration_Succeeds()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var doctorId = _doctorId; // 角色创建阶段已获取，Receptionist 无用户列表权限

        var patient = await PatientApi.CreatePatientAsync(new PatientInputDto
        {
            Name = UniqueName("E2E患者"),
            Gender = Gender.Male,
            PhoneNumber = UniquePhone()
        });
        patient.Success.Should().BeTrue(patient.Message);
        var patientId = patient.Data!.Id;

        var input = new RegistrationInputDto
        {
            PatientId = patientId,
            PatientName = patient.Data.Name,
            DoctorId = doctorId,
            DoctorName = _doctorName
        };
        var resp = await RegistrationApi.CreateAsync(input);
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();

        if (resp.Data != null)
        {
            CreatedRegistrationIds.Add(resp.Data.Id); // 交由 DisposeAsync 清理
        }
        CreatedPatientIds.Add(patientId);
    }

    [Fact(Skip = "PRE-EXISTING: Remote E2E requires live remote WebAPI (localhost:5000 / production host); not started locally")]
    [Trait("US", "US-REG-003")]
    public async Task GetPendingQueue_ReturnsData()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await RegistrationApi.GetQueueAsync();
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();
    }

    [Fact(Skip = "PRE-EXISTING: Remote E2E requires live remote WebAPI (localhost:5000 / production host); not started locally")]
    [Trait("US", "US-PAT-005")]
    public async Task GetPatientByIdNumber_FindsPatient()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var idNumber = UniqueIdNumber();
        var input = new PatientInputDto
        {
            Name = UniqueName("E2E患者"),
            Gender = Gender.Male,
            PhoneNumber = UniquePhone(),
            IdNumber = idNumber
        };
        var created = await PatientApi.CreatePatientAsync(input);
        created.Success.Should().BeTrue(created.Message);
        var patientId = created.Data!.Id;

        var resp = await PatientApi.GetPatientsAsync(1, 10, keyword: idNumber);
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();

        CreatedPatientIds.Add(patientId); // 交由 DisposeAsync 清理
    }
}
