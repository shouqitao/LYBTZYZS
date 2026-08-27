// ---------------------------------------------------------------------------
// FormulaCrudE2ETests — US-FORM-001 验方 CRUD + 克隆 + 导入 全链路
// 真实链路：桌面 IApiClientFormulas → LocalWebAPI FormulasController → LocalDB
// 注：批量导入经原始 HTTP 直连（桌面 FormulasHttpApiClient 传 FormulaBatchImportInputDto
// 信封，而本地控制器契约收 List<FormulaImportItemDto>——既有客户端契约偏差，不改客户端层）
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using System.Net.Http.Json;
using System.Text.Json;

namespace LYBT.Tests.Desktop.E2E.HerbFormulaFlow;

[Collection("E2ELocal")]
public class FormulaCrudE2ETests : E2ETestBase
{
    private async Task<Guid> CreateHerbAsync(string name)
    {
        var created = await HerbsApi.CreateHerbAsync(new HerbInputDto { Name = name, Unit = "克", Price = 10m });
        Assert.True(created.Success, created.Message);
        return created.Data!.Id;
    }

    private static FormulaInputDto NewFormula(Guid herbId, string herbName) => new()
    {
        Name = UniqueName("四君子汤"),
        Category = "补益剂",
        Effect = "益气健脾",
        Usage = "水煎服",
        IsShared = true,
        Herbs = new List<FormulaHerbItemInputDto>
        {
            new()
            {
                HerbId = herbId,
                HerbName = herbName,
                Dosage = 10,
                Unit = "克"
            }
        }
    };

    [Fact]
    public async Task Create_Formula_WithHerb_ReturnsDetail()
    {
        await LoginAsAdminAsync();
        var herbId = await CreateHerbAsync(UniqueName("人参"));
        var input = NewFormula(herbId, "人参");

        var result = await FormulasApi.CreateFormulaAsync(input);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data!.Id);
        Assert.Equal(input.Name, result.Data.Name);
        Assert.Single(result.Data.Herbs);
        Assert.Equal(herbId, result.Data.Herbs[0].HerbId);
    }

    [Fact]
    public async Task GetPaged_ReturnsFormulas()
    {
        await LoginAsAdminAsync();
        var herbId = await CreateHerbAsync(UniqueName("茯苓"));
        var created = await FormulasApi.CreateFormulaAsync(NewFormula(herbId, "茯苓"));

        var paged = await FormulasApi.GetFormulasAsync(1, 20, created.Data!.Name);

        Assert.True(paged.Success);
        Assert.Contains(paged.Data!.Items, f => f.Id == created.Data.Id);
    }

    [Fact]
    public async Task Update_Formula_PersistsChanges()
    {
        await LoginAsAdminAsync();
        var herbId = await CreateHerbAsync(UniqueName("甘草"));
        var created = await FormulasApi.CreateFormulaAsync(NewFormula(herbId, "甘草"));
        var newName = UniqueName("六君子汤");

        var updated = await FormulasApi.UpdateFormulaAsync(created.Data!.Id, new FormulaInputDto
        {
            Id = created.Data.Id,
            Name = newName,
            Category = "补益剂",
            Effect = "益气健脾化痰",
            Usage = "水煎服",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbId = herbId, HerbName = "甘草", Dosage = 6, Unit = "克" }
            }
        });

        Assert.True(updated.Success, updated.Message);
        Assert.Equal(newName, updated.Data!.Name);
    }

    [Fact]
    public async Task Clone_Formula_CreatesCopy()
    {
        await LoginAsAdminAsync();
        var herbId = await CreateHerbAsync(UniqueName("当归"));
        var created = await FormulasApi.CreateFormulaAsync(NewFormula(herbId, "当归"));

        var cloned = await FormulasApi.CloneFormulaAsync(created.Data!.Id);

        Assert.True(cloned.Success, cloned.Message);
        Assert.NotEqual(created.Data!.Id, cloned.Data!.Id);
        // 服务端克隆命名为「原名 (副本)」
        Assert.StartsWith(created.Data.Name, cloned.Data.Name);
        Assert.Single(cloned.Data.Herbs);
    }

    [Fact]
    public async Task BatchImport_Formulas_ImportsAndPersists()
    {
        await LoginAsAdminAsync();
        var herbId = await CreateHerbAsync(UniqueName("白芍"));
        var importName = UniqueName("导入方");

        // 本地控制器契约：List<FormulaImportItemDto>（与桌面客户端信封契约不同——直连原始 HTTP）
        var body = new List<FormulaImportItemDto>
        {
            new()
            {
                Name = importName,
                Category = "导入类",
                Effect = "导入功效",
                Usage = "水煎服",
                Herbs = new List<FormulaHerbImportItemDto>
                {
                    new() { HerbName = "白芍", Dosage = 12, Unit = "克" }
                }
            }
        };

        var response = await Client.PostAsJsonAsync("/api/v1/formulas/batch-import", body);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(json.GetProperty("success").GetBoolean(), json.ToString());

        var paged = await FormulasApi.GetFormulasAsync(1, 20, importName);
        Assert.Contains(paged.Data!.Items, f => f.Name == importName);
    }
}
