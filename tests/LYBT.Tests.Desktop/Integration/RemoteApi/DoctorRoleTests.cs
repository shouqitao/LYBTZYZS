using FluentAssertions;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.RemoteApi;

[Collection("RemoteApi")]
[Trait("Category", "RemoteApi")]
public class DoctorRoleTests : RemoteApiTestBase
{
    protected override string Username => "sysadmin";
    protected override string Password => "SysAdmin@2026!";

    [Fact(Skip = "Role not available on remote")]
    [Trait("US", "US-MC-001")]
    public async Task CreateMedicalCase_Succeeds()
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

        var input = new MedicalCaseInputDto
        {
            PatientId = patientId
        };
        var resp = await MedicalCaseApi.CreateMedicalCaseAsync(input);
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();

        if (resp.Data != null)
        {
            await MedicalCaseApi.DeleteMedicalCaseAsync(resp.Data.Id);
        }
        await PatientApi.DeletePatientAsync(patientId);
    }

    [Fact(Skip = "Role not available on remote")]
    [Trait("US", "US-MC-002")]
    public async Task UpdateMedicalCase_Succeeds()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var patient = await PatientApi.CreatePatientAsync(new PatientInputDto
        {
            Name = $"E2E患者{Guid.NewGuid():N}".Substring(0, 10),
            Gender = Gender.Female,
            PhoneNumber = UniquePhone()
        });
        patient.Success.Should().BeTrue(patient.Message);
        var patientId = patient.Data!.Id;

        var created = await MedicalCaseApi.CreateMedicalCaseAsync(new MedicalCaseInputDto
        {
            PatientId = patientId
        });
        created.Success.Should().BeTrue(created.Message);
        var caseId = created.Data!.Id;

        var updated = new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = patientId
        };
        var resp = await MedicalCaseApi.SaveAsync(caseId, updated);
        resp.Success.Should().BeTrue(resp.Message);

        await MedicalCaseApi.DeleteMedicalCaseAsync(caseId);
        await PatientApi.DeletePatientAsync(patientId);
    }

    [Fact(Skip = "Role not available on remote")]
    [Trait("US", "US-MC-007")]
    public async Task GetPendingCases_ReturnsData()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await MedicalCaseApi.GetMedicalCasesAsync(1, 10);
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();
    }

    [Fact(Skip = "Role not available on remote")]
    [Trait("US", "US-MC-012")]
    public async Task CompleteMedicalCase_Succeeds()
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

        var created = await MedicalCaseApi.CreateMedicalCaseAsync(new MedicalCaseInputDto
        {
            PatientId = patientId
        });
        created.Success.Should().BeTrue(created.Message);
        var caseId = created.Data!.Id;

        var complete = await MedicalCaseApi.CloseCaseAsync(caseId);
        complete.Should().NotBeNull();

        await MedicalCaseApi.DeleteMedicalCaseAsync(caseId);
        await PatientApi.DeletePatientAsync(patientId);
    }

    [Fact(Skip = "Role not available on remote")]
    [Trait("US", "US-PAT-002")]
    public async Task SearchPatients_ByKeyword()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await PatientApi.GetPatientsAsync(1, 10, keyword: "张");
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();
    }

    [Fact(Skip = "Role not available on remote")]
    [Trait("US", "US-HERB-002")]
    public async Task SearchHerbs_ByKeyword()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await HerbApi.GetHerbsAsync(1, 10, keyword: "参");
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();
    }

    [Fact(Skip = "Role not available on remote")]
    [Trait("US", "US-FORM-002")]
    public async Task SearchFormulas_ByKeyword()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await FormulaApi.GetFormulasAsync(1, 10, keyword: "汤");
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();
    }
}
