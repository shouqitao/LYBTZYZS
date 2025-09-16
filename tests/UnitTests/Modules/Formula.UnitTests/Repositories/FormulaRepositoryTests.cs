using FluentAssertions;
using LYBT.Module.Formula.Repositories;
using LYBT.Module.Formula.Tests.Base;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Module.Formula.Tests.Repositories;

/// <summary>
/// FormulaRepository 单元测试
/// 测试验方仓储的完整功能，包括缓存优化和业务方法
/// </summary>
public class FormulaRepositoryTests : RepositoryTestBase
{
    private readonly FormulaRepository _repository;
    private readonly IMemoryCache _cache;

    public FormulaRepositoryTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _repository = new FormulaRepository(Context, NullLogger<FormulaRepository>.Instance, _cache);
    }

    #region 基础CRUD操作测试

    [Fact]
    public async Task AddAsync_ShouldCreateFormula_WhenValidEntity()
    {
        // Arrange
        var formula = FormulaTestDataGenerator.CreateTestFormula();

        // Act
        var result = await _repository.AddAsync(formula);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(formula.Id);
        
        var saved = await _repository.GetByIdAsync(formula.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be(formula.Name);
        saved.Description.Should().Be(formula.Description);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFormula_WhenExists()
    {
        // Arrange
        var formula = FormulaTestDataGenerator.CreateTestFormula();
        await _repository.AddAsync(formula);

        // Act
        var result = await _repository.GetByIdAsync(formula.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(formula.Id);
        result.Name.Should().Be(formula.Name);
        result.Classification.Should().Be(formula.Classification);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyFormula_WhenValidChanges()
    {
        // Arrange
        var formula = FormulaTestDataGenerator.CreateTestFormula();
        await _repository.AddAsync(formula);

        var newName = "更新后的验方名称";
        var newStatus = CommonStatus.Disabled;
        formula.Name = newName;
        formula.Status = newStatus;

        // Act
        var result = await _repository.UpdateAsync(formula);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(newName);
        result.Status.Should().Be(newStatus);

        var updated = await _repository.GetByIdAsync(formula.Id);
        updated!.Name.Should().Be(newName);
        updated.Status.Should().Be(newStatus);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFormula_WhenExists()
    {
        // Arrange
        var formula = FormulaTestDataGenerator.CreateTestFormula();
        await _repository.AddAsync(formula);

        // Act
        var result = await _repository.DeleteAsync(formula.Id);

        // Assert
        result.Should().BeTrue();

        var deleted = await _repository.GetByIdAsync(formula.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region 列表查询测试

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllFormulas_WhenDataExists()
    {
        // Arrange
        var formulas = FormulaTestDataGenerator.CreateTestFormulas(5);
        foreach (var formula in formulas)
        {
            await _repository.AddAsync(formula);
        }

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmpty_WhenNoData()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllStatuses_WhenMixedData()
    {
        // Arrange
        var enabledFormula = FormulaTestDataGenerator.CreateTestFormula(status: CommonStatus.Enabled);
        var disabledFormula = FormulaTestDataGenerator.CreateTestFormula(status: CommonStatus.Disabled);
        
        await _repository.AddAsync(enabledFormula);
        await _repository.AddAsync(disabledFormula);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(f => f.Status == CommonStatus.Enabled);
        result.Should().Contain(f => f.Status == CommonStatus.Disabled);
    }

    #endregion

    #region 业务查询方法测试

    [Fact]
    public async Task GetTemplatesAsync_ShouldReturnOnlyEnabledFormulas_WhenMixedStatuses()
    {
        // Arrange
        var enabledFormula1 = FormulaTestDataGenerator.CreateTestFormula(status: CommonStatus.Enabled);
        var enabledFormula2 = FormulaTestDataGenerator.CreateTestFormula(status: CommonStatus.Enabled);
        var disabledFormula = FormulaTestDataGenerator.CreateTestFormula(status: CommonStatus.Disabled);
        
        await _repository.AddAsync(enabledFormula1);
        await _repository.AddAsync(enabledFormula2);
        await _repository.AddAsync(disabledFormula);

        // Act
        var result = await _repository.GetTemplatesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(f => f.Status.Should().Be(CommonStatus.Enabled));
        result.Should().Contain(f => f.Id == enabledFormula1.Id);
        result.Should().Contain(f => f.Id == enabledFormula2.Id);
        result.Should().NotContain(f => f.Id == disabledFormula.Id);
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldReturnEmpty_WhenNoEnabledFormulas()
    {
        // Arrange
        var disabledFormula1 = FormulaTestDataGenerator.CreateTestFormula(status: CommonStatus.Disabled);
        var disabledFormula2 = FormulaTestDataGenerator.CreateTestFormula(status: CommonStatus.Disabled);
        
        await _repository.AddAsync(disabledFormula1);
        await _repository.AddAsync(disabledFormula2);

        // Act
        var result = await _repository.GetTemplatesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldReturnAllEnabled_WhenOnlyEnabledFormulas()
    {
        // Arrange
        var formulas = FormulaTestDataGenerator.CreateTestFormulasWithStatus(CommonStatus.Enabled, 3);
        foreach (var formula in formulas)
        {
            await _repository.AddAsync(formula);
        }

        // Act
        var result = await _repository.GetTemplatesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(f => f.Status.Should().Be(CommonStatus.Enabled));
    }

    #endregion

    #region 分页查询测试

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResults_WhenValidParameters()
    {
        // Arrange
        var formulas = FormulaTestDataGenerator.CreateTestFormulas(15);
        foreach (var formula in formulas)
        {
            await _repository.AddAsync(formula);
        }

        // Act
        var result = await _repository.GetPagedAsync(null, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(15);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldFilterByPredicate_WhenProvided()
    {
        // Arrange
        var enabledFormulas = FormulaTestDataGenerator.CreateTestFormulasWithStatus(CommonStatus.Enabled, 3);
        var disabledFormulas = FormulaTestDataGenerator.CreateTestFormulasWithStatus(CommonStatus.Disabled, 2);
        
        foreach (var formula in enabledFormulas.Concat(disabledFormulas))
        {
            await _repository.AddAsync(formula);
        }

        // Act
        var result = await _repository.GetPagedAsync(
            f => f.Status == CommonStatus.Enabled, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.Items.Should().AllSatisfy(f => f.Status.Should().Be(CommonStatus.Enabled));
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnEmptyResult_WhenNoData()
    {
        // Act
        var result = await _repository.GetPagedAsync(null, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region 缓存行为测试

    [Fact]
    public async Task GetByIdAsync_ShouldUseCache_OnSecondCall()
    {
        // Arrange
        var formula = FormulaTestDataGenerator.CreateTestFormula();
        await _repository.AddAsync(formula);

        // Act - 第一次调用，数据库查询
        var firstResult = await _repository.GetByIdAsync(formula.Id);
        
        // 修改数据库中的数据
        var directEntity = Context.Formulas.First(f => f.Id == formula.Id);
        directEntity.Description = "数据库直接修改";
        await Context.SaveChangesAsync();
        
        // 第二次调用，应该从缓存返回
        var secondResult = await _repository.GetByIdAsync(formula.Id);

        // Assert
        firstResult.Should().NotBeNull();
        secondResult.Should().NotBeNull();
        // 缓存的数据应该与第一次查询相同
        secondResult!.Description.Should().Be(firstResult!.Description);
        secondResult.Description.Should().NotBe("数据库直接修改");
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldUseCache_OnSecondCall()
    {
        // Arrange
        var formulas = FormulaTestDataGenerator.CreateTestFormulasWithStatus(CommonStatus.Enabled, 2);
        foreach (var formula in formulas)
        {
            await _repository.AddAsync(formula);
        }

        // Act - 第一次调用
        var firstResult = await _repository.GetTemplatesAsync();
        
        // 添加新的启用验方
        var newFormula = FormulaTestDataGenerator.CreateTestFormula(status: CommonStatus.Enabled);
        await _repository.AddAsync(newFormula);
        
        // 第二次调用，应该从缓存返回
        var secondResult = await _repository.GetTemplatesAsync();

        // Assert
        firstResult.Should().HaveCount(2);
        secondResult.Should().HaveCount(2); // 应该与缓存中的数量相同
    }

    [Fact]
    public async Task GetAllAsync_ShouldUseCache_OnSecondCall()
    {
        // Arrange
        var formulas = FormulaTestDataGenerator.CreateTestFormulas(3);
        foreach (var formula in formulas)
        {
            await _repository.AddAsync(formula);
        }

        // Act - 第一次调用
        var firstResult = await _repository.GetAllAsync();
        
        // 添加新验方
        var newFormula = FormulaTestDataGenerator.CreateTestFormula();
        await _repository.AddAsync(newFormula);
        
        // 第二次调用，应该从缓存返回
        var secondResult = await _repository.GetAllAsync();

        // Assert
        firstResult.Should().HaveCount(3);
        secondResult.Should().HaveCount(3); // 应该与缓存中的数量相同
    }

    [Fact]
    public async Task Cache_ShouldStoreWithCorrectKey_ForGetTemplates()
    {
        // Arrange
        var formula = FormulaTestDataGenerator.CreateTestFormula(status: CommonStatus.Enabled);
        await _repository.AddAsync(formula);

        // Act
        await _repository.GetTemplatesAsync();
        
        // Assert - 验证缓存键是否正确
        var expectedCacheKey = "Formula_templates";
        var cached = _cache.Get<List<LYBT.Entities.Formula.Formula>>(expectedCacheKey);
        cached.Should().NotBeNull();
        cached!.Should().HaveCount(1);
        cached.First().Id.Should().Be(formula.Id);
    }

    #endregion

    #region 复杂数据场景测试

    [Fact]
    public async Task Repository_ShouldHandleDifferentClassifications()
    {
        // Arrange
        var formulas = new[]
        {
            FormulaTestDataGenerator.CreateTestFormula(classification: "解表剂"),
            FormulaTestDataGenerator.CreateTestFormula(classification: "清热剂"),
            FormulaTestDataGenerator.CreateTestFormula(classification: "温里剂"),
            FormulaTestDataGenerator.CreateTestFormula(classification: "补益剂")
        };

        foreach (var formula in formulas)
        {
            await _repository.AddAsync(formula);
        }

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(4);
        result.Should().Contain(f => f.Classification == "解表剂");
        result.Should().Contain(f => f.Classification == "清热剂");
        result.Should().Contain(f => f.Classification == "温里剂");
        result.Should().Contain(f => f.Classification == "补益剂");
    }

    [Fact]
    public async Task Repository_ShouldHandleComplexFormulaData()
    {
        // Arrange
        var formula = FormulaTestDataGenerator.CreateComplexFormula();
        await _repository.AddAsync(formula);

        // Act
        var result = await _repository.GetByIdAsync(formula.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().NotBeNullOrEmpty();
        result.Description.Should().NotBeNullOrEmpty();
        result.Composition.Should().NotBeNullOrEmpty();
        result.Usage.Should().NotBeNullOrEmpty();
        result.Functions.Should().NotBeNullOrEmpty();
        result.Indications.Should().NotBeNullOrEmpty();
        result.Classification.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldHandleLargeDataset()
    {
        // Arrange - 创建大量验方，其中一半启用
        var enabledFormulas = FormulaTestDataGenerator.CreateTestFormulasWithStatus(CommonStatus.Enabled, 50);
        var disabledFormulas = FormulaTestDataGenerator.CreateTestFormulasWithStatus(CommonStatus.Disabled, 50);

        foreach (var formula in enabledFormulas.Concat(disabledFormulas))
        {
            await _repository.AddAsync(formula);
        }

        // Act
        var result = await _repository.GetTemplatesAsync();

        // Assert
        result.Should().HaveCount(50); // 只返回启用的验方
        result.Should().AllSatisfy(f => f.Status.Should().Be(CommonStatus.Enabled));
    }

    #endregion

    #region 边界条件和异常测试

    [Fact]
    public async Task AddAsync_ShouldThrowException_WhenEntityIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _repository.AddAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowException_WhenEntityIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldHandleInvalidGuid()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPagedAsync_ShouldHandleInvalidPageParameters()
    {
        // Arrange
        var formulas = FormulaTestDataGenerator.CreateTestFormulas(5);
        foreach (var formula in formulas)
        {
            await _repository.AddAsync(formula);
        }

        // Act - 负数页面
        var negativePageResult = await _repository.GetPagedAsync(null, -1, 10);
        
        // Act - 零页面大小
        var zeroPageSizeResult = await _repository.GetPagedAsync(null, 1, 0);

        // Assert
        negativePageResult.Should().NotBeNull();
        negativePageResult.Items.Should().BeEmpty();
        
        zeroPageSizeResult.Should().NotBeNull();
        zeroPageSizeResult.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Repository_ShouldHandleMultipleConcurrentOperations()
    {
        // Arrange
        var formulas = FormulaTestDataGenerator.CreateTestFormulas(10);

        // Act - 并发添加
        var addTasks = formulas.Select(f => _repository.AddAsync(f));
        var results = await Task.WhenAll(addTasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().AllSatisfy(result => result.Should().NotBeNull());

        var allFormulas = await _repository.GetAllAsync();
        allFormulas.Should().HaveCount(10);
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cache?.Dispose();
        }
        base.Dispose(disposing);
    }
}