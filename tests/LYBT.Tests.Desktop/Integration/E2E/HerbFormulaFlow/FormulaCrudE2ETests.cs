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

    private static FormulaInputDto NewFormula(
        Guid herbId,
        string herbName,
        Guid? secondHerbId = null,
        string? secondHerbName = null)
    {
        var herbs = new List<FormulaHerbItemInputDto>
        {
            new()
            {
                HerbId = herbId,
                HerbName = herbName,
                Dosage = 9,
                Unit = "克",
                Usage = "先煎",
                ProcessingMethod = "制",
                DecocteMethod = DecocteMethod.PreDecoct
            }
        };

        if (secondHerbId is { } secondId && secondHerbName is { } secondName)
        {
            herbs.Add(new FormulaHerbItemInputDto
            {
                HerbId = secondId,
                HerbName = secondName,
                Dosage = 15,
                Unit = "克",
                ProcessingMethod = "炒"
            });
        }

        return new FormulaInputDto
        {
            Name = UniqueName("四君子汤"),
            Category = "补益剂",
            Effect = "益气健脾",
            Usage = "水煎服",
            IsShared = true,
            Herbs = herbs
        };
    }

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

    /// <summary>
    /// US-FORM-014：克隆副本的药材组成必须逐字段复制（药味/剂量/单位/用法/炮制/煎法），
    /// 仅药材行主键是新实体。回归点：克隆曾漏拷 ProcessingMethod（炮制方法）。
    /// </summary>
    [Fact]
    public async Task Clone_Formula_CopiesHerbComposition()
    {
        await LoginAsAdminAsync();
        var preDecoctHerb = await CreateHerbAsync(UniqueName("附子"));
        var friedHerb = await CreateHerbAsync(UniqueName("白术"));
        var source = await FormulasApi.CreateFormulaAsync(
            NewFormula(preDecoctHerb, "附子", friedHerb, "白术"));
        Assert.True(source.Success, source.Message);

        var cloned = await FormulasApi.CloneFormulaAsync(source.Data!.Id);

        Assert.True(cloned.Success, cloned.Message);
        Assert.NotNull(cloned.Data);
        Assert.Equal(source.Data.Herbs.Count, cloned.Data!.Herbs.Count);

        foreach (var sourceHerb in source.Data.Herbs)
        {
            var copied = Assert.Single(cloned.Data.Herbs, h => h.HerbId == sourceHerb.HerbId);
            Assert.Equal(sourceHerb.HerbName, copied.HerbName);
            Assert.Equal(sourceHerb.Dosage, copied.Dosage);
            Assert.Equal(sourceHerb.Unit, copied.Unit);
            Assert.Equal(sourceHerb.Usage, copied.Usage);
            Assert.Equal(sourceHerb.ProcessingMethod, copied.ProcessingMethod);
            Assert.Equal(sourceHerb.DecocteMethod, copied.DecocteMethod);
            // 药材行是新建实体（副本可独立编辑），但引用的药材主数据一致
            Assert.NotEqual(sourceHerb.Id, copied.Id);
        }

        // 处方级字段随副本复制
        Assert.Equal(source.Data.Effect, cloned.Data.Effect);
        Assert.Equal(source.Data.Usage, cloned.Data.Usage);
        Assert.Equal(source.Data.Category, cloned.Data.Category);
    }

    /// <summary>
    /// US-FORM-014：副本名称 = 源名称 + " (副本)"（服务端格式：半角空格 + 全角括号）。
    /// </summary>
    [Fact]
    public async Task Clone_Formula_AppendsCopySuffixToName()
    {
        await LoginAsAdminAsync();
        var herbId = await CreateHerbAsync(UniqueName("川芎"));
        var source = await FormulasApi.CreateFormulaAsync(NewFormula(herbId, "川芎"));

        var cloned = await FormulasApi.CloneFormulaAsync(source.Data!.Id);

        Assert.True(cloned.Success, cloned.Message);
        Assert.Equal($"{source.Data!.Name} (副本)", cloned.Data!.Name);
        Assert.NotEqual(source.Data.Id, cloned.Data.Id);
    }

    /// <summary>
    /// US-FORM-014：克隆是只读复制——原验方（名称/属性/药材组成/更新时间）在克隆后完全不变。
    /// </summary>
    [Fact]
    public async Task Clone_Formula_LeavesSourceUnchanged()
    {
        await LoginAsAdminAsync();
        var herb1 = await CreateHerbAsync(UniqueName("熟地"));
        var herb2 = await CreateHerbAsync(UniqueName("山茱萸"));
        var created = await FormulasApi.CreateFormulaAsync(NewFormula(herb1, "熟地", herb2, "山茱萸"));
        var before = await FormulasApi.GetFormulaByIdAsync(created.Data!.Id);
        Assert.True(before.Success, before.Message);

        var cloned = await FormulasApi.CloneFormulaAsync(created.Data.Id);
        Assert.True(cloned.Success, cloned.Message);

        var after = await FormulasApi.GetFormulaByIdAsync(created.Data.Id);
        Assert.True(after.Success, after.Message);
        Assert.Equal(before.Data!.Name, after.Data!.Name);
        Assert.Equal(before.Data.Effect, after.Data.Effect);
        Assert.Equal(before.Data.Usage, after.Data.Usage);
        Assert.Equal(before.Data.IsShared, after.Data.IsShared);
        Assert.Equal(before.Data.ValidationStatus, after.Data.ValidationStatus);
        Assert.Equal(before.Data.CreatedAt, after.Data.CreatedAt);
        Assert.Equal(before.Data.UpdatedAt, after.Data.UpdatedAt);
        Assert.Equal(before.Data.Herbs.Count, after.Data.Herbs.Count);
        foreach (var herb in before.Data.Herbs)
        {
            var stillThere = Assert.Single(after.Data.Herbs, h => h.Id == herb.Id);
            Assert.Equal(herb.HerbId, stillThere.HerbId);
            Assert.Equal(herb.Dosage, stillThere.Dosage);
            Assert.Equal(herb.Unit, stillThere.Unit);
        }

        // 原验方仍可检索（未被克隆覆盖或软删除）
        var paged = await FormulasApi.GetFormulasAsync(1, 20, after.Data.Name);
        Assert.Contains(paged.Data!.Items, f => f.Id == after.Data.Id);
    }

    /// <summary>
    /// US-FORM-014：副本初始为 Draft（草稿）——需求「生成新验方（Draft 初始）」。
    /// </summary>
    [Fact]
    public async Task Clone_Formula_StartsAsDraft()
    {
        await LoginAsAdminAsync();
        var herbId = await CreateHerbAsync(UniqueName("丹参"));
        var source = await FormulasApi.CreateFormulaAsync(NewFormula(herbId, "丹参"));

        var cloned = await FormulasApi.CloneFormulaAsync(source.Data!.Id);

        Assert.True(cloned.Success, cloned.Message);
        Assert.Equal(FormulaValidationStatus.Draft, cloned.Data!.ValidationStatus);
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

    [Fact]
    public async Task FormulaWriteEndpoints_AreNotShadowedByBaseCrudRoutes()
    {
        // L1-01 回归：Local FormulasController 曾以新方法名（DeleteFormula/ToggleFormulaStatus/
        // BatchDeleteFormulas/RestoreFormula）+ 绝对路由声明这 4 个端点，未 override 基类虚方法：
        //   - batch-delete 模板与基类完全相同 → AmbiguousMatchException
        //   - {id} / {id}/toggle-status / {id}/restore 被基类 {id:guid} 抢占 → 基类 NotSupportedException
        // 四条路径全部 500。此处逐条走真实链路断言可用。
        await LoginAsAdminAsync();
        var herbId = await CreateHerbAsync(UniqueName("当归"));

        var created = await FormulasApi.CreateFormulaAsync(NewFormula(herbId, "当归"));
        Assert.True(created.Success, created.Message);
        var formulaId = created.Data!.Id;

        var toggled = await FormulasApi.ToggleStatusAsync(formulaId);
        Assert.True(toggled.Success, toggled.Message);
        Assert.Equal(CommonStatus.Disabled, toggled.Data!.Status);

        var deleted = await FormulasApi.DeleteFormulaAsync(formulaId);
        Assert.True(deleted.Success, deleted.Message);

        var restored = await FormulasApi.RestoreAsync(formulaId);
        Assert.True(restored.Success, restored.Message);

        var second = await FormulasApi.CreateFormulaAsync(NewFormula(herbId, "当归"));
        Assert.True(second.Success, second.Message);

        var batchDeleted = await FormulasApi.BatchDeleteAsync(
            new BatchDeleteInputDto { Ids = [second.Data!.Id] });
        Assert.True(batchDeleted.Success, batchDeleted.Message);
    }
}
