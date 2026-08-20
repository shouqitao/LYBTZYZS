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
    protected override string Username => "sysadmin";
    protected override string Password => "SysAdmin@2026!";

    [Fact(Skip = "Role not available on remote")]
    [Trait("US", "US-REG-002")]
    public async Task CreateRegistration_Succeeds()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var patient = await PatientApi.CreatePatientAsync(new PatientInputDto
        {
            Name = $"E2E患者{Guid.NewGuid():N}".Substring(0, 10),
            Gender = Gender.Male,
            PhoneNumber = UniquePhone()
        });
        patient.Success.Should().BeTrue(patient.Message);
        var patientId = patient.Data!.Id;

        var input = new RegistrationInputDto
        {
            PatientId = patientId,
            PatientName = patient.Data.Name,
            DoctorId = Guid.NewGuid()
        };
        var resp = await RegistrationApi.CreateAsync(input);
        resp.Should().NotBeNull();
        if (resp.Data != null)
        {
            await RegistrationApi.CancelAsync(resp.Data.Id);
        }
        await PatientApi.DeletePatientAsync(patientId);
    }

    [Fact(Skip = "Role not available on remote")]
    [Trait("US", "US-REG-003")]
    public async Task GetPendingQueue_ReturnsData()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await RegistrationApi.GetListAsync(1, 10);
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();
    }

    [Fact(Skip = "Role not available on remote")]
    [Trait("US", "US-PAT-003")]
    public async Task CreatePatient_Succeeds()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var input = new PatientInputDto
        {
            Name = $"E2E患者{Guid.NewGuid():N}".Substring(0, 10),
            Gender = Gender.Female,
            PhoneNumber = UniquePhone()
        };
        var created = await PatientApi.CreatePatientAsync(input);
        created.Success.Should().BeTrue(created.Message);
        created.Data.Should().NotBeNull();
        if (created.Data != null)
        {
            await PatientApi.DeletePatientAsync(created.Data.Id);
        }
    }

    [Fact(Skip = "Role not available on remote")]
    [Trait("US", "US-PAT-005")]
    public async Task GetPatientByIdNumber_FindsPatient()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var idNumber = UniqueIdNumber();
        var input = new PatientInputDto
        {
            Name = $"E2E患者{Guid.NewGuid():N}".Substring(0, 10),
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

        await PatientApi.DeletePatientAsync(patientId);
    }
}
