using System.Text.Json;
using FluentAssertions;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Sync.Services;
using LYBT.Shared.Models.Contracts.Sync;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Unit.Modules.Sync.Services;

/// <summary>
/// SyncService 单元测试
/// 验证同步服务的核心业务逻辑
/// OpenSpec: implement-data-sync
/// </summary>
public class SyncServiceTests : TestBase
{
    private readonly AppDbContext _dbContext;
    private readonly IHerbCrossModuleService _herbCrossModuleMock;
    private readonly IPatientCrossModuleService _patientCrossModuleMock;
    private readonly ILogger<SyncService> _loggerMock;
    private readonly SyncService _syncService;

    public SyncServiceTests()
    {
        // 创建内存数据库上下文
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);

        _herbCrossModuleMock = CreateMock<IHerbCrossModuleService>();
        _patientCrossModuleMock = CreateMock<IPatientCrossModuleService>();
        _loggerMock = CreateLoggerMock<SyncService>();

        _syncService = new SyncService(
            _dbContext,
            _herbCrossModuleMock,
            _patientCrossModuleMock,
            _loggerMock);
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
            LocalEntities = new List<LocalEntityMetadata>()
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
                    Checksum = "different-checksum-456",
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

        var serverChecksum = ChecksumHelper.ComputeHerbChecksum(herb);

