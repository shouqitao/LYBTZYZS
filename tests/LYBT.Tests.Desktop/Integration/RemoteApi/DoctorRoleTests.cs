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
    private string _doctorName = "";
    private string _adminName = "";

    protected override async Task SetupRoleAsync()
    {
        // sysadmin 创建 Admin → Admin 创建 Doctor → 登录 Doctor
        _adminName = UniqueUsername("e2eadmin");
        var admin = await CreateUserAsync(_adminName, RolePassword, UserRole.Admin, "E2E管理员");
        if (admin == null) throw new InvalidOperationException("sysadmin 创建 Admin 失败");

        await LoginAsAsync(_adminName, RolePassword);
        _doctorName = UniqueUsername("e2edoctor");
        var doctor = await CreateUserAsync(_doctorName, RolePassword, UserRole.Doctor, "E2E医生");
        if (doctor == null) throw new InvalidOperationException("Admin 创建 Doctor 失败");

        await LoginAsAsync(_doctorName, RolePassword);
    }

    /// <summary>Doctor 创建患者并建医案，返回 patientId/caseId</summary>
    private async Task<(Guid PatientId, Guid CaseId, string PatientName)> CreateCaseForDoctorAsync()
    {
        var patient = await PatientApi.CreatePatientAsync(new PatientInputDto
        {
            Name = UniqueName("E2E患者"),
            Gender = Gender.Male,
            PhoneNumber = UniquePhone()
        });
        patient.Success.Should().BeTrue(patient.Message);
        var patientId = patient.Data!.Id;

        var created = await MedicalCaseApi.CreateMedicalCaseAsync(new MedicalCaseInputDto { PatientId = patientId });
        created.Success.Should().BeTrue(created.Message);
        created.Data.Should().NotBeNull();
        // 角色无删除权限——交由 DisposeAsync 统一用 sysadmin 清理
        CreatedPatientIds.Add(patientId);
        CreatedMedicalCaseIds.Add(created.Data!.Id);
        return (patientId, created.Data!.Id, created.Data!.PatientName);
    }

    [SkippableFact]
    [Trait("US", "US-MC-001")]
    public async Task CreateMedicalCase_Succeeds()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var (patientId, caseId, _) = await CreateCaseForDoctorAsync();
        _ = (patientId, caseId); // 数据交由 DisposeAsync 清理
    }

    [SkippableFact]
    [Trait("US", "US-MC-002")]
    public async Task UpdateMedicalCase_Succeeds()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var (patientId, caseId, _) = await CreateCaseForDoctorAsync();

        var updated = new MedicalCaseInputDto { Id = caseId, PatientId = patientId };
        var resp = await MedicalCaseApi.SaveAsync(caseId, updated);
        resp.Success.Should().BeTrue(resp.Message);
        // 数据交由 DisposeAsync 清理
    }

    [SkippableFact]
    [Trait("US", "US-MC-007")]
    public async Task GetPendingCases_ReturnsData()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await MedicalCaseApi.GetMedicalCasesAsync(1, 10);
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();
    }

    [SkippableFact]
    [Trait("US", "US-MC-012")]
    public async Task CompleteMedicalCase_Succeeds()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var (patientId, caseId, _) = await CreateCaseForDoctorAsync();

        // 完成要求含中医诊断（CODE-01）——先保存诊断
        var withDiagnosis = new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = patientId,
            Consultation = new LYBT.Shared.Models.Contracts.Consultation.ConsultationInputDto
            {
                TcmDiagnosis = "气血两虚"
            }
        };
        var saved = await MedicalCaseApi.SaveAsync(caseId, withDiagnosis);
        saved.Success.Should().BeTrue(saved.Message);

        // 关闭医案属 Admin+（P1-11 权限）——切换 Admin 完成闭环后切回 Doctor
        await LoginAsAsync(_adminName, RolePassword);
        var complete = await MedicalCaseApi.CloseCaseAsync(caseId);
        complete.Success.Should().BeTrue(complete.Message);

        // 恢复 Doctor 会话（患者/医案交由 DisposeAsync 清理）
        await LoginAsAsync(_doctorName, RolePassword);
    }

    [SkippableFact]
    [Trait("US", "US-PAT-002")]
    public async Task SearchPatients_ByKeyword()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await PatientApi.GetPatientsAsync(1, 10, keyword: "张");
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();
    }

    [SkippableFact]
    [Trait("US", "US-HERB-002")]
    public async Task SearchHerbs_ByKeyword()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await HerbApi.GetHerbsAsync(1, 10, keyword: "参");
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();
    }

    [SkippableFact]
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
