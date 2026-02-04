using FluentAssertions;
using LYBT.Entities.Herbs;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Sync.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Sync;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Sync.Tests.Services;

/// <summary>
/// SyncService 单元测试
/// 验证同步服务的核心业务逻辑
/// OpenSpec: implement-data-sync
/// </summary>
public class SyncServiceTests : TestBase
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IHerbService> _herbServiceMock;
    private readonly Mock<IPatientService> _patientServiceMock;
    private readonly Mock<ILogger<SyncService>> _loggerMock;
    private readonly SyncService _syncService;

    public SyncServiceTests()
    {
        // 创建内存数据库上下文
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);

        _herbServiceMock = CreateMock<IHerbService>();
        _patientServiceMock = CreateMock<IPatientService>();
        _loggerMock = CreateLoggerMock<SyncService>();

        _syncService = new SyncService(
            _dbContext,
            _herbServiceMock.Object,
            _patientServiceMock.Object,
            _loggerMock.Object);
    }

    #region GetSupportedEntityTypes 测试

    [Fact]
    public void GetSupportedEntityTypes_ShouldReturnHerbPatientFormula()
    {
        // Act
        var types = _syncService.GetSupportedEntityTypes();

        // Assert
        types.Should().NotBeNull();
        types.Should().HaveCount(3);
        types.Should().Contain("Herb");
        types.Should().Contain("Patient");
        types.Should().Contain("Formula");
    }

    #endregion

    #region GetMetadataAsync 测试

    [Fact]
    public async Task GetMetadataAsync_WithValidHerbType_ShouldReturnMetadata()
    {
        // Arrange
        var herb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "黄芪",
            PinYinCode = "HQ",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Herbs.Add(herb);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _syncService.GetMetadataAsync("Herb");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data![0].EntityId.Should().Be(herb.Id);
        result.Data[0].Checksum.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetMetadataAsync_WithInvalidEntityType_ShouldReturnFailure()
    {
        // Act
        var result = await _syncService.GetMetadataAsync("InvalidType");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("不支持的实体类型");
    }

    [Fact]
    public async Task GetMetadataAsync_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Act
        var result = await _syncService.GetMetadataAsync("Herb");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }

    #endregion

    #region CompareAsync 测试

    [Fact]
    public async Task CompareAsync_WithLocalOnlyEntity_ShouldReturnLocalOnlyDiff()
    {
        // Arrange
        var localEntityId = Guid.NewGuid();
        var input = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>
            {
                new()
                {
                    EntityId = localEntityId,
                    Checksum = "local-checksum-123",
                    LastModifiedAt = DateTime.UtcNow
                }
            }
        };

        // Act
        var result = await _syncService.CompareAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Diffs.Should().HaveCount(1);
        result.Data.Diffs[0].DiffType.Should().Be(SyncDiffType.LocalOnly);
        result.Data.Diffs[0].EntityId.Should().Be(localEntityId);
    }

    [Fact]
    public async Task CompareAsync_WithServerOnlyEntity_ShouldReturnServerOnlyDiff()
    {
        // Arrange
        var herb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "黄芪",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Herbs.Add(herb);
        await _dbContext.SaveChangesAsync();

        var input = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>() // 空列表，没有本地数据
        };

        // Act
        var result = await _syncService.CompareAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Diffs.Should().HaveCount(1);
        result.Data.Diffs[0].DiffType.Should().Be(SyncDiffType.ServerOnly);
        result.Data.Diffs[0].EntityId.Should().Be(herb.Id);
    }

    [Fact]
    public async Task CompareAsync_WithModifiedEntity_ShouldReturnModifiedDiff()
    {
        // Arrange
        var herb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "黄芪",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Herbs.Add(herb);
        await _dbContext.SaveChangesAsync();

        var input = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>
            {
                new()
                {
                    EntityId = herb.Id,
                    Checksum = "different-checksum-456", // 不同的 Checksum
                    LastModifiedAt = DateTime.UtcNow
                }
            }
        };

        // Act
        var result = await _syncService.CompareAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Diffs.Should().HaveCount(1);
        result.Data.Diffs[0].DiffType.Should().Be(SyncDiffType.Modified);
        result.Data.Diffs[0].EntityId.Should().Be(herb.Id);
    }

    [Fact]
    public async Task CompareAsync_WithIdenticalEntity_ShouldReturnNoDiff()
    {
        // Arrange
        var herb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "黄芪",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Herbs.Add(herb);
        await _dbContext.SaveChangesAsync();

        // 计算服务器端的 Checksum
        var serverChecksum = ChecksumHelper.ComputeHerbChecksum(herb);

        var input = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>
            {
                new()
                {
                    EntityId = herb.Id,
                    Checksum = serverChecksum, // 相同的 Checksum
                    LastModifiedAt = DateTime.UtcNow
                }
            }
        };

        // Act
        var result = await _syncService.CompareAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Diffs.Should().BeEmpty(); // Checksum 相同，无差异
    }

    [Fact]
    public async Task CompareAsync_WithInvalidEntityType_ShouldReturnFailure()
    {
        // Arrange
        var input = new SyncCompareInputDto
        {
            EntityType = "InvalidType",
            LocalEntities = new List<LocalEntityMetadata>()
        };

        // Act
        var result = await _syncService.CompareAsync(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("不支持的实体类型");
    }

    #endregion

    #region DownloadAsync 测试

    [Fact]
    public async Task DownloadAsync_WithExistingEntities_ShouldReturnEntities()
    {
        // Arrange
        var herb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "黄芪",
            PinYinCode = "HQ",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Herbs.Add(herb);
        await _dbContext.SaveChangesAsync();

        var input = new SyncDownloadInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { herb.Id }
        };

        // Act
        var result = await _syncService.DownloadAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Count.Should().Be(1);
        result.Data.Entities.Should().HaveCount(1);
    }

    [Fact]
    public async Task DownloadAsync_WithNonExistentEntity_ShouldReturnEmptyList()
    {
        // Arrange
        var input = new SyncDownloadInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var result = await _syncService.DownloadAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Count.Should().Be(0);
        result.Data.Entities.Should().BeEmpty();
    }

    [Fact]
    public async Task DownloadAsync_WithInvalidEntityType_ShouldReturnFailure()
    {
        // Arrange
        var input = new SyncDownloadInputDto
        {
            EntityType = "InvalidType",
            EntityIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var result = await _syncService.DownloadAsync(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("不支持的实体类型");
    }

    #endregion

    #region DeleteAsync 测试

    [Fact]
    public async Task DeleteAsync_WithNoReferences_ShouldSoftDeleteEntity()
    {
        // Arrange
        var herb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "黄芪",
            Status = CommonStatus.Enabled,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Herbs.Add(herb);
        await _dbContext.SaveChangesAsync();

        _herbServiceMock
            .Setup(x => x.CheckReferenceAsync(herb.Id))
            .Returns(Task.FromResult(Result<HerbReferenceCheckDto>.Success(
                new HerbReferenceCheckDto { HerbId = herb.Id, HasReferences = false, ReferenceCount = 0 })));

        var input = new SyncDeleteInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { herb.Id }
        };

        // Act
        var result = await _syncService.DeleteAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Success.Should().Contain(herb.Id);
        result.Data.Rejected.Should().BeEmpty();

        // 验证数据库中已软删除
        var deletedHerb = await _dbContext.Herbs.FindAsync(herb.Id);
        deletedHerb!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ShouldRejectDelete()
    {
        // Arrange
        var herb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "黄芪",
            Status = CommonStatus.Enabled,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Herbs.Add(herb);
        await _dbContext.SaveChangesAsync();

        _herbServiceMock
            .Setup(x => x.CheckReferenceAsync(herb.Id))
            .Returns(Task.FromResult(Result<HerbReferenceCheckDto>.Success(
                new HerbReferenceCheckDto
                {
                    HerbId = herb.Id,
                    HasReferences = true,
                    ReferenceCount = 5
                })));

        var input = new SyncDeleteInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { herb.Id }
        };

        // Act
        var result = await _syncService.DeleteAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Success.Should().BeEmpty();
        result.Data.Rejected.Should().HaveCount(1);
        result.Data.Rejected[0].EntityId.Should().Be(herb.Id);
        result.Data.Rejected[0].Reason.Should().Contain("被");
        result.Data.Rejected[0].Reason.Should().Contain("处方引用");

        // 验证数据库中未删除
        var notDeletedHerb = await _dbContext.Herbs.FindAsync(herb.Id);
        notDeletedHerb!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_PatientWithNoReferences_ShouldSoftDeleteEntity()
    {
        // Arrange
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            Name = "张三",
            Status = CommonStatus.Enabled,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Patients.Add(patient);
        await _dbContext.SaveChangesAsync();

        _patientServiceMock
            .Setup(x => x.CheckReferenceAsync(patient.Id))
            .Returns(Task.FromResult(Result<PatientReferenceCheckDto>.Success(
                new PatientReferenceCheckDto { PatientId = patient.Id, HasReferences = false, ReferenceCount = 0 })));

        var input = new SyncDeleteInputDto
        {
            EntityType = "Patient",
            EntityIds = new List<Guid> { patient.Id }
        };

        // Act
        var result = await _syncService.DeleteAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Success.Should().Contain(patient.Id);
    }

    [Fact]
    public async Task DeleteAsync_FormulaNoReferenceCheck_ShouldSoftDeleteDirectly()
    {
        // Arrange
        var formula = new LYBT.Entities.Formulas.Formula
        {
            Id = Guid.NewGuid(),
            Name = "补中益气汤",
            Status = CommonStatus.Enabled,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Formulas.Add(formula);
        await _dbContext.SaveChangesAsync();

        var input = new SyncDeleteInputDto
        {
            EntityType = "Formula",
            EntityIds = new List<Guid> { formula.Id }
        };

        // Act
        var result = await _syncService.DeleteAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Success.Should().Contain(formula.Id);
        result.Data.Rejected.Should().BeEmpty();

        // Formula 无引用检查，直接软删除
        var deletedFormula = await _dbContext.Formulas.FindAsync(formula.Id);
        deletedFormula!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithAlreadyDeletedEntity_ShouldReject()
    {
        // Arrange
        var herb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "黄芪",
            Status = CommonStatus.Enabled,
            IsDeleted = true, // 已经删除
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Herbs.Add(herb);
        await _dbContext.SaveChangesAsync();

        _herbServiceMock
            .Setup(x => x.CheckReferenceAsync(herb.Id))
            .Returns(Task.FromResult(Result<HerbReferenceCheckDto>.Success(
                new HerbReferenceCheckDto { HerbId = herb.Id, HasReferences = false, ReferenceCount = 0 })));

        var input = new SyncDeleteInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { herb.Id }
        };

        // Act
        var result = await _syncService.DeleteAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Success.Should().BeEmpty();
        result.Data.Rejected.Should().HaveCount(1);
        result.Data.Rejected[0].Reason.Should().Contain("不存在或已删除");
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidEntityType_ShouldReturnFailure()
    {
        // Arrange
        var input = new SyncDeleteInputDto
        {
            EntityType = "InvalidType",
            EntityIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var result = await _syncService.DeleteAsync(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("不支持的实体类型");
    }

    #endregion

    #region 清理

    public override void Dispose()
    {
        _dbContext?.Dispose();
        base.Dispose();
    }

    #endregion
}