        var input = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>
            {
                new()
                {
                    EntityId = herb.Id,
                    Checksum = serverChecksum,
                    LastModifiedAt = DateTime.UtcNow
                }
            }
        };

        // Act
        var result = await _syncService.CompareAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Diffs.Should().BeEmpty();
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

        _herbCrossModuleMock.CheckHerbReferenceAsync(herb.Id)
            .Returns(new ReferenceCheckResult(HasReferences: false, ReferenceCount: 0));

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

        _herbCrossModuleMock.CheckHerbReferenceAsync(herb.Id)
            .Returns(new ReferenceCheckResult(HasReferences: true, ReferenceCount: 5));

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

        _patientCrossModuleMock.CheckPatientReferenceAsync(patient.Id)
            .Returns(new ReferenceCheckResult(HasReferences: false, ReferenceCount: 0));

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
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Herbs.Add(herb);
        await _dbContext.SaveChangesAsync();

        _herbCrossModuleMock.CheckHerbReferenceAsync(herb.Id)
            .Returns(new ReferenceCheckResult(HasReferences: false, ReferenceCount: 0));

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

    #region UploadAsync 测试

    [Fact]
    public async Task UploadAsync_WithNewHerb_ShouldCreate()
    {
        // Arrange
        var herbId = Guid.NewGuid();
        var herb = new Herb
        {
            Id = herbId,
            Name = "黄芪",
            PinYinCode = "HQ",
            Unit = "g",
            Price = 50m,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
        var json = SerializeToJsonElement(herb);

        var input = new SyncUploadInputDto
        {
            EntityType = "Herb",
            Entities = new List<JsonElement> { json },
            OverwriteConflicts = false
        };

        // Act
        var result = await _syncService.UploadAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(1);
        result.Data.ConflictCount.Should().Be(0);
        result.Data.ErrorCount.Should().Be(0);

        var createdHerb = await _dbContext.Herbs.FindAsync(herbId);
        createdHerb.Should().NotBeNull();
        createdHerb!.Name.Should().Be("黄芪");
    }

    [Fact]
    public async Task UploadAsync_WithExistingHerb_OverwriteTrue_ShouldUpdate()
    {
        // Arrange
        var herbId = Guid.NewGuid();
        var existingHerb = new Herb
        {
            Id = herbId,
            Name = "黄芪",
            PinYinCode = "HQ",
            Unit = "g",
            Price = 50m,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Herbs.Add(existingHerb);
        await _dbContext.SaveChangesAsync();

        var updatedHerb = new Herb
        {
            Id = herbId,
            Name = "黄芪（蜜炙）",
            PinYinCode = "HQMZ",
            Unit = "g",
            Price = 60m,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
        var json = SerializeToJsonElement(updatedHerb);

        var input = new SyncUploadInputDto
        {
            EntityType = "Herb",
            Entities = new List<JsonElement> { json },
            OverwriteConflicts = true
        };

        // Act
        var result = await _syncService.UploadAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(1);
        result.Data.ConflictCount.Should().Be(0);

        var herb = await _dbContext.Herbs.FindAsync(herbId);
        herb!.Name.Should().Be("黄芪（蜜炙）");
        herb.Price.Should().Be(60m);
    }

    [Fact]
    public async Task UploadAsync_WithExistingHerb_OverwriteFalse_ShouldReturnConflict()
    {
        // Arrange
        var herbId = Guid.NewGuid();
        var existingHerb = new Herb
        {
            Id = herbId,
            Name = "黄芪",
            Unit = "g",
            Price = 50m,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Herbs.Add(existingHerb);
        await _dbContext.SaveChangesAsync();

        var uploadHerb = new Herb
        {
            Id = herbId,
            Name = "黄芪（修改）",
            Unit = "g",
            Price = 60m,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
        var json = SerializeToJsonElement(uploadHerb);

        var input = new SyncUploadInputDto
        {
            EntityType = "Herb",
            Entities = new List<JsonElement> { json },
            OverwriteConflicts = false
        };

        // Act
        var result = await _syncService.UploadAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(0);
        result.Data.ConflictCount.Should().Be(1);
        result.Data.Results[0].IsConflict.Should().BeTrue();

        var herb = await _dbContext.Herbs.FindAsync(herbId);
        herb!.Name.Should().Be("黄芪");
    }

    [Fact]
    public async Task UploadAsync_WithNewPatient_ShouldCreate()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new Patient
        {
            Id = patientId,
            Name = "张三",
            PinYinCode = "ZS",
            Gender = Gender.Male,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
        var json = SerializeToJsonElement(patient);

        var input = new SyncUploadInputDto
        {
            EntityType = "Patient",
            Entities = new List<JsonElement> { json },
            OverwriteConflicts = false
        };

        // Act
        var result = await _syncService.UploadAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(1);

        var createdPatient = await _dbContext.Patients.FindAsync(patientId);
        createdPatient.Should().NotBeNull();
        createdPatient!.Name.Should().Be("张三");
    }

    [Fact]
    public async Task UploadAsync_WithNewFormula_ShouldCreateWithHerbs()
    {
        // Arrange
        var formulaId = Guid.NewGuid();
        var formula = new Formula
        {
            Id = formulaId,
            Name = "补中益气汤",
            Category = "补益剂",
            Effect = "补中益气",
            Status = CommonStatus.Enabled,
            FormulaType = FormulaType.Classic,
            CreatedAt = DateTime.UtcNow,
            Herbs = new List<FormulaHerbItem>
            {
                new() { Id = Guid.NewGuid(), FormulaId = formulaId, HerbId = Guid.NewGuid(), HerbName = "黄芪", Dosage = 15, Unit = "g" },
                new() { Id = Guid.NewGuid(), FormulaId = formulaId, HerbId = Guid.NewGuid(), HerbName = "党参", Dosage = 10, Unit = "g" }
            }
        };
        var json = SerializeToJsonElement(formula);

        var input = new SyncUploadInputDto
        {
            EntityType = "Formula",
            Entities = new List<JsonElement> { json },
            OverwriteConflicts = false
        };

        // Act
        var result = await _syncService.UploadAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(1);

        var createdFormula = await _dbContext.Formulas
            .Include(f => f.Herbs)
            .FirstOrDefaultAsync(f => f.Id == formulaId);
        createdFormula.Should().NotBeNull();
        createdFormula!.Name.Should().Be("补中益气汤");
        createdFormula.Herbs.Should().HaveCount(2);
    }

    [Fact]
    public async Task UploadAsync_WithExistingFormula_OverwriteTrue_ShouldUpdateHerbs()
    {
        // Arrange
        var formulaId = Guid.NewGuid();
        var oldHerbItemId = Guid.NewGuid();
        var existingFormula = new Formula
        {
            Id = formulaId,
            Name = "补中益气汤",
            Status = CommonStatus.Enabled,
            FormulaType = FormulaType.Classic,
            CreatedAt = DateTime.UtcNow,
            Herbs = new List<FormulaHerbItem>
            {
                new() { Id = oldHerbItemId, FormulaId = formulaId, HerbId = Guid.NewGuid(), HerbName = "旧药材", Dosage = 5, Unit = "g" }
            }
        };
        _dbContext.Formulas.Add(existingFormula);
        await _dbContext.SaveChangesAsync();

        var updatedFormula = new Formula
        {
            Id = formulaId,
            Name = "补中益气汤（加减）",
            Status = CommonStatus.Enabled,
            FormulaType = FormulaType.Experience,
            CreatedAt = DateTime.UtcNow,
            Herbs = new List<FormulaHerbItem>
            {
                new() { Id = Guid.NewGuid(), FormulaId = formulaId, HerbId = Guid.NewGuid(), HerbName = "新药材1", Dosage = 10, Unit = "g" },
                new() { Id = Guid.NewGuid(), FormulaId = formulaId, HerbId = Guid.NewGuid(), HerbName = "新药材2", Dosage = 15, Unit = "g" }
            }
        };
        var json = SerializeToJsonElement(updatedFormula);

        var input = new SyncUploadInputDto
        {
            EntityType = "Formula",
            Entities = new List<JsonElement> { json },
            OverwriteConflicts = true
        };

        // Act
        var result = await _syncService.UploadAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(1);

        var formula = await _dbContext.Formulas
            .Include(f => f.Herbs)
            .FirstOrDefaultAsync(f => f.Id == formulaId);
        formula!.Name.Should().Be("补中益气汤（加减）");
        formula.FormulaType.Should().Be(FormulaType.Experience);
        formula.Herbs.Should().HaveCount(2);
        formula.Herbs.Should().Contain(h => h.HerbName == "新药材1");
    }

    [Fact]
    public async Task UploadAsync_WithBatchEntities_ShouldProcessAll()
    {
        // Arrange
        var herbs = Enumerable.Range(1, 5)
            .Select(i => new Herb
            {
                Id = Guid.NewGuid(),
                Name = $"药材{i}",
                Unit = "g",
                Price = i * 10m,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        var input = new SyncUploadInputDto
        {
            EntityType = "Herb",
            Entities = herbs.Select(SerializeToJsonElement).ToList(),
            OverwriteConflicts = false
        };

        // Act
        var result = await _syncService.UploadAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(5);
        result.Data.Results.Should().HaveCount(5);

        var count = await _dbContext.Herbs.CountAsync();
        count.Should().Be(5);
    }

    [Fact]
    public async Task UploadAsync_WithInvalidJson_ShouldReturnError()
    {
        // Arrange
        var invalidJson = JsonDocument.Parse("[1, 2, 3]").RootElement;

        var input = new SyncUploadInputDto
        {
            EntityType = "Herb",
            Entities = new List<JsonElement> { invalidJson },
            OverwriteConflicts = false
        };

        // Act
        var result = await _syncService.UploadAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.ErrorCount.Should().Be(1);
        result.Data.SuccessCount.Should().Be(0);
    }

    [Fact]
    public async Task UploadAsync_WithInvalidEntityType_ShouldReturnFailure()
    {
        // Arrange
        var input = new SyncUploadInputDto
        {
            EntityType = "InvalidType",
            Entities = new List<JsonElement>(),
            OverwriteConflicts = false
        };

        // Act
        var result = await _syncService.UploadAsync(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("不支持的实体类型");
    }

    [Fact]
    public async Task UploadAsync_WithMixedResults_ShouldReportCorrectly()
    {
        // Arrange
        var existingHerbId = Guid.NewGuid();
        var existingHerb = new Herb
        {
            Id = existingHerbId,
            Name = "已存在药材",
            Unit = "g",
            Price = 50m,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Herbs.Add(existingHerb);
        await _dbContext.SaveChangesAsync();

        var newHerb = new Herb
        {
            Id = Guid.NewGuid(),
            Name = "新药材",
            Unit = "g",
            Price = 30m,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };

        var conflictHerb = new Herb
        {
            Id = existingHerbId,
            Name = "冲突药材",
            Unit = "g",
            Price = 40m,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow
        };

        var input = new SyncUploadInputDto
        {
            EntityType = "Herb",
            Entities = new List<JsonElement>
            {
                SerializeToJsonElement(newHerb),
                SerializeToJsonElement(conflictHerb)
            },
            OverwriteConflicts = false
        };

        // Act
        var result = await _syncService.UploadAsync(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(1);
        result.Data.ConflictCount.Should().Be(1);
    }

    #endregion

    #region 辅助方法

    private static JsonElement SerializeToJsonElement<T>(T obj)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        var json = JsonSerializer.Serialize(obj, options);
        return JsonDocument.Parse(json).RootElement.Clone();
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
