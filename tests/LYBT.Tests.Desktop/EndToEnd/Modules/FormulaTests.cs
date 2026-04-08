using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Modules;

public class FormulaTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public FormulaTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static HerbInputDto CreateTestHerbInput(string suffix = "") => new()
    {
        Name = $"方剂用药{suffix}_{Guid.NewGuid():N}".Substring(0, 15),
        PinYinCode = "FYYY",
        Unit = "克",
        Price = 10m
    };

    private async Task<Guid> CreateTestHerbAsync()
    {
        var herbInput = CreateTestHerbInput();
        var response = await HerbApi.CreateHerbAsync(herbInput);
        response.Success.Should().BeTrue(response.Message);
        return response.Data!.Id;
    }

    private FormulaInputDto CreateTestFormulaInput(string suffix = "", Guid? herbId = null) => new()
    {
        Name = $"测试方剂{suffix}_{Guid.NewGuid():N}".Substring(0, 15),
        Effect = "清热解毒，活血化瘀",
        Usage = "水煎服，日一剂",
        Category = "清热剂",
        IsShared = false,
        Herbs = new List<FormulaHerbItemInputDto>
        {
            new()
            {
                HerbId = herbId,
                HerbName = "测试药材",
                Dosage = 10,
                Unit = "克",
                SortOrder = 0,
                DecocteMethod = DecocteMethod.Default
            }
        }
    };

    #region CRUD

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "FormulaManagement")]
    [Trait("Role", "Doctor")]
    public async Task CreateFormula_ValidInput_ReturnsCreatedFormula()
    {
        await LoginAsSysadminAsync();
        var herbId = await CreateTestHerbAsync();
        var input = CreateTestFormulaInput(herbId: herbId);

        var response = await FormulaApi.CreateFormulaAsync(input);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be(input.Name);
        _output.WriteLine($"Created formula: {response.Data.Id} - {response.Data.Name}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "FormulaManagement")]
    [Trait("Role", "Doctor")]
    public async Task GetFormulaById_ExistingFormula_ReturnsDetail()
    {
        await LoginAsSysadminAsync();
        var herbId = await CreateTestHerbAsync();
        var createResponse = await FormulaApi.CreateFormulaAsync(CreateTestFormulaInput(herbId: herbId));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var formulaId = createResponse.Data!.Id;

        var response = await FormulaApi.GetFormulaByIdAsync(formulaId);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(formulaId);
        _output.WriteLine($"Retrieved formula: {response.Data.Id}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "FormulaManagement")]
    [Trait("Role", "Doctor")]
    public async Task UpdateFormula_ValidInput_ReturnsUpdatedFormula()
    {
        await LoginAsSysadminAsync();
        var herbId = await CreateTestHerbAsync();
        var createResponse = await FormulaApi.CreateFormulaAsync(CreateTestFormulaInput(herbId: herbId));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var formulaId = createResponse.Data!.Id;

        var updateInput = CreateTestFormulaInput("upd", herbId);
        var response = await FormulaApi.UpdateFormulaAsync(formulaId, updateInput);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Name.Should().Be(updateInput.Name);
        _output.WriteLine($"Updated formula: {response.Data.Id} - {response.Data.Name}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "FormulaManagement")]
    [Trait("Role", "Doctor")]
    public async Task GetFormulas_WithPagination_ReturnsPagedResult()
    {
        await LoginAsSysadminAsync();
        var herbId = await CreateTestHerbAsync();
        await FormulaApi.CreateFormulaAsync(CreateTestFormulaInput(herbId: herbId));

        var response = await FormulaApi.GetFormulasAsync(1, 10);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().NotBeEmpty();
        response.Data.TotalCount.Should().BeGreaterThan(0);
        _output.WriteLine($"Formulas page: {response.Data.Items.Count}/{response.Data.TotalCount}");
    }

    #endregion

    #region Clone

    [Fact(Skip = "Clone endpoint not yet implemented on server")]
    [Trait("Category", "E2E")]
    [Trait("Phase", "FormulaManagement")]
    [Trait("Role", "Doctor")]
    public async Task CloneFormula_ExistingFormula_ReturnsClonedFormula()
    {
        await LoginAsSysadminAsync();
        var herbId = await CreateTestHerbAsync();
        var createResponse = await FormulaApi.CreateFormulaAsync(CreateTestFormulaInput(herbId: herbId));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var formulaId = createResponse.Data!.Id;

        var response = await FormulaApi.CloneFormulaAsync(formulaId);
        _output.WriteLine($"Cloned formula: {formulaId} -> {response.Data.Id}");
    }

    #endregion

    #region Status Toggle

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "FormulaManagement")]
    [Trait("Role", "Doctor")]
    public async Task ToggleStatus_EnabledFormula_TogglesSuccessfully()
    {
        await LoginAsSysadminAsync();
        var herbId = await CreateTestHerbAsync();
        var createResponse = await FormulaApi.CreateFormulaAsync(CreateTestFormulaInput(herbId: herbId));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var formulaId = createResponse.Data!.Id;

        var response = await FormulaApi.ToggleStatusAsync(formulaId);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Toggled formula {formulaId} status");
    }

    #endregion

    #region Soft Delete & Restore

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "FormulaManagement")]
    [Trait("Role", "Doctor")]
    public async Task DeleteAndRestore_Formula_CompletesSuccessfully()
    {
        await LoginAsSysadminAsync();
        var herbId = await CreateTestHerbAsync();
        var createResponse = await FormulaApi.CreateFormulaAsync(CreateTestFormulaInput(herbId: herbId));
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var formulaId = createResponse.Data!.Id;

        var deleteResponse = await FormulaApi.DeleteFormulaAsync(formulaId);
        deleteResponse.Success.Should().BeTrue(deleteResponse.Message);
        _output.WriteLine($"Deleted formula {formulaId}");

        var restoreResponse = await FormulaApi.RestoreAsync(formulaId);
        restoreResponse.Success.Should().BeTrue(restoreResponse.Message);
        _output.WriteLine($"Restored formula {formulaId}");

        var getResponse = await FormulaApi.GetFormulaByIdAsync(formulaId);
        getResponse.Success.Should().BeTrue(getResponse.Message);
    }

    #endregion

    #region Batch Operations

    private async Task<FormulaDetailDto> CreateTestFormulaAsync(string suffix = "")
    {
        var herbId = await CreateTestHerbAsync();
        var input = CreateTestFormulaInput(suffix, herbId);
        var response = await FormulaApi.CreateFormulaAsync(input);
        response.Success.Should().BeTrue(response.Message);
        return response.Data!;
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "FormulaManagement")]
    [Trait("Role", "Doctor")]
    public async Task BatchDelete_MultipleFormulas_ReturnsOperationResult()
    {
        await LoginAsSysadminAsync();
        var formula1 = await CreateTestFormulaAsync("bf1");
        var formula2 = await CreateTestFormulaAsync("bf2");

        var batchInput = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { formula1.Id, formula2.Id }
        };

        var response = await FormulaApi.BatchDeleteAsync(batchInput);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.SuccessCount.Should().Be(2);
        _output.WriteLine($"Batch deleted: {response.Data.SuccessCount}/{response.Data.TotalCount}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "FormulaManagement")]
    [Trait("Role", "Doctor")]
    public async Task BatchEnable_MultipleFormulas_ReturnsOperationResult()
    {
        await LoginAsSysadminAsync();
        var formula1 = await CreateTestFormulaAsync("be1");
        var formula2 = await CreateTestFormulaAsync("be2");

        var batchInput = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { formula1.Id, formula2.Id }
        };

        var response = await FormulaApi.BatchEnableAsync(batchInput);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Batch enabled: {response.Data!.SuccessCount}/{response.Data.TotalCount}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "FormulaManagement")]
    [Trait("Role", "Doctor")]
    public async Task BatchDisable_MultipleFormulas_ReturnsOperationResult()
    {
        await LoginAsSysadminAsync();
        var formula1 = await CreateTestFormulaAsync("bd1");
        var formula2 = await CreateTestFormulaAsync("bd2");

        var batchInput = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { formula1.Id, formula2.Id }
        };

        var response = await FormulaApi.BatchDisableAsync(batchInput);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "FormulaManagement")]
    [Trait("Role", "Doctor")]
    public async Task BatchImport_ValidData_ReturnsImportResult()
    {
        await LoginAsSysadminAsync();
        var herbInput = CreateTestHerbInput();
        var herbResponse = await HerbApi.CreateHerbAsync(herbInput);
        herbResponse.Success.Should().BeTrue(herbResponse.Message);

        var importData = new FormulaBatchImportInputDto
        {
            Formulas = new List<FormulaImportItemDto>
            {
                new()
                {
                    Name = $"批量导入测试配方_{Guid.NewGuid():N}",
                    Effect = "测试功效",
                    Usage = "水煎服",
                    Herbs = new List<FormulaHerbImportItemDto>
                    {
                        new() { HerbName = herbInput.Name, Dosage = 10, Unit = "克" }
                    }
                }
            }
        };

        var response = await FormulaApi.BatchImportAsync(importData);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Batch imported: {response.Data!.SuccessCount}/{response.Data.TotalCount}");
    }

    #endregion

    #region Export Operations

    [Fact(Skip = "Export/Import endpoints not yet implemented")]
    [Trait("Category", "E2E")]
    [Trait("Phase", "FormulaManagement")]
    [Trait("Role", "Doctor")]
    public async Task ExportTemplate_ReturnsFileResponse()
    {
        await LoginAsSysadminAsync();

        var response = await FormulaApi.ExportTemplateAsync();

        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Should().NotBeNull();
        _output.WriteLine($"Export template: {response.Content!.Headers.ContentType}");
    }

    [Fact(Skip = "Export/Import endpoints not yet implemented")]
    [Trait("Category", "E2E")]
    [Trait("Phase", "FormulaManagement")]
    [Trait("Role", "Doctor")]
    public async Task ExportFormulas_WithCategory_ReturnsFileResponse()
    {
        await LoginAsSysadminAsync();
        await CreateTestFormulaAsync("exp");

        var response = await FormulaApi.ExportFormulasAsync("Experience");

        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Should().NotBeNull();
        _output.WriteLine($"Export formulas: {response.Content!.Headers.ContentType}");
    }

    #endregion

    #region Search

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "FormulaManagement")]
    [Trait("Role", "Doctor")]
    public async Task GetFormulas_WithKeyword_FiltersResults()
    {
        await LoginAsSysadminAsync();
        var herbId = await CreateTestHerbAsync();
        var uniqueName = $"搜索方剂_{Guid.NewGuid():N}".Substring(0, 15);
        var input = CreateTestFormulaInput(herbId: herbId);
        input.Name = uniqueName;
        var createResponse = await FormulaApi.CreateFormulaAsync(input);
        createResponse.Success.Should().BeTrue(createResponse.Message);

        var response = await FormulaApi.GetFormulasAsync(1, 10, uniqueName);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().Contain(f => f.Name == uniqueName);
        _output.WriteLine($"Search for '{uniqueName}': found {response.Data.Items.Count}");
    }

    #endregion

    #region Full Lifecycle

    [Fact(Skip = "Clone endpoint not yet implemented on server")]
    [Trait("Category", "E2E")]
    [Trait("Phase", "FormulaManagement")]
    [Trait("Role", "Doctor")]
    public async Task FormulaFullLifecycle_CreateCloneToggleDeleteRestore_AllSucceed()
    {
        await LoginAsSysadminAsync();
        var herbId = await CreateTestHerbAsync();

        // Step 1: Create
        var input = CreateTestFormulaInput("lc", herbId);
        var createResponse = await FormulaApi.CreateFormulaAsync(input);
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var formulaId = createResponse.Data!.Id;
        _output.WriteLine($"[Lifecycle] Created: {formulaId}");

        // Step 2: Read
        var getResponse = await FormulaApi.GetFormulaByIdAsync(formulaId);
        getResponse.Success.Should().BeTrue(getResponse.Message);
        getResponse.Data!.Name.Should().Be(input.Name);

        // Step 3: Clone
        var cloneResponse = await FormulaApi.CloneFormulaAsync(formulaId);
        cloneResponse.Success.Should().BeTrue(cloneResponse.Message);
        _output.WriteLine($"[Lifecycle] Cloned: {cloneResponse.Data!.Id}");

        // Step 4: Update
        var updateInput = CreateTestFormulaInput("lc_upd", herbId);
        var updateResponse = await FormulaApi.UpdateFormulaAsync(formulaId, updateInput);
        updateResponse.Success.Should().BeTrue(updateResponse.Message);
        _output.WriteLine($"[Lifecycle] Updated: {updateResponse.Data!.Name}");

        // Step 5: Toggle status
        var toggleResponse = await FormulaApi.ToggleStatusAsync(formulaId);
        toggleResponse.Success.Should().BeTrue(toggleResponse.Message);
        _output.WriteLine("[Lifecycle] Status toggled");

        // Step 6: Soft delete
        var deleteResponse = await FormulaApi.DeleteFormulaAsync(formulaId);
        deleteResponse.Success.Should().BeTrue(deleteResponse.Message);
        _output.WriteLine("[Lifecycle] Deleted");

        // Step 7: Restore
        var restoreResponse = await FormulaApi.RestoreAsync(formulaId);
        restoreResponse.Success.Should().BeTrue(restoreResponse.Message);
        _output.WriteLine("[Lifecycle] Restored");

        // Step 8: Verify accessible after restore
        var finalGet = await FormulaApi.GetFormulaByIdAsync(formulaId);
        finalGet.Success.Should().BeTrue(finalGet.Message);
        _output.WriteLine($"[Lifecycle] Final verification OK: {finalGet.Data!.Id}");
    }

    #endregion
}
