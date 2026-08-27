// ---------------------------------------------------------------------------
// HerbCrudE2ETests — US-HERB-001 药材 CRUD + 搜索 + 批量 全链路
// 真实链路：桌面 IApiClientHerbs → LocalWebAPI HerbsController → LocalDB
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop.E2E.HerbFormulaFlow;

[Collection("E2ELocal")]
public class HerbCrudE2ETests : E2ETestBase
{
    private static HerbInputDto NewHerb() => new()
    {
        Name = UniqueName("黄芪"),
        Category = "补气药",
        Unit = "克",
        Price = 25.5m,
        CostPrice = 18m,
        Effect = "补气升阳",
        Origin = "甘肃"
    };

    [Fact]
    public async Task Create_Herb_ReturnsDetail()
    {
        await LoginAsAdminAsync();
        var input = NewHerb();

        var result = await HerbsApi.CreateHerbAsync(input);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data!.Id);
        Assert.Equal(input.Name, result.Data.Name);
        Assert.Equal("补气药", result.Data.Category);
        Assert.Equal(25.5m, result.Data.Price);
        Assert.Equal(CommonStatus.Enabled, result.Data.Status);
    }

    [Fact]
    public async Task GetPaged_ReturnsHerbs()
    {
        await LoginAsAdminAsync();
        var created = await HerbsApi.CreateHerbAsync(NewHerb());

        var paged = await HerbsApi.GetHerbsAsync(1, 20, created.Data!.Name);

        Assert.True(paged.Success);
        Assert.Contains(paged.Data!.Items, h => h.Id == created.Data.Id);
    }

    [Fact]
    public async Task Update_Herb_PersistsChanges()
    {
        await LoginAsAdminAsync();
        var created = await HerbsApi.CreateHerbAsync(NewHerb());
        var newName = UniqueName("党参");

        var updated = await HerbsApi.UpdateHerbAsync(created.Data!.Id, new HerbInputDto
        {
            Id = created.Data.Id,
            Name = newName,
            Category = "补气药",
            Unit = "克",
            Price = 30m,
            Effect = "补中益气"
        });

        Assert.True(updated.Success, updated.Message);
        Assert.Equal(newName, updated.Data!.Name);
        Assert.Equal(30m, updated.Data.Price);

        var detail = await HerbsApi.GetHerbByIdAsync(created.Data.Id);
        Assert.Equal(newName, detail.Data!.Name);
    }

    [Fact]
    public async Task Delete_Herb_SoftDeletes()
    {
        await LoginAsAdminAsync();
        var created = await HerbsApi.CreateHerbAsync(NewHerb());

        var deleted = await HerbsApi.DeleteHerbAsync(created.Data!.Id);

        Assert.True(deleted.Success, deleted.Message);

        var paged = await HerbsApi.GetHerbsAsync(1, 100, created.Data.Name);
        Assert.DoesNotContain(paged.Data!.Items, h => h.Id == created.Data.Id);
    }

    [Fact]
    public async Task ToggleStatus_DisablesAndEnables()
    {
        await LoginAsAdminAsync();
        var created = await HerbsApi.CreateHerbAsync(NewHerb());

        var disabled = await HerbsApi.ToggleStatusAsync(created.Data!.Id);
        Assert.True(disabled.Success, disabled.Message);
        Assert.Equal(CommonStatus.Disabled, disabled.Data!.Status);

        var enabled = await HerbsApi.ToggleStatusAsync(created.Data.Id);
        Assert.True(enabled.Success);
        Assert.Equal(CommonStatus.Enabled, enabled.Data!.Status);
    }

    [Fact]
    public async Task BatchDisable_ThenBatchEnable_UpdatesAll()
    {
        await LoginAsAdminAsync();
        var herb1 = await HerbsApi.CreateHerbAsync(NewHerb());
        var herb2 = await HerbsApi.CreateHerbAsync(NewHerb());
        var ids = new List<Guid> { herb1.Data!.Id, herb2.Data!.Id };

        var disabled = await HerbsApi.BatchDisableAsync(new BatchDeleteInputDto { Ids = ids });
        Assert.True(disabled.Success, disabled.Message);
        Assert.Equal(2, disabled.Data!.SuccessCount);

        var d1 = await HerbsApi.GetHerbByIdAsync(herb1.Data.Id);
        var d2 = await HerbsApi.GetHerbByIdAsync(herb2.Data.Id);
        Assert.Equal(CommonStatus.Disabled, d1.Data!.Status);
        Assert.Equal(CommonStatus.Disabled, d2.Data!.Status);

        var enabled = await HerbsApi.BatchEnableAsync(new BatchDeleteInputDto { Ids = ids });
        Assert.True(enabled.Success);
        Assert.Equal(2, enabled.Data!.SuccessCount);
    }

    [Fact]
    public async Task BatchDelete_RemovesMultipleHerbs()
    {
        await LoginAsAdminAsync();
        var herb1 = await HerbsApi.CreateHerbAsync(NewHerb());
        var herb2 = await HerbsApi.CreateHerbAsync(NewHerb());

        var result = await HerbsApi.BatchDeleteAsync(new BatchDeleteInputDto
        {
            Ids = new List<Guid> { herb1.Data!.Id, herb2.Data!.Id }
        });

        Assert.True(result.Success, result.Message);
        Assert.Equal(2, result.Data!.SuccessCount);
    }

    [Fact]
    public async Task Restore_DeletedHerb_BringsItBack()
    {
        await LoginAsAdminAsync();
        var created = await HerbsApi.CreateHerbAsync(NewHerb());

        await HerbsApi.DeleteHerbAsync(created.Data!.Id);
        var restored = await HerbsApi.RestoreAsync(created.Data!.Id);

        Assert.True(restored.Success, restored.Message);
        Assert.Equal(CommonStatus.Enabled, restored.Data!.Status);
    }
}
