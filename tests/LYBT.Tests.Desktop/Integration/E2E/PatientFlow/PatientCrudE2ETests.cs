// ---------------------------------------------------------------------------
// PatientCrudE2ETests — US-PAT-001 患者创建→分页→详情→更新→删除 全链路
// 真实链路：桌面 IApiClientPatients → LocalWebAPI PatientsController → Service → LocalDB
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using System.Net.Http;

namespace LYBT.Tests.Desktop.E2E.PatientFlow;

[Collection("E2ELocal")]
public class PatientCrudE2ETests : E2ETestBase
{
    private static PatientInputDto NewPatient() => new()
    {
        Name = UniqueName("患者"),
        IdNumber = UniqueIdNumber(),
        PhoneNumber = UniquePhone(),
        Gender = Gender.Female,
        BirthDate = new DateTime(1990, 5, 20)
    };

    [Fact]
    public async Task Create_Patient_ReturnsDetail()
    {
        await LoginAsAdminAsync();
        var input = NewPatient();

        var result = await PatientsApi.CreatePatientAsync(input);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data!.Id);
        Assert.Equal(input.Name, result.Data.Name);
        // 隐私契约：响应手机号脱敏（138*****740）
        Assert.Contains("****", result.Data.PhoneNumber);
        Assert.Equal(Gender.Female, result.Data.Gender);
        Assert.Equal(CommonStatus.Enabled, result.Data.Status);
    }

    [Fact]
    public async Task GetPaged_ReturnsCreatedPatient()
    {
        await LoginAsAdminAsync();
        var created = await PatientsApi.CreatePatientAsync(NewPatient());
        Assert.True(created.Success);

        var paged = await PatientsApi.GetPatientsAsync(1, 20, created.Data!.Name);

        Assert.True(paged.Success);
        Assert.NotNull(paged.Data);
        Assert.Contains(paged.Data!.Items, p => p.Id == created.Data.Id);
    }

    [Fact]
    public async Task GetById_ReturnsPatient()
    {
        await LoginAsAdminAsync();
        var created = await PatientsApi.CreatePatientAsync(NewPatient());

        var detail = await PatientsApi.GetPatientByIdAsync(created.Data!.Id);

        Assert.True(detail.Success);
        Assert.Equal(created.Data.Name, detail.Data!.Name);
        Assert.Equal(created.Data.IdNumber, detail.Data.IdNumber);
    }

    [Fact]
    public async Task Update_Patient_PersistsChanges()
    {
        await LoginAsAdminAsync();
        var input = NewPatient();
        var created = await PatientsApi.CreatePatientAsync(input);
        var newName = UniqueName("患者改");

        // 身份证/手机号取原始输入值（响应为脱敏值，回传会触发校验失败）
        var updated = await PatientsApi.UpdatePatientAsync(created.Data!.Id, new PatientInputDto
        {
            Id = created.Data.Id,
            Name = newName,
            IdNumber = input.IdNumber,
            PhoneNumber = input.PhoneNumber,
            Gender = input.Gender,
            BirthDate = input.BirthDate
        });

        Assert.True(updated.Success, updated.Message);
        Assert.Equal(newName, updated.Data!.Name);

        var detail = await PatientsApi.GetPatientByIdAsync(created.Data.Id);
        Assert.Equal(newName, detail.Data!.Name);
    }

    [Fact]
    public async Task Delete_Patient_SoftDeletes()
    {
        await LoginAsAdminAsync();
        var created = await PatientsApi.CreatePatientAsync(NewPatient());

        var deleted = await PatientsApi.DeletePatientAsync(created.Data!.Id);

        Assert.True(deleted.Success, deleted.Message);

        // 软删除后默认列表不再包含
        var paged = await PatientsApi.GetPatientsAsync(1, 100, created.Data.Name);
        Assert.DoesNotContain(paged.Data!.Items, p => p.Id == created.Data.Id);
    }

    [Fact]
    public async Task ToggleStatus_DisablesAndEnablesPatient()
    {
        await LoginAsAdminAsync();
        var created = await PatientsApi.CreatePatientAsync(NewPatient());

        var disabled = await PatientsApi.ToggleStatusAsync(created.Data!.Id);
        Assert.True(disabled.Success, disabled.Message);
        Assert.Equal(CommonStatus.Disabled, disabled.Data!.Status);

        var enabled = await PatientsApi.ToggleStatusAsync(created.Data.Id);
        Assert.True(enabled.Success);
        Assert.Equal(CommonStatus.Enabled, enabled.Data!.Status);
    }

    [Fact]
    public async Task Restore_DeletedPatient_BringsItBack()
    {
        await LoginAsAdminAsync();
        var created = await PatientsApi.CreatePatientAsync(NewPatient());

        await PatientsApi.DeletePatientAsync(created.Data!.Id);
        var restored = await PatientsApi.RestoreAsync(created.Data!.Id);

        Assert.True(restored.Success, restored.Message);
        Assert.Equal(CommonStatus.Enabled, restored.Data!.Status);

        var paged = await PatientsApi.GetPatientsAsync(1, 100, created.Data.Name);
        Assert.Contains(paged.Data!.Items, p => p.Id == created.Data.Id);
    }
}
