using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.DataSources;
using LYBT.Desktop.LocalData.Tests.TestFixtures;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Enums;
using FormulaEntity = LYBT.Entities.Formulas.Formula;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Tests.DataSources;

/// <summary>
/// LocalFormulaDataSource 单元测试
/// Phase 4.3: Desktop LocalData Tests
/// </summary>
public class LocalFormulaDataSourceTests : IClassFixture<LocalDbContextFixture>
{
    private readonly LocalDbContextFixture _fixture;
    private readonly ILogger<LocalFormulaDataSource> _logger;

    public LocalFormulaDataSourceTests(LocalDbContextFixture fixture)
    {
        _fixture = fixture;
        _logger = LocalDbContextFixture.CreateLogger<LocalFormulaDataSource>();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingFormula_ReturnsFormula()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var formula = CreateTestFormula("四君子汤", "补气健脾");
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();

        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var result = await dataSource.GetByIdAsync(formula.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("四君子汤");
        result.Effect.Should().Be("补气健脾");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingFormula_ReturnsNull()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var result = await dataSource.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetWithHerbsAsync Tests

    [Fact]
    public async Task GetWithHerbsAsync_ExistingFormula_ReturnsFormulaWithHerbs()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var formula = CreateTestFormula("六味地黄丸", "滋阴补肾");
        formula.Herbs.Add(CreateTestHerbItem("熟地黄", 24));
        formula.Herbs.Add(CreateTestHerbItem("山茱萸", 12));
        formula.Herbs.Add(CreateTestHerbItem("山药", 12));
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();

        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var result = await dataSource.GetWithHerbsAsync(formula.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("六味地黄丸");
        result.Herbs.Should().HaveCount(3);
        result.Herbs.Should().Contain(h => h.HerbName == "熟地黄" && h.Dosage == 24);
    }

    [Fact]
    public async Task GetWithHerbsAsync_NonExistingFormula_ReturnsNull()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var result = await dataSource.GetWithHerbsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_WithKeyword_FiltersByName()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Formulas.Add(CreateTestFormula("四君子汤", "补气"));
        context.Formulas.Add(CreateTestFormula("四物汤", "补血"));
        context.Formulas.Add(CreateTestFormula("六味地黄丸", "补肾"));
        await context.SaveChangesAsync();

        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var (items, total) = await dataSource.GetPagedAsync(
            page: 1, pageSize: 10, keyword: "四");

        // Assert
        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().OnlyContain(f => f.Name.Contains("四"));
    }

    [Fact]
    public async Task GetPagedAsync_WithKeyword_FiltersByEffect()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Formulas.Add(CreateTestFormula("四君子汤", "补气健脾"));
        context.Formulas.Add(CreateTestFormula("补中益气汤", "补气升阳"));
        context.Formulas.Add(CreateTestFormula("四物汤", "补血调经"));
        await context.SaveChangesAsync();

        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var (items, total) = await dataSource.GetPagedAsync(
            page: 1, pageSize: 10, keyword: "补气");

        // Assert
        total.Should().Be(2);
        items.Should().OnlyContain(f => f.Effect!.Contains("补气"));
    }

    [Fact]
    public async Task GetPagedAsync_WithCategory_FiltersResults()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Formulas.Add(CreateTestFormula("四君子汤", category: "补益剂"));
        context.Formulas.Add(CreateTestFormula("银翘散", category: "解表剂"));
        context.Formulas.Add(CreateTestFormula("补中益气汤", category: "补益剂"));
        await context.SaveChangesAsync();

        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var (items, total) = await dataSource.GetPagedAsync(
            page: 1, pageSize: 10, keyword: null, category: "补益剂");

