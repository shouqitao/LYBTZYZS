using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Formula.Repositories;
using LYBT.Desktop.Herbs.Repositories;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Tests.Integration._Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Integration.Flows;

/// <summary>
/// Formula CRUD flow integration tests.
/// Tests the full chain: FormulaRepository -> IFormulaApi -> Server FormulaController -> SQL Server.
/// </summary>
[Collection("Integration")]
public class FormulaFlowTests : IntegrationTestBase
{
    public FormulaFlowTests(IntegrationFixture fixture) : base(fixture) { }

    private async Task<(FormulaRepository FormulaDs, HerbRepository HerbDs)> CreateDataSourcesAsync()
    {
        var (client, formulaApi) = await LoginAsAdminWithApiAsync<IFormulaApi>();
        var herbApi = Fixture.CreateApi<IHerbApi>(client);
        return (
            new FormulaRepository(formulaApi, null!, new RemoteOnlyApiRouter(), NullLogger<FormulaRepository>.Instance),
            new HerbRepository(herbApi, null!, new RemoteOnlyApiRouter(), NullLogger<HerbRepository>.Instance)
        );
    }

    private async Task<Guid> CreateTestHerbAsync(HerbRepository herbDs, string name = "测试药材")
    {
        var herb = await herbDs.CreateAsync(new HerbInputDto
        {
            Name = name,
            Unit = "克",
            Price = 5.00m
        });
        return herb.Id;
    }

    [Fact]
    public async Task CreateAndRetrieve_FormulaWithHerbs_Succeeds()
    {
        // Arrange
        var (formulaDs, herbDs) = await CreateDataSourcesAsync();
        var herbId = await CreateTestHerbAsync(herbDs, "黄芪验方测试");

        var input = new FormulaInputDto
        {
            Name = "四君子汤",
            Effect = "补气健脾",
            Usage = "水煎服",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbId = herbId, HerbName = "黄芪验方测试", Dosage = 15, Unit = "克" }
            }
        };

        // Act
        var created = await formulaDs.CreateAsync(input);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);
        created.Name.Should().Be("四君子汤");

        // Retrieve with herbs
        var detail = await formulaDs.GetByIdAsync(created.Id);
        detail.Should().NotBeNull();
        detail!.Name.Should().Be("四君子汤");
    }

    [Fact]
    public async Task ToggleStatus_Formula_Succeeds()
    {
        // Arrange
        var (formulaDs, herbDs) = await CreateDataSourcesAsync();
        var herbId = await CreateTestHerbAsync(herbDs, "状态切换药材");

        var created = await formulaDs.CreateAsync(new FormulaInputDto
        {
            Name = "状态切换验方",
            Effect = "测试效果",
            Usage = "口服",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbId = herbId, HerbName = "状态切换药材", Dosage = 10, Unit = "克" }
            }
        });

        // Act - toggle status
        var toggled = await formulaDs.ToggleStatusAsync(created.Id);

        // Assert
        toggled.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteFormula_SoftDeletes()
    {
        // Arrange
        var (formulaDs, herbDs) = await CreateDataSourcesAsync();
        var herbId = await CreateTestHerbAsync(herbDs, "删除验方药材");

        var created = await formulaDs.CreateAsync(new FormulaInputDto
        {
            Name = "待删除验方",
            Effect = "测试",
            Usage = "测试",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbId = herbId, HerbName = "删除验方药材", Dosage = 10, Unit = "克" }
            }
        });

        // Act
        var deleted = await formulaDs.DeleteAsync(created.Id);

        // Assert
        deleted.Should().BeTrue();

        // Verify soft-deleted: server returns 404, Refit throws ApiException
        var act = () => formulaDs.GetByIdAsync(created.Id);
        await act.Should().ThrowAsync<Refit.ApiException>();
    }

    [Fact]
    public async Task GetPaged_ReturnsFormulaList()
    {
        // Arrange
        var (formulaDs, herbDs) = await CreateDataSourcesAsync();
        var herbId = await CreateTestHerbAsync(herbDs, "分页验方药材");

        await formulaDs.CreateAsync(new FormulaInputDto
        {
            Name = "分页验方A",
            Effect = "效果A",
            Usage = "用法A",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbId = herbId, HerbName = "分页验方药材", Dosage = 10, Unit = "克" }
            }
        });

        // Act
        var result = await formulaDs.GetPagedAsync(1, 20);

        // Assert
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Should().NotBeEmpty();
    }
}
