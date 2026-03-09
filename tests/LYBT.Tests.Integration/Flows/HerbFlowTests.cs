using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Herbs.Repositories;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Integration.Flows;

/// <summary>
/// Herb CRUD flow integration tests.
/// Tests the full chain: HerbRepository -> IHerbApi -> Server HerbController -> SQL Server.
/// </summary>
[Collection("Integration")]
public class HerbFlowTests : IntegrationTestBase
{
    public HerbFlowTests(IntegrationFixture fixture) : base(fixture) { }

    private async Task<HerbRepository> CreateRepositoryAsync()
    {
        var (_, api) = await LoginAsAdminWithApiAsync<IHerbApi>();
        return new HerbRepository(api, NullLogger<HerbRepository>.Instance);
    }

    [Fact]
    public async Task CreateAndRetrieve_Herb_Succeeds()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var input = new HerbInputDto
        {
            Name = "黄芪",
            PinYinCode = "HQ",
            Category = "补气药",
            Unit = "克",
            Price = 5.00m
        };

        // Act
        var created = await ds.CreateAsync(input);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);
        created.Name.Should().Be("黄芪");

        // Retrieve
        var retrieved = await ds.GetByIdAsync(created.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("黄芪");
        retrieved.Category.Should().Be("补气药");
    }

    [Fact]
    public async Task ToggleStatus_DisablesAndEnables()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var created = await ds.CreateAsync(new HerbInputDto
        {
            Name = "当归状态",
            Unit = "克",
            Price = 8.00m
        });

        // Act - toggle to disabled
        var firstToggle = await ds.ToggleStatusAsync(created.Id);

        // Assert
        firstToggle.Should().NotBeNull();

        // Act - toggle back to enabled
        var secondToggle = await ds.ToggleStatusAsync(created.Id);
        secondToggle.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteHerb_SoftDeletes()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var created = await ds.CreateAsync(new HerbInputDto
        {
            Name = "删除测试药材",
            Unit = "克",
            Price = 3.00m
        });

        // Act
        var deleted = await ds.DeleteAsync(created.Id);

        // Assert
        deleted.Should().BeTrue();

        // Verify soft-deleted: server returns 404, Refit throws ApiException
        var act = () => ds.GetByIdAsync(created.Id);
        await act.Should().ThrowAsync<Refit.ApiException>();
    }

    [Fact]
    public async Task GetPaged_ReturnsHerbList()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        await ds.CreateAsync(new HerbInputDto { Name = "分页药材A", Unit = "克", Price = 1.00m });
        await ds.CreateAsync(new HerbInputDto { Name = "分页药材B", Unit = "克", Price = 2.00m });

        // Act
        var result = await ds.GetPagedAsync(1, 20);

        // Assert
        result.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        result.Items.Should().NotBeEmpty();
    }
}
