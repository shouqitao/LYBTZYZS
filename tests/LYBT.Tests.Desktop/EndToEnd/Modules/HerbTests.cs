using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Modules;

public class HerbTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public HerbTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static HerbInputDto CreateTestHerbInput(string suffix = "") => new()
    {
        Name = $"测试{suffix}_{Guid.NewGuid():N}"[..15],
        PinYinCode = "CSZY",
        Category = "清热药",
        Origin = "E2E测试产地",
        Spec = "10g/袋",
        Unit = "克",
        Price = 15.5m,
        CostPrice = 8.0m,
        Effect = "清热解毒",
        Usage = "水煎服",
        Remark = "E2E test herb"
    };

    #region CRUD

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "HerbManagement")]
    [Trait("Role", "Admin")]
    public async Task CreateHerb_ValidInput_ReturnsCreatedHerb()
    {
        await LoginAsSysadminAsync();
        var input = CreateTestHerbInput();

        var response = await HerbApi.CreateHerbAsync(input);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be(input.Name);
        _output.WriteLine($"Created herb: {response.Data.Id} - {response.Data.Name}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "HerbManagement")]
    [Trait("Role", "Admin")]
    public async Task GetHerbById_ExistingHerb_ReturnsDetail()
    {
        await LoginAsSysadminAsync();
        var createResponse = await HerbApi.CreateHerbAsync(CreateTestHerbInput());
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var herbId = createResponse.Data!.Id;

        var response = await HerbApi.GetHerbByIdAsync(herbId);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(herbId);
        _output.WriteLine($"Retrieved herb: {response.Data.Id}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "HerbManagement")]
    [Trait("Role", "Admin")]
    public async Task UpdateHerb_ValidInput_ReturnsUpdatedHerb()
    {
        await LoginAsSysadminAsync();
        var createResponse = await HerbApi.CreateHerbAsync(CreateTestHerbInput());
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var herbId = createResponse.Data!.Id;

        var updateInput = CreateTestHerbInput("upd");
        var response = await HerbApi.UpdateHerbAsync(herbId, updateInput);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be(updateInput.Name);
        _output.WriteLine($"Updated herb: {response.Data.Id} - {response.Data.Name}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "HerbManagement")]
    [Trait("Role", "Admin")]
    public async Task GetHerbs_WithPagination_ReturnsPagedResult()
    {
        await LoginAsSysadminAsync();
        await HerbApi.CreateHerbAsync(CreateTestHerbInput());

        var response = await HerbApi.GetHerbsAsync(1, 10);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().NotBeEmpty();
        response.Data.TotalCount.Should().BeGreaterThan(0);
        _output.WriteLine($"Herbs page: {response.Data.Items.Count}/{response.Data.TotalCount}");
    }

    #endregion

    #region Status Toggle

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "HerbManagement")]
    [Trait("Role", "Admin")]
    public async Task ToggleStatus_EnabledHerb_TogglesSuccessfully()
    {
        await LoginAsSysadminAsync();
        var createResponse = await HerbApi.CreateHerbAsync(CreateTestHerbInput());
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var herbId = createResponse.Data!.Id;

        var response = await HerbApi.ToggleStatusAsync(herbId);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Toggled herb {herbId} status");
    }

    #endregion

    #region Soft Delete & Restore

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "HerbManagement")]
    [Trait("Role", "Admin")]
    public async Task DeleteAndRestore_Herb_CompletesSuccessfully()
    {
        await LoginAsSysadminAsync();
        var createResponse = await HerbApi.CreateHerbAsync(CreateTestHerbInput());
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var herbId = createResponse.Data!.Id;

        var deleteResponse = await HerbApi.DeleteHerbAsync(herbId);
        deleteResponse.Success.Should().BeTrue(deleteResponse.Message);
        _output.WriteLine($"Deleted herb {herbId}");

        var restoreResponse = await HerbApi.RestoreAsync(herbId);
        restoreResponse.Success.Should().BeTrue(restoreResponse.Message);
        _output.WriteLine($"Restored herb {herbId}");

        var getResponse = await HerbApi.GetHerbByIdAsync(herbId);
        getResponse.Success.Should().BeTrue(getResponse.Message);
    }

    #endregion

    #region Batch Operations

    private async Task<HerbDetailDto> CreateTestHerbAsync(string suffix = "")
    {
        var input = CreateTestHerbInput(suffix);
        var response = await HerbApi.CreateHerbAsync(input);
        response.Success.Should().BeTrue(response.Message);
        DataTracker.Track(EntityType.Herb, response.Data!.Id);
        return response.Data!;
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "HerbManagement")]
    [Trait("Role", "Admin")]
    public async Task BatchDelete_MultipleHerbs_ReturnsOperationResult()
    {
        await LoginAsSysadminAsync();
        var h1 = await HerbApi.CreateHerbAsync(CreateTestHerbInput("b1"));
        var h2 = await HerbApi.CreateHerbAsync(CreateTestHerbInput("b2"));
        h1.Success.Should().BeTrue(h1.Message);
        h2.Success.Should().BeTrue(h2.Message);

        var batchInput = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { h1.Data!.Id, h2.Data!.Id }
        };

        var response = await HerbApi.BatchDeleteAsync(batchInput);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.SuccessCount.Should().Be(2);
        _output.WriteLine($"Batch deleted: {response.Data.SuccessCount}/{response.Data.TotalCount}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "HerbManagement")]
    [Trait("Role", "Admin")]
    public async Task BatchEnable_MultipleHerbs_ReturnsOperationResult()
    {
        await LoginAsSysadminAsync();
        var herb1 = await CreateTestHerbAsync("be1");
        var herb2 = await CreateTestHerbAsync("be2");

        var batchInput = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { herb1.Id, herb2.Id }
        };

        var response = await HerbApi.BatchEnableAsync(batchInput);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Batch enabled: {response.Data!.SuccessCount}/{response.Data.TotalCount}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "HerbManagement")]
    [Trait("Role", "Admin")]
    public async Task BatchDisable_MultipleHerbs_ReturnsOperationResult()
    {
        await LoginAsSysadminAsync();
        var herb1 = await CreateTestHerbAsync("bd1");
        var herb2 = await CreateTestHerbAsync("bd2");

        var batchInput = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { herb1.Id, herb2.Id }
        };

        var response = await HerbApi.BatchDisableAsync(batchInput);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Batch disabled: {response.Data!.SuccessCount}/{response.Data.TotalCount}");
    }

    #endregion

    #region Search

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "HerbManagement")]
    [Trait("Role", "Admin")]
    public async Task GetHerbs_WithKeyword_FiltersResults()
    {
        await LoginAsSysadminAsync();
        var uniqueName = $"搜索药材_{Guid.NewGuid():N}".Substring(0, 15);
        var input = CreateTestHerbInput();
        input.Name = uniqueName;
        var createResponse = await HerbApi.CreateHerbAsync(input);
        createResponse.Success.Should().BeTrue(createResponse.Message);

        var response = await HerbApi.GetHerbsAsync(1, 10, uniqueName);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().Contain(h => h.Name == uniqueName);
        _output.WriteLine($"Search for '{uniqueName}': found {response.Data.Items.Count}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "HerbManagement")]
    [Trait("Role", "Admin")]
    public async Task GetHerbs_WithCategory_FiltersResults()
    {
        await LoginAsSysadminAsync();
        var input = CreateTestHerbInput();
        input.Category = "补虚药";
        var createResponse = await HerbApi.CreateHerbAsync(input);
        createResponse.Success.Should().BeTrue(createResponse.Message);

        var response = await HerbApi.GetHerbsAsync(1, 10, category: "补虚药");

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().NotBeEmpty();
        _output.WriteLine($"Category filter '补虚药': found {response.Data.Items.Count}");
    }

    #endregion

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "HerbManagement")]
    [Trait("Role", "Admin")]
    public async Task ExportTemplate_ReturnsFileResponse()
    {
        await LoginAsSysadminAsync();

        var response = await HerbApi.ExportTemplateAsync();

        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Should().NotBeNull();
        _output.WriteLine($"Export template: {response.Content!.Headers.ContentType}");
    }
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "HerbManagement")]
    [Trait("Role", "Admin")]
    public async Task ExportHerbs_WithKeyword_ReturnsFileResponse()
    {
        await LoginAsSysadminAsync();
        await CreateTestHerbAsync("export_test");

        var response = await HerbApi.ExportHerbsAsync("export_test");

        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Should().NotBeNull();
        _output.WriteLine($"Export herbs: {response.Content!.Headers.ContentType}");
    }

    #region Full Lifecycle

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "HerbManagement")]
    [Trait("Role", "Admin")]
    public async Task HerbFullLifecycle_CreateUpdateToggleDeleteRestore_AllSucceed()
    {
        await LoginAsSysadminAsync();

        // Step 1: Create
        var input = CreateTestHerbInput("lc");
        var createResponse = await HerbApi.CreateHerbAsync(input);
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var herbId = createResponse.Data!.Id;
        _output.WriteLine($"[Lifecycle] Created: {herbId}");

        // Step 2: Read
        var getResponse = await HerbApi.GetHerbByIdAsync(herbId);
        getResponse.Success.Should().BeTrue(getResponse.Message);
        getResponse.Data!.Name.Should().Be(input.Name);

        // Step 3: Update
        var updateInput = CreateTestHerbInput("lc_upd");
        var response = await HerbApi.UpdateHerbAsync(herbId, updateInput);
        response.Success.Should().BeTrue(response.Message);
        response.Data!.Name.Should().Be(updateInput.Name);
        _output.WriteLine($"[Lifecycle] Updated: {response.Data.Name}");

        // Step 4: Toggle status
        var toggleResponse = await HerbApi.ToggleStatusAsync(herbId);
        toggleResponse.Success.Should().BeTrue(toggleResponse.Message);
        _output.WriteLine("[Lifecycle] Status toggled");

        // Step 5: Soft delete
        var deleteResponse = await HerbApi.DeleteHerbAsync(herbId);
        deleteResponse.Success.Should().BeTrue(deleteResponse.Message);
        _output.WriteLine("[Lifecycle] Deleted");

        // Step 6: Restore
        var restoreResponse = await HerbApi.RestoreAsync(herbId);
        restoreResponse.Success.Should().BeTrue(restoreResponse.Message);
        _output.WriteLine("[Lifecycle] Restored");

        // Step 7: Verify accessible after restore
        var finalGet = await HerbApi.GetHerbByIdAsync(herbId);
        finalGet.Success.Should().BeTrue(finalGet.Message);
        _output.WriteLine($"[Lifecycle] Final verification OK: {finalGet.Data!.Id}");
    }

    #endregion
}
