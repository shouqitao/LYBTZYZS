// ---------------------------------------------------------------------------
// PatientSearchE2ETests — US-PAT-002 搜索 全链路
// 真实链路：桌面 IApiClientPatients.GetPatientsAsync(keyword) → LocalWebAPI → LocalDB
// 本地契约：keyword 按患者姓名/拼音码匹配（手机号/身份证不参与搜索；
// 身份证查询走专用端点 GET /api/v1/patients/by-id-number/{idNumber}）
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using System.Net.Http.Json;
using System.Text.Json;

namespace LYBT.Tests.Desktop.E2E.PatientFlow;

[Collection("E2ELocal")]
public class PatientSearchE2ETests : E2ETestBase
{
    private async Task<PatientDetailDto> CreatePatientAsync(string name)
    {
        var created = await PatientsApi.CreatePatientAsync(new PatientInputDto
        {
            Name = name,
            IdNumber = UniqueIdNumber(),
            PhoneNumber = UniquePhone(),
            Gender = Gender.Male
        });
        Assert.True(created.Success, created.Message);
        return created.Data!;
    }

    [Fact]
    public async Task Search_ByExactName_FindsPatient()
    {
        await LoginAsAdminAsync();
        var patient = await CreatePatientAsync(UniqueName("精准搜索"));

        var paged = await PatientsApi.GetPatientsAsync(1, 20, patient.Name);

        Assert.Contains(paged.Data!.Items, p => p.Id == patient.Id);
    }

    [Fact]
    public async Task Search_ByPartialName_FindsPatient()
    {
        await LoginAsAdminAsync();
        var patient = await CreatePatientAsync(UniqueName("前缀模糊"));

        var paged = await PatientsApi.GetPatientsAsync(1, 20, patient.Name[..4]);

        Assert.Contains(paged.Data!.Items, p => p.Id == patient.Id);
    }

    [Fact]
    public async Task Search_ByPinYin_FindsPatient()
    {
        await LoginAsAdminAsync();
        var patient = await CreatePatientAsync(UniqueName("拼音搜索"));

        // 使用服务端生成的拼音码（响应非脱敏字段）作为关键词
        var paged = await PatientsApi.GetPatientsAsync(1, 20, patient.PinYinCode);

        Assert.Contains(paged.Data!.Items, p => p.Id == patient.Id);
    }

    [Fact]
    public async Task GetByIdNumber_ReturnsPatient()
    {
        await LoginAsAdminAsync();
        var idNumber = UniqueIdNumber();
        var created = await PatientsApi.CreatePatientAsync(new PatientInputDto
        {
            Name = UniqueName("证件查询"),
            IdNumber = idNumber,
            PhoneNumber = UniquePhone(),
            Gender = Gender.Male
        });
        Assert.True(created.Success, created.Message);

        // 原始身份证号（响应为脱敏值）
        var response = await Client.GetAsync($"/api/v1/patients/by-id-number/{idNumber}");
        var patient = created.Data!;

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(json.GetProperty("success").GetBoolean(), json.ToString());
        Assert.Equal(patient.Id.ToString(), json.GetProperty("data").GetProperty("id").GetString());
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmpty()
    {
        await LoginAsAdminAsync();

        var paged = await PatientsApi.GetPatientsAsync(1, 20, $"无匹配_{Guid.NewGuid():N}");

        Assert.True(paged.Success);
        Assert.Empty(paged.Data!.Items);
    }

    [Fact]
    public async Task Search_SupportsPaging()
    {
        await LoginAsAdminAsync();
        for (var i = 0; i < 5; i++)
            await CreatePatientAsync(UniqueName("分页"));

        var page1 = await PatientsApi.GetPatientsAsync(1, 3);
        var page2 = await PatientsApi.GetPatientsAsync(2, 3);

        Assert.True(page1.Success);
        Assert.True(page2.Success);
        Assert.Equal(3, page1.Data!.Items.Count);
        Assert.Equal(1, page1.Data.CurrentPage);
        Assert.Equal(2, page2.Data!.CurrentPage);
        Assert.True(page2.Data.TotalCount >= 5);
    }
}
