using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Caching;
using LYBT.Infrastructure.Data;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace LYBT.Module.Herbs.Tests.Services;

/// <summary>
/// HerbService 单元测试
/// 测试覆盖：CRUD操作、批量导入、引用检查、状态管理
/// </summary>
public class HerbServiceTests : IDisposable
{
    private readonly IHerbRepository _repositoryMock;
    private readonly IValidator<HerbInputDto> _validatorMock;
    private readonly AppDbContext _dbContext;
    private readonly ICacheInvalidationService _cacheInvalidationMock;
    private readonly IHerbImportExportService _importExportMock;
    private readonly HerbService _sut;

    public HerbServiceTests()
    {
        _repositoryMock = Substitute.For<IHerbRepository>();
        _validatorMock = Substitute.For<IValidator<HerbInputDto>>();
        _cacheInvalidationMock = Substitute.For<ICacheInvalidationService>();
        _importExportMock = Substitute.For<IHerbImportExportService>();

        // 使用 InMemory 数据库用于 DbContext 操作
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);

        // 默认验证通过
        _validatorMock
            .ValidateAsync(Arg.Any<HerbInputDto>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());

        // 默认名称不存在
        _repositoryMock
            .ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>())
            .Returns(false);

        _sut = new HerbService(
            _repositoryMock,
            NullLogger<HerbService>.Instance,
            _validatorMock,
            _dbContext,
            _cacheInvalidationMock,
            _importExportMock);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_WithDefaultParameters_ShouldReturnPagedResult()
    {
        // Arrange
        var herbs = CreateTestHerbs(5);
        var pagedResult = new PagedResult<Herb>
        {
            Items = herbs,
            TotalCount = 5,
            CurrentPage = 1,
            PageSize = 20
        };

        _repositoryMock
            .GetPagedAsync(1, 20, null)
            .Returns(pagedResult);

        // Act
        var result = await _sut.GetPagedAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(5);
        result.Data.TotalCount.Should().Be(5);
        result.Data.CurrentPage.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_WithKeyword_ShouldFilterByNameOrPinyin()
    {
        // Arrange
        var herbs = new List<Herb>
        {
            CreateTestHerb(name: "黄芪", pinYinCode: "HQ")
        };
        var pagedResult = new PagedResult<Herb>
        {
            Items = herbs,
            TotalCount = 1,
            CurrentPage = 1,
            PageSize = 20
        };

        _repositoryMock
            .GetPagedAsync(1, 20, "黄芪")
            .Returns(pagedResult);

        // Act
        var result = await _sut.GetPagedAsync(keyword: "黄芪");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().Name.Should().Be("黄芪");
    }

    [Fact]
    public async Task GetPagedAsync_WithCategory_ShouldFilterByCategory()
    {
        // Arrange
        var herbs = new List<Herb>
        {
            CreateTestHerb(name: "黄芪", category: "补气药"),
            CreateTestHerb(name: "当归", category: "补血药")
        };
        var pagedResult = new PagedResult<Herb>
        {
            Items = herbs,
            TotalCount = 2,
            CurrentPage = 1,
            PageSize = 20
        };

        _repositoryMock
            .GetPagedAsync(1, 20, null)
            .Returns(pagedResult);

        // Act
        var result = await _sut.GetPagedAsync(category: "补气药");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().Category.Should().Be("补气药");
    }

    [Fact]
    public async Task GetPagedAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var herbs = CreateTestHerbs(3);
        var pagedResult = new PagedResult<Herb>
        {
            Items = herbs,
            TotalCount = 10,
            CurrentPage = 2,
            PageSize = 3
        };

        _repositoryMock
            .GetPagedAsync(2, 3, null)
            .Returns(pagedResult);

        // Act
        var result = await _sut.GetPagedAsync(page: 2, pageSize: 3);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.CurrentPage.Should().Be(2);
        result.Data.PageSize.Should().Be(3);
        result.Data.TotalCount.Should().Be(10);
    }

    [Fact]
    public async Task GetPagedAsync_WithNoResults_ShouldReturnEmptyList()
    {
        // Arrange
        var pagedResult = new PagedResult<Herb>
        {
            Items = new List<Herb>(),
            TotalCount = 0,
            CurrentPage = 1,
            PageSize = 20
        };

        _repositoryMock
            .GetPagedAsync(1, 20, "不存在")
            .Returns(pagedResult);

        // Act
        var result = await _sut.GetPagedAsync(keyword: "不存在");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnHerbDetail()
    {
        // Arrange
        var herbId = Guid.NewGuid();
        var herb = CreateTestHerb(id: herbId, name: "黄芪");

        _repositoryMock
            .GetByIdAsync(herbId)
            .Returns(herb);

        // Act
        var result = await _sut.GetByIdAsync(herbId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("黄芪");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnFailure()
    {
        // Arrange
        var herbId = Guid.NewGuid();

        _repositoryMock
            .GetByIdAsync(herbId)
            .Returns((Herb?)null);

        // Act
        var result = await _sut.GetByIdAsync(herbId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("不存在");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidInput_ShouldCreateAndReturnHerb()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        var savedHerb = CreateTestHerb(name: dto.Name);

        _repositoryMock
            .AddAsync(Arg.Any<Herb>())
            .Returns(savedHerb);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(dto.Name);

        await _repositoryMock.Received(1).AddAsync(Arg.Any<Herb>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidInput_ShouldReturnValidationError()
    {
        // Arrange
        var dto = CreateValidHerbInputDto();
        var validationResult = new FluentValidation.Results.ValidationResult(new[]
        {
            new ValidationFailure("Name", "药材名称不能为空")
        });

        _validatorMock
            .ValidateAsync(dto, Arg.Any<CancellationToken>())
            .Returns(validationResult);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("药材名称不能为空");

        await _repositoryMock.DidNotReceive().AddAsync(Arg.Any<Herb>());
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidInput_ShouldUpdateAndReturnHerb()
    {
        // Arrange
        var herbId = Guid.NewGuid();
        var existingHerb = CreateTestHerb(id: herbId, name: "旧名称");
        var dto = CreateValidHerbInputDto("新名称");

        _repositoryMock
            .GetByIdAsync(herbId)
            .Returns(existingHerb);

        _repositoryMock
            .UpdateAsync(Arg.Any<Herb>())
            .Returns(x => (Herb)x[0]);

        // Act
        var result = await _sut.UpdateAsync(herbId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _repositoryMock.Received(1).UpdateAsync(Arg.Any<Herb>());
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ShouldReturnFailure()
    {
        // Arrange
        var herbId = Guid.NewGuid();
        var dto = CreateValidHerbInputDto();

        _repositoryMock
            .GetByIdAsync(herbId)
            .Returns((Herb?)null);

        // Act
        var result = await _sut.UpdateAsync(herbId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("不存在");

        await _repositoryMock.DidNotReceive().UpdateAsync(Arg.Any<Herb>());
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidInput_ShouldReturnValidationError()
    {
        // Arrange
        var herbId = Guid.NewGuid();
        var existingHerb = CreateTestHerb(id: herbId);
        var dto = CreateValidHerbInputDto();

        _repositoryMock
            .GetByIdAsync(herbId)
            .Returns(existingHerb);

        _validatorMock
            .ValidateAsync(dto, Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult(new[]
            {
                new ValidationFailure("Price", "单价必须大于0")
            }));

        // Act
        var result = await _sut.UpdateAsync(herbId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("单价必须大于0");

        await _repositoryMock.DidNotReceive().UpdateAsync(Arg.Any<Herb>());
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingId_ShouldSoftDelete()
    {
        // Arrange
        var herbId = Guid.NewGuid();

        // Act
        var result = await _sut.DeleteAsync(herbId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _repositoryMock.Received(1).DeleteAsync(herbId);
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithKeyword_ShouldReturnMatchingHerbs()
    {
        // Arrange
        var herbs = new List<Herb>
        {
            CreateTestHerb(name: "黄芪"),
            CreateTestHerb(name: "黄芩")
        };

        _repositoryMock
            .FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Herb, bool>>>())
            .Returns(herbs);

        // Act
        var result = await _sut.SearchAsync("黄");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    #endregion

    #region ToggleStatusAsync Tests

    [Fact]
    public async Task ToggleStatusAsync_EnabledToDisabled_ShouldToggle()
    {
        // Arrange
        var herbId = Guid.NewGuid();
        var herb = CreateTestHerb(id: herbId, status: CommonStatus.Enabled);

        _repositoryMock
            .GetByIdAsync(herbId)
            .Returns(herb);

        _repositoryMock
            .UpdateAsync(Arg.Any<Herb>())
            .Returns(x =>
            {
                var h = (Herb)x[0];
                return h;
            });

        // Act
        var result = await _sut.ToggleStatusAsync(herbId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _repositoryMock.Received(1).UpdateAsync(
            Arg.Is<Herb>(h => h.Status == CommonStatus.Disabled));
    }

    [Fact]
    public async Task ToggleStatusAsync_DisabledToEnabled_ShouldToggle()
    {
        // Arrange
        var herbId = Guid.NewGuid();
        var herb = CreateTestHerb(id: herbId, status: CommonStatus.Disabled);

        _repositoryMock
            .GetByIdAsync(herbId)
            .Returns(herb);

        _repositoryMock
            .UpdateAsync(Arg.Any<Herb>())
            .Returns(x => (Herb)x[0]);

        // Act
        var result = await _sut.ToggleStatusAsync(herbId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _repositoryMock.Received(1).UpdateAsync(
            Arg.Is<Herb>(h => h.Status == CommonStatus.Enabled));
    }

    [Fact]
    public async Task ToggleStatusAsync_WithNonExistentId_ShouldReturnFailure()
    {
        // Arrange
        var herbId = Guid.NewGuid();

        _repositoryMock
            .GetByIdAsync(herbId)
            .Returns((Herb?)null);

        // Act
        var result = await _sut.ToggleStatusAsync(herbId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("不存在");
    }

    #endregion

    #region RestoreAsync Tests

    [Fact]
    public async Task RestoreAsync_WithDeletedHerb_ShouldRestore()
    {
        // Arrange
        var herbId = Guid.NewGuid();
        var deletedHerb = CreateTestHerb(id: herbId, isDeleted: true);

        _repositoryMock
            .GetByIdIncludingDeletedAsync(herbId)
            .Returns(deletedHerb);

        _repositoryMock
            .UpdateAsync(Arg.Any<Herb>())
            .Returns(x => (Herb)x[0]);

        // Act
        var result = await _sut.RestoreAsync(herbId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _repositoryMock.Received(1).UpdateAsync(
            Arg.Is<Herb>(h => h.IsDeleted == false));
    }

    [Fact]
    public async Task RestoreAsync_WithNonDeletedHerb_ShouldReturnFailure()
    {
        // Arrange
        var herbId = Guid.NewGuid();
        var activeHerb = CreateTestHerb(id: herbId, isDeleted: false);

        _repositoryMock
            .GetByIdIncludingDeletedAsync(herbId)
            .Returns(activeHerb);

        // Act
        var result = await _sut.RestoreAsync(herbId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("未被删除");
    }

    [Fact]
    public async Task RestoreAsync_WithNonExistentId_ShouldReturnFailure()
    {
        // Arrange
        var herbId = Guid.NewGuid();

        _repositoryMock
            .GetByIdIncludingDeletedAsync(herbId)
            .Returns((Herb?)null);

        // Act
        var result = await _sut.RestoreAsync(herbId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("不存在");
    }

    #endregion

    #region BatchImportAsync Tests

    [Fact]
    public async Task BatchImportAsync_WithNewHerbs_ShouldCreateAll()
    {
        // Arrange
        var dtos = new List<HerbInputDto>
        {
            CreateValidHerbInputDto("黄芪"),
            CreateValidHerbInputDto("当归")
        };

        _repositoryMock
            .ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>())
            .Returns(false);

        _repositoryMock
            .AddAsync(Arg.Any<Herb>())
            .Returns(x => (Herb)x[0]);

        // Act
        var result = await _sut.BatchImportAsync(dtos, DuplicateStrategy.Skip);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(2);
        result.Data.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task BatchImportAsync_WithDuplicates_SkipStrategy_ShouldSkip()
    {
        // Arrange
        var dtos = new List<HerbInputDto>
        {
            CreateValidHerbInputDto("已存在药材")
        };

        _repositoryMock
            .ExistsByNameAsync("已存在药材", Arg.Any<Guid?>())
            .Returns(true);

        // Act
        var result = await _sut.BatchImportAsync(dtos, DuplicateStrategy.Skip);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(0);
        result.Data.SkippedCount.Should().Be(1);

        await _repositoryMock.DidNotReceive().AddAsync(Arg.Any<Herb>());
    }

    [Fact]
    public async Task BatchImportAsync_WithDuplicates_ErrorStrategy_ShouldReportError()
    {
        // Arrange
        var dtos = new List<HerbInputDto>
        {
            CreateValidHerbInputDto("已存在药材")
        };

        _repositoryMock
            .ExistsByNameAsync("已存在药材", Arg.Any<Guid?>())
            .Returns(true);

        // Act
        var result = await _sut.BatchImportAsync(dtos, DuplicateStrategy.Error);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.FailureCount.Should().Be(1);
        result.Data.Failures.Should().Contain(f => f.Reason == "药材名称重复");
    }

    [Fact]
    public async Task BatchImportAsync_WithOverLimit_ShouldReturnError()
    {
        // Arrange
        var dtos = Enumerable.Range(0, 10001)
            .Select(i => CreateValidHerbInputDto($"药材{i}"))
            .ToList();

        // Act
        var result = await _sut.BatchImportAsync(dtos, DuplicateStrategy.Skip);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("10000");
    }

    #endregion

    #region BatchDeleteAsync Tests

    [Fact]
    public async Task BatchDeleteAsync_WithValidIds_ShouldDeleteAll()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        foreach (var id in ids)
        {
            _repositoryMock
                .GetByIdAsync(id)
                .Returns(CreateTestHerb(id: id));
        }

        _repositoryMock
            .UpdateAsync(Arg.Any<Herb>())
            .Returns(x => (Herb)x[0]);

        // Act
        var result = await _sut.BatchDeleteAsync(ids);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(2);
        result.Data.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task BatchDeleteAsync_WithSomeNonExistent_ShouldReportResults()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();
        var ids = new List<Guid> { existingId, nonExistentId };

        _repositoryMock
            .GetByIdAsync(existingId)
            .Returns(CreateTestHerb(id: existingId));

        _repositoryMock
            .GetByIdAsync(nonExistentId)
            .Returns((Herb?)null);

        _repositoryMock
            .UpdateAsync(Arg.Any<Herb>())
            .Returns(x => (Herb)x[0]);

        // Act
        var result = await _sut.BatchDeleteAsync(ids);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(1);
        result.Data.FailureCount.Should().Be(1);
        result.Data.FailedIds.Should().Contain(nonExistentId);
    }

    #endregion

    #region BatchUpdateStatusAsync Tests

    [Fact]
    public async Task BatchUpdateStatusAsync_WithValidIds_ShouldUpdateAll()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        foreach (var id in ids)
        {
            _repositoryMock
                .GetByIdAsync(id)
                .Returns(CreateTestHerb(id: id, status: CommonStatus.Enabled));
        }

        _repositoryMock
            .UpdateAsync(Arg.Any<Herb>())
            .Returns(x => (Herb)x[0]);

        // Act
        var result = await _sut.BatchUpdateStatusAsync(ids, CommonStatus.Disabled);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(2);
    }

    #endregion

    #region GetAllForExportAsync Tests

    [Fact]
    public async Task GetAllForExportAsync_ShouldReturnAllHerbs()
    {
        // Arrange
        var herbs = CreateTestHerbs(3);

        _repositoryMock
            .GetAllAsync()
            .Returns(herbs);

        // Act
        var result = await _sut.GetAllForExportAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllForExportAsync_WithCategory_ShouldFilter()
    {
        // Arrange
        var herbs = new List<Herb>
        {
            CreateTestHerb(name: "黄芪", category: "补气药"),
            CreateTestHerb(name: "当归", category: "补血药")
        };

        _repositoryMock
            .GetAllAsync()
            .Returns(herbs);

        // Act
        var result = await _sut.GetAllForExportAsync(category: "补气药");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data!.First().Category.Should().Be("补气药");
    }

    #endregion

    #region GenerateImportTemplate Tests

    [Fact]
    public void GenerateImportTemplate_ShouldReturnValidTemplate()
    {
        // Act
        var result = _sut.GenerateImportTemplate();

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region Helper Methods

    private static Herb CreateTestHerb(
        Guid? id = null,
        string? name = null,
        string? pinYinCode = null,
        string? category = null,
        CommonStatus status = CommonStatus.Enabled,
        bool isDeleted = false)
    {
        return new Herb
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? $"测试药材_{Guid.NewGuid():N}"[..20],
            PinYinCode = pinYinCode ?? "CSYC",
            Category = category ?? "补气药",
            Origin = "测试产地",
            Spec = "统货",
            Unit = "g",
            Price = 50m,
            CostPrice = 30m,
            Effect = "测试功效",
            Usage = "测试用法",
            Remark = "测试备注",
            Status = status,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
    }

    private static List<Herb> CreateTestHerbs(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => CreateTestHerb(name: $"药材{i}"))
            .ToList();
    }

    private static HerbInputDto CreateValidHerbInputDto(string? name = null)
    {
        return new HerbInputDto
        {
            Name = name ?? $"测试药材_{Guid.NewGuid():N}"[..20],
            Unit = "g",
            Price = 50m
        };
    }

    #endregion
}
