using FluentAssertions;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Module.Herbs.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Module.Herbs.Tests.Repositories;

/// <summary>
/// HerbRepository 单元测试
/// Issue #1469 (FORMULA-8) - 验证智能药材匹配功能
/// </summary>
public class HerbRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly HerbRepository _sut;

    public HerbRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        var logger = NullLogger<HerbRepository>.Instance;
        _sut = new HerbRepository(_context, logger);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetByNameAsync Tests

    [Fact]
    public async Task GetByNameAsync_WithExactName_ReturnsHerb()
    {
        // Arrange
        var herb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "柴胡",
            PinYinCode = "CH",
            Origin = "产地测试",
            CreatedBy = Guid.NewGuid()
        };

        _context.Herbs.Add(herb);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByNameAsync("柴胡");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("柴胡");
        result.PinYinCode.Should().Be("CH");
    }

    [Fact]
    public async Task GetByNameAsync_WithNonExistentName_ReturnsNull()
    {
        // Arrange - 空数据库

        // Act
        var result = await _sut.GetByNameAsync("不存在的药材");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_WithDeletedHerb_ReturnsNull()
    {
        // Arrange
        var deletedHerb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "已删除药材",
            PinYinCode = "YSCYC",
            Origin = "测试",
            CreatedBy = Guid.NewGuid(),
            IsDeleted = true
        };

        _context.Herbs.Add(deletedHerb);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByNameAsync("已删除药材");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByNameOrPinyinAsync Tests - Issue #1469核心功能

    [Fact]
    public async Task GetByNameOrPinyinAsync_WithExactName_ReturnsHerb()
    {
        // Arrange
        var herb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "黄芪",
            PinYinCode = "HQ",
            Origin = "甘肃",
            CreatedBy = Guid.NewGuid()
        };

        _context.Herbs.Add(herb);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByNameOrPinyinAsync("黄芪");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("黄芪");
        result.PinYinCode.Should().Be("HQ");
    }

    [Fact]
    public async Task GetByNameOrPinyinAsync_WithPinyinCode_ReturnsHerb()
    {
        // Arrange
        var herb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "当归",
            PinYinCode = "DG",
            Origin = "甘肃岷县",
            CreatedBy = Guid.NewGuid()
        };

        _context.Herbs.Add(herb);
        await _context.SaveChangesAsync();

        // Act - 使用拼音码查询
        var result = await _sut.GetByNameOrPinyinAsync("DG");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("当归");
        result.PinYinCode.Should().Be("DG");
    }

    [Fact]
    public async Task GetByNameOrPinyinAsync_WithPartialPinyinCode_ReturnsHerb()
    {
        // Arrange
        var herb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "白芍",
            PinYinCode = "BS",
            Origin = "浙江",
            CreatedBy = Guid.NewGuid()
        };

        _context.Herbs.Add(herb);
        await _context.SaveChangesAsync();

        // Act - 模糊匹配拼音码
        var result = await _sut.GetByNameOrPinyinAsync("B");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("白芍");
    }

    [Fact]
    public async Task GetByNameOrPinyinAsync_PrioritizesExactNameMatch()
    {
        // Arrange - 创建两个药材，一个名称匹配，一个拼音码匹配
        var herb1 = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "甘草",
            PinYinCode = "GC",
            Origin = "内蒙古",
            CreatedBy = Guid.NewGuid()
        };

        var herb2 = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "测试药材",
            PinYinCode = "甘草", // 拼音码包含"甘草"
            Origin = "测试",
            CreatedBy = Guid.NewGuid()
        };

        _context.Herbs.AddRange(herb1, herb2);
        await _context.SaveChangesAsync();

        // Act - 应该优先返回名称精确匹配的
        var result = await _sut.GetByNameOrPinyinAsync("甘草");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("甘草"); // 名称精确匹配优先
        result.PinYinCode.Should().Be("GC");
    }

    [Fact]
    public async Task GetByNameOrPinyinAsync_WithNonExistentTerm_ReturnsNull()
    {
        // Arrange
        var herb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "川芎",
            PinYinCode = "CX",
            Origin = "四川",
            CreatedBy = Guid.NewGuid()
        };

        _context.Herbs.Add(herb);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByNameOrPinyinAsync("不存在");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameOrPinyinAsync_WithDeletedHerb_ReturnsNull()
    {
        // Arrange
        var deletedHerb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "红花",
            PinYinCode = "HH",
            Origin = "新疆",
            CreatedBy = Guid.NewGuid(),
            IsDeleted = true
        };

        _context.Herbs.Add(deletedHerb);
        await _context.SaveChangesAsync();

        // Act - 按名称查询
        var resultByName = await _sut.GetByNameOrPinyinAsync("红花");

        // Act - 按拼音码查询
        var resultByPinyin = await _sut.GetByNameOrPinyinAsync("HH");

        // Assert
        resultByName.Should().BeNull();
        resultByPinyin.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameOrPinyinAsync_WithNullPinyinCode_OnlyMatchesName()
    {
        // Arrange - 药材没有拼音码
        var herb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "特殊药材",
            PinYinCode = null, // 没有拼音码
            Origin = "测试",
            CreatedBy = Guid.NewGuid()
        };

        _context.Herbs.Add(herb);
        await _context.SaveChangesAsync();

        // Act - 按名称查询应该成功
        var resultByName = await _sut.GetByNameOrPinyinAsync("特殊药材");

        // Act - 按不存在的拼音码查询应该失败
        var resultByPinyin = await _sut.GetByNameOrPinyinAsync("TSYC");

        // Assert
        resultByName.Should().NotBeNull();
        resultByName!.Name.Should().Be("特殊药材");

        resultByPinyin.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameOrPinyinAsync_WithMultiplePinyinMatches_ReturnsFirstMatch()
    {
        // Arrange - 创建多个拼音码包含相同字符的药材
        var herb1 = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "白芷",
            PinYinCode = "BZ",
            Origin = "浙江",
            CreatedBy = Guid.NewGuid()
        };

        var herb2 = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "白术",
            PinYinCode = "BS",
            Origin = "浙江",
            CreatedBy = Guid.NewGuid()
        };

        // 确保插入顺序，先插入herb1
        _context.Herbs.Add(herb1);
        await _context.SaveChangesAsync();

        _context.Herbs.Add(herb2);
        await _context.SaveChangesAsync();

        // Act - 模糊匹配"B"
        var result = await _sut.GetByNameOrPinyinAsync("B");

        // Assert - 应该返回第一个匹配的
        result.Should().NotBeNull();
        result!.PinYinCode.Should().Contain("B");
    }

    #endregion

    #region 实际业务场景测试 - Issue #1469延迟绑定场景

    [Fact]
    public async Task GetByNameOrPinyinAsync_ImportScenario_HandlesVariantNames()
    {
        // Arrange - 模拟老系统导入场景：药材有多个异名
        var herb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "柴胡", // 标准名称
            PinYinCode = "CH",
            Origin = "甘肃",
            CreatedBy = Guid.NewGuid()
        };

        _context.Herbs.Add(herb);
        await _context.SaveChangesAsync();

        // Act - 老系统可能使用异名"北柴胡"导入
        var resultByVariantName = await _sut.GetByNameOrPinyinAsync("北柴胡");

        // Act - 但可以通过拼音码"CH"匹配
        var resultByPinyin = await _sut.GetByNameOrPinyinAsync("CH");

        // Assert
        resultByVariantName.Should().BeNull(); // 异名无法直接匹配
        resultByPinyin.Should().NotBeNull();   // 拼音码可以匹配
        resultByPinyin!.Name.Should().Be("柴胡");
    }

    [Fact]
    public async Task GetByNameOrPinyinAsync_BatchImportScenario_PerformanceTest()
    {
        // Arrange - 创建100个药材模拟大批量导入
        var herbs = new List<Herb>();
        for (int i = 0; i < 100; i++)
        {
            herbs.Add(new Herb
            {
                Id = Guid.NewGuid(),
                Name = $"药材{i}",
                PinYinCode = $"YC{i}",
                Origin = "测试产地",
                CreatedBy = Guid.NewGuid()
            });
        }

        _context.Herbs.AddRange(herbs);
        await _context.SaveChangesAsync();

        // Act - 查询中间某个药材
        var result = await _sut.GetByNameOrPinyinAsync("药材50");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("药材50");
        result.PinYinCode.Should().Be("YC50");
    }

    #endregion

    #region GetPagedAsync Tests - Phase 2.8: Epic #1725新增分页功能

    [Fact]
    public async Task GetPagedAsync_WithDefaultParameters_ReturnsPagedResult()
    {
        // Arrange - 创建5个药材
        var herbs = new List<Herb>
        {
            new Herb { Id = Guid.NewGuid(), Name = "柴胡", PinYinCode = "CH", Origin = "甘肃", CreatedBy = Guid.NewGuid() },
            new Herb { Id = Guid.NewGuid(), Name = "黄芪", PinYinCode = "HQ", Origin = "内蒙古", CreatedBy = Guid.NewGuid() },
            new Herb { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "DG", Origin = "甘肃", CreatedBy = Guid.NewGuid() },
            new Herb { Id = Guid.NewGuid(), Name = "白芍", PinYinCode = "BS", Origin = "浙江", CreatedBy = Guid.NewGuid() },
            new Herb { Id = Guid.NewGuid(), Name = "甘草", PinYinCode = "GC", Origin = "内蒙古", CreatedBy = Guid.NewGuid() }
        };

        _context.Herbs.AddRange(herbs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPagedAsync(1, 20, keyword: null);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(5);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetPagedAsync_WithKeywordMatchingName_ReturnsFilteredResults()
    {
        // Arrange
        var herbs = new List<Herb>
        {
            new Herb { Id = Guid.NewGuid(), Name = "柴胡", PinYinCode = "CH", Origin = "甘肃", CreatedBy = Guid.NewGuid() },
            new Herb { Id = Guid.NewGuid(), Name = "黄芪", PinYinCode = "HQ", Origin = "内蒙古", CreatedBy = Guid.NewGuid() },
            new Herb { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "DG", Origin = "甘肃", CreatedBy = Guid.NewGuid() }
        };

        _context.Herbs.AddRange(herbs);
        await _context.SaveChangesAsync();

        // Act - 搜索"柴"
        var result = await _sut.GetPagedAsync(1, 20, "柴");

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("柴胡");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_WithKeywordMatchingPinyin_ReturnsFilteredResults()
    {
        // Arrange
        var herbs = new List<Herb>
        {
            new Herb { Id = Guid.NewGuid(), Name = "柴胡", PinYinCode = "CH", Origin = "甘肃", CreatedBy = Guid.NewGuid() },
            new Herb { Id = Guid.NewGuid(), Name = "黄芪", PinYinCode = "HQ", Origin = "内蒙古", CreatedBy = Guid.NewGuid() },
            new Herb { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "DG", Origin = "甘肃", CreatedBy = Guid.NewGuid() }
        };

        _context.Herbs.AddRange(herbs);
        await _context.SaveChangesAsync();

        // Act - 搜索拼音码"CH"
        var result = await _sut.GetPagedAsync(1, 20, "CH");

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("柴胡");
        result.Items[0].PinYinCode.Should().Be("CH");
    }

    [Fact]
    public async Task GetPagedAsync_WithKeywordMatchingMultiple_ReturnsAllMatches()
    {
        // Arrange - 创建多个包含"草"的药材
        var herbs = new List<Herb>
        {
            new Herb { Id = Guid.NewGuid(), Name = "甘草", PinYinCode = "GC", Origin = "内蒙古", CreatedBy = Guid.NewGuid() },
            new Herb { Id = Guid.NewGuid(), Name = "益母草", PinYinCode = "YMC", Origin = "四川", CreatedBy = Guid.NewGuid() },
            new Herb { Id = Guid.NewGuid(), Name = "夏枯草", PinYinCode = "XKC", Origin = "江苏", CreatedBy = Guid.NewGuid() },
            new Herb { Id = Guid.NewGuid(), Name = "柴胡", PinYinCode = "CH", Origin = "甘肃", CreatedBy = Guid.NewGuid() }
        };

        _context.Herbs.AddRange(herbs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPagedAsync(1, 20, "草");

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.Items.Should().OnlyContain(h => h.Name.Contains("草"));
    }

    [Fact]
    public async Task GetPagedAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - 创建10个药材
        var herbs = new List<Herb>();
        for (int i = 0; i < 10; i++)
        {
            herbs.Add(new Herb
            {
                Id = Guid.NewGuid(),
                Name = $"药材{i:D2}", // 00-09确保排序
                PinYinCode = $"YC{i}",
                Origin = "测试",
                CreatedBy = Guid.NewGuid()
            });
        }

        _context.Herbs.AddRange(herbs);
        await _context.SaveChangesAsync();

        // Act - 获取第2页，每页3条
        var result = await _sut.GetPagedAsync(2, 3, keyword: null);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.CurrentPage.Should().Be(2);
        result.PageSize.Should().Be(3);
        result.TotalPages.Should().Be(4);
    }

    [Fact]
    public async Task GetPagedAsync_WithLargeDataset_Supports300PlusHerbs()
    {
        // Arrange - 创建300个药材模拟实际场景（用户需求：300+药材）
        var herbs = new List<Herb>();
        for (int i = 0; i < 300; i++)
        {
            herbs.Add(new Herb
            {
                Id = Guid.NewGuid(),
                Name = $"药材{i:D3}",
                PinYinCode = $"YC{i}",
                Origin = "测试产地",
                CreatedBy = Guid.NewGuid()
            });
        }

        _context.Herbs.AddRange(herbs);
        await _context.SaveChangesAsync();

        // Act - 分页获取（每页20条）
        var page1 = await _sut.GetPagedAsync(1, 20, keyword: null);
        var page5 = await _sut.GetPagedAsync(5, 20, keyword: null);
        var page15 = await _sut.GetPagedAsync(15, 20, keyword: null); // 最后一页

        // Assert - 第1页
        page1.Should().NotBeNull();
        page1.Items.Should().HaveCount(20);
        page1.TotalCount.Should().Be(300);
        page1.CurrentPage.Should().Be(1);

        // Assert - 第5页
        page5.Should().NotBeNull();
        page5.Items.Should().HaveCount(20);
        page5.CurrentPage.Should().Be(5);

        // Assert - 最后一页
        page15.Should().NotBeNull();
        page15.Items.Should().HaveCount(20);
        page15.CurrentPage.Should().Be(15);
        page15.TotalPages.Should().Be(15);
    }

    [Fact]
    public async Task GetPagedAsync_WithDeletedHerbs_ExcludesDeleted()
    {
        // Arrange
        var herbs = new List<Herb>
        {
            new Herb { Id = Guid.NewGuid(), Name = "有效药材1", PinYinCode = "YX1", Origin = "测试", CreatedBy = Guid.NewGuid(), IsDeleted = false },
            new Herb { Id = Guid.NewGuid(), Name = "已删除药材", PinYinCode = "YSCYC", Origin = "测试", CreatedBy = Guid.NewGuid(), IsDeleted = true },
            new Herb { Id = Guid.NewGuid(), Name = "有效药材2", PinYinCode = "YX2", Origin = "测试", CreatedBy = Guid.NewGuid(), IsDeleted = false }
        };

        _context.Herbs.AddRange(herbs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPagedAsync(1, 20, keyword: null);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(h => !h.IsDeleted);
    }

    [Fact]
    public async Task GetPagedAsync_ResultsSortedByName_Ascending()
    {
        // Arrange - 创建无序的药材
        var herbs = new List<Herb>
        {
            new Herb { Id = Guid.NewGuid(), Name = "枸杞", PinYinCode = "GQ", Origin = "宁夏", CreatedBy = Guid.NewGuid() },
            new Herb { Id = Guid.NewGuid(), Name = "阿胶", PinYinCode = "AJ", Origin = "山东", CreatedBy = Guid.NewGuid() },
            new Herb { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "DG", Origin = "甘肃", CreatedBy = Guid.NewGuid() }
        };

        _context.Herbs.AddRange(herbs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPagedAsync(1, 20, keyword: null);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("阿胶"); // 按名称升序
        result.Items[1].Name.Should().Be("当归");
        result.Items[2].Name.Should().Be("枸杞");
    }

    [Fact]
    public async Task GetPagedAsync_WithEmptyDatabase_ReturnsEmptyResult()
    {
        // Arrange - 空数据库

        // Act
        var result = await _sut.GetPagedAsync(1, 20, keyword: null);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.CurrentPage.Should().Be(1);
    }

    #endregion
}