        // Assert
        total.Should().Be(2);
        items.Should().OnlyContain(f => f.Category == "补益剂");
    }

    [Fact]
    public async Task GetPagedAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        for (int i = 1; i <= 5; i++)
        {
            context.Formulas.Add(CreateTestFormula($"验方{i}"));
        }
        await context.SaveChangesAsync();

        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var (items, total) = await dataSource.GetPagedAsync(page: 2, pageSize: 2);

        // Assert
        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0);

        // 分别添加并保存，确保 CreatedAt 正确设置
        var formula1 = CreateTestFormula("第一方");
        formula1.CreatedAt = baseTime.AddDays(-2);
        context.Formulas.Add(formula1);

        var formula2 = CreateTestFormula("第二方");
        formula2.CreatedAt = baseTime.AddDays(-1);
        context.Formulas.Add(formula2);

        var formula3 = CreateTestFormula("第三方");
        formula3.CreatedAt = baseTime;
        context.Formulas.Add(formula3);

        await context.SaveChangesAsync();

        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var (items, _) = await dataSource.GetPagedAsync(page: 1, pageSize: 10);

        // Assert - 验证降序排列（最新的在前）
        items.Should().HaveCount(3);
        var sortedItems = items.OrderByDescending(f => f.CreatedAt).ToList();
        items.Select(f => f.Name).Should().ContainInOrder(sortedItems.Select(f => f.Name));
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidFormula_ReturnsCreatedFormula()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalFormulaDataSource(context, _logger);
        var formula = new FormulaEntity
        {
            Name = "新验方",
            Effect = "新功效",
            Category = "新分类"
        };

        // Act
        var result = await dataSource.CreateAsync(formula);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Name.Should().Be("新验方");

        // Verify persisted
        var persisted = await context.Formulas.FindAsync(result.Id);
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithHerbs_CreatesRelationships()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalFormulaDataSource(context, _logger);
        var formula = new FormulaEntity
        {
            Name = "带药材验方",
            Herbs = new List<FormulaHerbItem>
            {
                new() { HerbName = "黄芪", Dosage = 15 },
                new() { HerbName = "党参", Dosage = 10 }
            }
        };

        // Act
        var result = await dataSource.CreateAsync(formula);

        // Assert
        result.Herbs.Should().HaveCount(2);
        result.Herbs.Should().OnlyContain(h => h.FormulaId == result.Id);
        result.Herbs.Should().OnlyContain(h => h.Id != Guid.Empty);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingFormula_UpdatesProperties()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var formula = CreateTestFormula("原名称", "原功效");
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();

        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        formula.Name = "新名称";
        formula.Effect = "新功效";
        var result = await dataSource.UpdateAsync(formula);

        // Assert
        result.Name.Should().Be("新名称");
        result.Effect.Should().Be("新功效");
    }

    [Fact]
    public async Task UpdateAsync_WithHerbs_ReplacesHerbs()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var originalFormula = CreateTestFormula("验方");
        originalFormula.Herbs.Add(CreateTestHerbItem("旧药材", 10));
        context.Formulas.Add(originalFormula);
        await context.SaveChangesAsync();
        var formulaId = originalFormula.Id;

        // Detach 原实体避免追踪冲突
        context.Entry(originalFormula).State = EntityState.Detached;
        foreach (var herb in originalFormula.Herbs)
        {
            context.Entry(herb).State = EntityState.Detached;
        }

        var dataSource = new LocalFormulaDataSource(context, _logger);

        // 创建新实例用于更新
        var updateFormula = new FormulaEntity
        {
            Id = formulaId,
            Name = "验方",
            Effect = "默认功效",
            Category = "默认分类",
            FormulaType = FormulaType.Experience,
            Status = CommonStatus.Enabled,
            Herbs = new List<FormulaHerbItem>
            {
                new() { HerbName = "新药材1", Dosage = 15 },
                new() { HerbName = "新药材2", Dosage = 20 }
            }
        };

        // Act
        await dataSource.UpdateAsync(updateFormula);

        // Assert - 重新查询以验证
        var updated = await context.Formulas
            .AsNoTracking()
            .Include(f => f.Herbs)
            .FirstOrDefaultAsync(f => f.Id == formulaId);

        updated!.Herbs.Should().HaveCount(2);
        updated.Herbs.Should().Contain(h => h.HerbName == "新药材1");
        updated.Herbs.Should().Contain(h => h.HerbName == "新药材2");
        updated.Herbs.Should().NotContain(h => h.HerbName == "旧药材");
    }

    [Fact]
    public async Task UpdateAsync_NonExistingFormula_ThrowsException()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalFormulaDataSource(context, _logger);
        var formula = new FormulaEntity { Id = Guid.NewGuid(), Name = "不存在的验方" };

        // Act
        var act = () => dataSource.UpdateAsync(formula);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*验方不存在*");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingFormula_SoftDeletes()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var formula = CreateTestFormula("待删除验方");
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();

        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var result = await dataSource.DeleteAsync(formula.Id);

        // Assert
        result.Should().BeTrue();
        var deleted = await context.Formulas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == formula.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingFormula_ReturnsFalse()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var result = await dataSource.DeleteAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CloneAsync Tests

    [Fact]
    public async Task CloneAsync_ExistingFormula_CreatesClone()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var formula = CreateTestFormula("原始验方", "原始功效");
        formula.Category = "补益剂";
        formula.Herbs.Add(CreateTestHerbItem("黄芪", 15));
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();

        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var clone = await dataSource.CloneAsync(formula.Id);

        // Assert
        clone.Should().NotBeNull();
        clone!.Id.Should().NotBe(formula.Id);
        clone.Name.Should().Be("原始验方 (副本)");
        clone.Effect.Should().Be("原始功效");
        clone.Category.Should().Be("补益剂");
        clone.Herbs.Should().HaveCount(1);
        clone.Herbs.First().HerbName.Should().Be("黄芪");
    }

    [Fact]
    public async Task CloneAsync_NonExistingFormula_ReturnsNull()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var result = await dataSource.CloneAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ToggleStatusAsync Tests

    [Fact]
    public async Task ToggleStatusAsync_EnabledFormula_DisablesIt()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var formula = CreateTestFormula("待禁用验方");
        formula.Status = CommonStatus.Enabled;
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();

        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var result = await dataSource.ToggleStatusAsync(formula.Id);

        // Assert
        result.Should().BeTrue();
        var updated = await context.Formulas.FindAsync(formula.Id);
        updated!.Status.Should().Be(CommonStatus.Disabled);
    }

    [Fact]
    public async Task ToggleStatusAsync_DisabledFormula_EnablesIt()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var formula = CreateTestFormula("待启用验方");
        formula.Status = CommonStatus.Disabled;
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();

        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var result = await dataSource.ToggleStatusAsync(formula.Id);

        // Assert
        result.Should().BeTrue();
        var updated = await context.Formulas.FindAsync(formula.Id);
        updated!.Status.Should().Be(CommonStatus.Enabled);
    }

    [Fact]
    public async Task ToggleStatusAsync_NonExistingFormula_ReturnsFalse()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var result = await dataSource.ToggleStatusAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region RestoreAsync Tests

    [Fact]
    public async Task RestoreAsync_DeletedFormula_RestoresSuccessfully()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var formula = CreateTestFormula("已删除待恢复");
        formula.IsDeleted = true;
        context.Formulas.Add(formula);
        await context.SaveChangesAsync();

        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var result = await dataSource.RestoreAsync(formula.Id);

        // Assert
        result.Should().NotBeNull();
        result!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task RestoreAsync_NonExistingFormula_ReturnsNull()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var dataSource = new LocalFormulaDataSource(context, _logger);

        // Act
        var result = await dataSource.RestoreAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private static FormulaEntity CreateTestFormula(
        string name,
        string? effect = null,
        string? category = null)
    {
        return new FormulaEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Effect = effect ?? "默认功效",
            Category = category ?? "默认分类",
            FormulaType = FormulaType.Experience,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    private static FormulaHerbItem CreateTestHerbItem(string herbName, int dosage)
    {
        return new FormulaHerbItem
        {
            Id = Guid.NewGuid(),
            HerbName = herbName,
            Dosage = dosage,
            Unit = "g"
        };
    }

    #endregion
}
