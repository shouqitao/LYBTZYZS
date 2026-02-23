using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.DataSources;
using LYBT.Desktop.LocalData.Tests.TestFixtures;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Tests.DataSources;

/// <summary>
/// LocalHerbDataSource 单元测试
/// OpenSpec: implement-local-mode Phase 5
/// </summary>
public class LocalHerbDataSourceTests : IClassFixture<LocalDbContextFixture>
{
    private readonly LocalDbContextFixture _fixture;
    private readonly ILogger<LocalHerbDataSource> _logger;

    public LocalHerbDataSourceTests(LocalDbContextFixture fixture)
    {
        _fixture = fixture;
        _logger = LocalDbContextFixture.CreateLogger<LocalHerbDataSource>();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingHerb_ReturnsHerb()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var herb = CreateTestHerb("黄芪", "补气药");
        context.Herbs.Add(herb);
        await context.SaveChangesAsync();

        var dataSource = new LocalHerbDataSource(context, _logger);

        // Act
        var result = await dataSource.GetByIdAsync(herb.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("黄芪");
        result.Category.Should().Be("补气药");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingHerb_ReturnsNull()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalHerbDataSource(context, _logger);

        // Act
        var result = await dataSource.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_WithCategory_FiltersResults()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Herbs.Add(CreateTestHerb("黄芪", "补气药"));
        context.Herbs.Add(CreateTestHerb("当归", "补血药"));
        context.Herbs.Add(CreateTestHerb("人参", "补气药"));
        await context.SaveChangesAsync();

        var dataSource = new LocalHerbDataSource(context, _logger);

        // Act
        var (items, total) = await dataSource.GetPagedAsync(
            page: 1, pageSize: 10, keyword: null, category: "补气药");

        // Assert
        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().OnlyContain(h => h.Category == "补气药");
    }

    [Fact]
    public async Task GetPagedAsync_WithKeyword_FiltersResults()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Herbs.Add(CreateTestHerb("黄芪", pinYinCode: "HQ"));
        context.Herbs.Add(CreateTestHerb("黄连", pinYinCode: "HL"));
        context.Herbs.Add(CreateTestHerb("当归", pinYinCode: "DG"));
        await context.SaveChangesAsync();

        var dataSource = new LocalHerbDataSource(context, _logger);

        // Act
        var (items, total) = await dataSource.GetPagedAsync(
            page: 1, pageSize: 10, keyword: "黄");

        // Assert
        total.Should().Be(2);
        items.Should().OnlyContain(h => h.Name.Contains("黄"));
    }

    [Fact]
    public async Task GetPagedAsync_OrdersByName()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Herbs.Add(CreateTestHerb("当归"));
        context.Herbs.Add(CreateTestHerb("阿胶"));
        context.Herbs.Add(CreateTestHerb("白术"));
        await context.SaveChangesAsync();

        var dataSource = new LocalHerbDataSource(context, _logger);

        // Act
        var (items, _) = await dataSource.GetPagedAsync(page: 1, pageSize: 10);

        // Assert
        items.Should().BeInAscendingOrder(h => h.Name);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidHerb_ReturnsCreatedHerb()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalHerbDataSource(context, _logger);
        var input = new HerbInputDto
        {
            Name = "新药材",
            Category = "清热药",
            Price = 25.00m
        };

        // Act
        var result = await dataSource.CreateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Name.Should().Be("新药材");
    }

    #endregion

    #region ToggleStatusAsync Tests

    [Fact]
    public async Task ToggleStatusAsync_EnabledHerb_DisablesIt()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var herb = CreateTestHerb("待禁用");
        herb.Status = CommonStatus.Enabled;
        context.Herbs.Add(herb);
        await context.SaveChangesAsync();

        var dataSource = new LocalHerbDataSource(context, _logger);

        // Act
        var result = await dataSource.ToggleStatusAsync(herb.Id);

        // Assert
        result.Should().BeTrue();
        var updated = await context.Herbs.FindAsync(herb.Id);
        updated!.Status.Should().Be(CommonStatus.Disabled);
    }

    [Fact]
    public async Task ToggleStatusAsync_DisabledHerb_EnablesIt()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var herb = CreateTestHerb("待启用");
        herb.Status = CommonStatus.Disabled;
        context.Herbs.Add(herb);
        await context.SaveChangesAsync();

        var dataSource = new LocalHerbDataSource(context, _logger);

        // Act
        var result = await dataSource.ToggleStatusAsync(herb.Id);

        // Assert
        result.Should().BeTrue();
        var updated = await context.Herbs.FindAsync(herb.Id);
        updated!.Status.Should().Be(CommonStatus.Enabled);
    }

    [Fact]
    public async Task ToggleStatusAsync_NonExistingHerb_ReturnsFalse()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalHerbDataSource(context, _logger);

        // Act
        var result = await dataSource.ToggleStatusAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region DeleteAsync and RestoreAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingHerb_SoftDeletes()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var herb = CreateTestHerb("待删除药材");
        context.Herbs.Add(herb);
        await context.SaveChangesAsync();

        var dataSource = new LocalHerbDataSource(context, _logger);

        // Act
        var result = await dataSource.DeleteAsync(herb.Id);

        // Assert
        result.Should().BeTrue();
        var deleted = await context.Herbs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.Id == herb.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreAsync_DeletedHerb_RestoresSuccessfully()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var herb = CreateTestHerb("已删除待恢复");
        herb.IsDeleted = true;
        context.Herbs.Add(herb);
        await context.SaveChangesAsync();

        var dataSource = new LocalHerbDataSource(context, _logger);

        // Act
        var result = await dataSource.RestoreAsync(herb.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("已删除待恢复");

        // Verify entity is no longer soft-deleted
        var restored = await context.Herbs.FindAsync(herb.Id);
        restored!.IsDeleted.Should().BeFalse();
    }

    #endregion

    #region GetCategoriesAsync Tests

    [Fact]
    public async Task GetCategoriesAsync_ReturnsDistinctCategories()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Herbs.Add(CreateTestHerb("黄芪", "补气药"));
        context.Herbs.Add(CreateTestHerb("人参", "补气药"));
        context.Herbs.Add(CreateTestHerb("当归", "补血药"));
        context.Herbs.Add(CreateTestHerb("黄连", "清热药"));
        await context.SaveChangesAsync();

        var dataSource = new LocalHerbDataSource(context, _logger);

        // Act
        var result = await dataSource.GetCategoriesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("补气药");
        result.Should().Contain("补血药");
        result.Should().Contain("清热药");
        result.Should().BeInAscendingOrder(); // 验证排序
    }

    #endregion

    #region Helper Methods

    private static Herb CreateTestHerb(
        string name,
        string? category = null,
        string? pinYinCode = null)
    {
        return new Herb
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = category ?? "默认分类",
            PinYinCode = pinYinCode ?? name.ToUpper(),
            Price = 10.00m,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    #endregion
}
