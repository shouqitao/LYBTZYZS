using FluentAssertions;
using LYBT.Entities.Herbs;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Module.Sync.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Sync;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.WebAPI.IntegrationTests.Controllers;

/// <summary>
/// SyncController 集成测试
/// 测试 6 个 API 端点: entity-types, metadata, compare, upload, download, delete
/// OpenSpec: implement-data-sync
/// </summary>
public class SyncControllerIntegrationTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    private const string BaseUrl = "/api/v1/sync";

    // 测试数据ID
    private Guid _testHerbId;
    private Guid _testPatientId;

    public SyncControllerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    protected override void SeedBasicTestData(AppDbContext context)
    {
        base.SeedBasicTestData(context);

        // 创建测试药材数据
        _testHerbId = Guid.NewGuid();
        var testHerb = new Herb
        {
            Id = _testHerbId,
            Name = "同步测试黄芪",
            PinYinCode = "TBHQ",
            Category = "补气药",
            Origin = "甘肃",
            Spec = "统货",
            Unit = "克",
            Price = 0.5m,
            CostPrice = 0.3m,
            Effect = "补气升阳，益卫固表",
            Usage = "煎服，9-30g",
            Status = CommonStatus.Enabled,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Set<Herb>().Add(testHerb);

        // 创建测试患者数据
        _testPatientId = Guid.NewGuid();
        var testPatient = new Patient
        {
            Id = _testPatientId,
            Name = "同步测试患者",
            PinYinCode = "TBHZ",
            Gender = Gender.Male,
            BirthDate = new DateTime(1990, 1, 1),
            PhoneNumber = "13800138000",
            Address = "测试地址",
            Status = CommonStatus.Enabled,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Set<Patient>().Add(testPatient);

        context.SaveChanges();
    }

    #region GetEntityTypes Tests

    [Fact]
    public async Task GetEntityTypes_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync($"{BaseUrl}/entity-types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetEntityTypes_WithAuthentication_ShouldReturnSupportedTypes()
    {
        // Act
        var response = await Client.GetAsync($"{BaseUrl}/entity-types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<string>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(3);
        result.Data.Should().Contain("Herb");
        result.Data.Should().Contain("Patient");
        result.Data.Should().Contain("Formula");

        _output.WriteLine($"Supported entity types: {string.Join(", ", result.Data!)}");
    }

    #endregion

    #region GetMetadata Tests

    [Fact]
    public async Task GetMetadata_WithValidEntityType_ShouldReturnMetadata()
    {
        // Act
        var response = await Client.GetAsync($"{BaseUrl}/metadata?entityType=Herb");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<SyncMetadataDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCountGreaterThan(0);

        // 验证元数据结构
        var metadata = result.Data!.First();
        metadata.EntityId.Should().NotBeEmpty();
        metadata.Checksum.Should().NotBeNullOrEmpty();
        metadata.Checksum.Length.Should().Be(64); // SHA256 hex string

        _output.WriteLine($"Herb metadata count: {result.Data.Count}");
    }

    [Fact]
    public async Task GetMetadata_WithInvalidEntityType_ShouldReturnBadRequest()
    {
        // Act
        var response = await Client.GetAsync($"{BaseUrl}/metadata?entityType=InvalidType");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("不支持的实体类型");
    }

    [Fact]
    public async Task GetMetadata_WithEmptyEntityType_ShouldReturnBadRequest()
    {
        // Act
        var response = await Client.GetAsync($"{BaseUrl}/metadata?entityType=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    #endregion

    #region Compare Tests

    [Fact]
    public async Task Compare_WithLocalOnlyEntity_ShouldReturnLocalOnlyDiff()
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
                    Checksum = "local-only-checksum-12345678901234567890123456789012",
                    LastModifiedAt = DateTime.UtcNow
                }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseUrl}/compare", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncCompareResultDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        // 应该检测到 LocalOnly（本地有但服务器没有）
        var localOnlyDiffs = result.Data!.Diffs.Where(d => d.DiffType == SyncDiffType.LocalOnly).ToList();
        localOnlyDiffs.Should().Contain(d => d.EntityId == localEntityId);

        _output.WriteLine($"Compare result: LocalOnly={localOnlyDiffs.Count}, ServerOnly={result.Data.Diffs.Count(d => d.DiffType == SyncDiffType.ServerOnly)}, Modified={result.Data.Diffs.Count(d => d.DiffType == SyncDiffType.Modified)}");
    }

    [Fact]
    public async Task Compare_WithServerOnlyEntity_ShouldReturnServerOnlyDiff()
    {
        // Arrange - 发送空的本地列表，服务器上的数据会被识别为 ServerOnly
        var input = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>()
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseUrl}/compare", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncCompareResultDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Diffs.Should().Contain(d => d.DiffType == SyncDiffType.ServerOnly);

        _output.WriteLine($"ServerOnly entities count: {result.Data.Diffs.Count(d => d.DiffType == SyncDiffType.ServerOnly)}");
    }

    [Fact]
    public async Task Compare_WithIdenticalChecksum_ShouldReturnNoDiff()
    {
        // Arrange - 首先获取服务器端的 Checksum
        var metadataResponse = await Client.GetAsync($"{BaseUrl}/metadata?entityType=Herb");
        var metadataResult = await metadataResponse.Content.ReadFromJsonAsync<ApiResponse<List<SyncMetadataDto>>>();
        var serverMetadata = metadataResult!.Data!.First();

        // 使用相同的 Checksum 发送比对请求
        var input = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>
            {
                new()
                {
                    EntityId = serverMetadata.EntityId,
                    Checksum = serverMetadata.Checksum, // 使用服务器端相同的 Checksum
                    LastModifiedAt = DateTime.UtcNow
                }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseUrl}/compare", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncCompareResultDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        // 相同 Checksum 的实体不应该出现在差异列表中
        result.Data!.Diffs.Should().NotContain(d => d.EntityId == serverMetadata.EntityId && d.DiffType == SyncDiffType.Modified);
    }

    [Fact]
    public async Task Compare_WithInvalidEntityType_ShouldReturnBadRequest()
    {
        // Arrange
        var input = new SyncCompareInputDto
        {
            EntityType = "InvalidType",
            LocalEntities = new List<LocalEntityMetadata>()
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseUrl}/compare", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Download Tests

    [Fact]
    public async Task Download_WithExistingEntityIds_ShouldReturnEntities()
    {
        // Arrange - 获取服务器上存在的实体ID
        var metadataResponse = await Client.GetAsync($"{BaseUrl}/metadata?entityType=Herb");
        var metadataResult = await metadataResponse.Content.ReadFromJsonAsync<ApiResponse<List<SyncMetadataDto>>>();
        var existingEntityId = metadataResult!.Data!.First().EntityId;

        var input = new SyncDownloadInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { existingEntityId }
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseUrl}/download", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncDownloadResultDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(1);
        result.Data.Entities.Should().HaveCount(1);

        _output.WriteLine($"Downloaded {result.Data.Count} entities");
    }

    [Fact]
    public async Task Download_WithNonExistentEntityIds_ShouldReturnEmptyList()
    {
        // Arrange
        var input = new SyncDownloadInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { Guid.NewGuid() } // 不存在的ID
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseUrl}/download", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncDownloadResultDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Count.Should().Be(0);
    }

    [Fact]
    public async Task Download_WithEmptyEntityIds_ShouldReturnBadRequest()
    {
        // Arrange
        var input = new SyncDownloadInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid>()
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseUrl}/download", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("不能为空");
    }

    #endregion

    #region Upload Tests

    [Fact]
    public async Task Upload_WithNewEntity_ShouldCreateEntity()
    {
        // Arrange - 创建一个新的 Herb 实体
        var newHerbId = Guid.NewGuid();
        var newHerb = new
        {
            id = newHerbId,
            name = "上传测试药材",
            pinYinCode = "SCCS",
            category = "补气药",
            origin = "测试产地",
            spec = "统货",
            unit = "克",
            price = 1.0m,
            costPrice = 0.5m,
            effect = "测试功效",
            usage = "测试用法",
            status = 1, // Enabled
            isDeleted = false,
            createdAt = DateTime.UtcNow
        };

        var input = new SyncUploadInputDto
        {
            EntityType = "Herb",
            Entities = new List<JsonElement>
            {
                JsonSerializer.SerializeToElement(newHerb)
            },
            OverwriteConflicts = false
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseUrl}/upload", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncUploadResultDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.SuccessCount.Should().Be(1);
        result.Data.ErrorCount.Should().Be(0);

        _output.WriteLine($"Upload result: Success={result.Data.SuccessCount}, Conflict={result.Data.ConflictCount}, Error={result.Data.ErrorCount}");
    }

    [Fact]
    public async Task Upload_WithEmptyEntities_ShouldReturnBadRequest()
    {
        // Arrange
        var input = new SyncUploadInputDto
        {
            EntityType = "Herb",
            Entities = new List<JsonElement>()
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseUrl}/upload", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("不能为空");
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_FormulaWithNoReferences_ShouldSoftDelete()
    {
        // Arrange - Formula 没有引用检查，可以直接删除
        // 先创建一个测试用的 Formula
        var newFormulaId = Guid.NewGuid();
        var newFormula = new
        {
            id = newFormulaId,
            name = "删除测试方剂",
            category = "补益剂",
            effect = "测试功效",
            usage = "测试用法",
            status = 1,
            isDeleted = false,
            createdAt = DateTime.UtcNow
        };

        var uploadInput = new SyncUploadInputDto
        {
            EntityType = "Formula",
            Entities = new List<JsonElement> { JsonSerializer.SerializeToElement(newFormula) },
            OverwriteConflicts = false
        };
        await Client.PostAsJsonAsync($"{BaseUrl}/upload", uploadInput);

        // Act - 删除刚创建的 Formula
        var deleteInput = new SyncDeleteInputDto
        {
            EntityType = "Formula",
            EntityIds = new List<Guid> { newFormulaId }
        };
        var response = await Client.PostAsJsonAsync($"{BaseUrl}/delete", deleteInput);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncDeleteResultDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Success.Should().Contain(newFormulaId);
        result.Data.Rejected.Should().BeEmpty();

        _output.WriteLine($"Delete result: Success={result.Data.Success.Count}, Rejected={result.Data.Rejected.Count}");
    }

    [Fact]
    public async Task Delete_WithEmptyEntityIds_ShouldReturnBadRequest()
    {
        // Arrange
        var input = new SyncDeleteInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid>()
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseUrl}/delete", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("不能为空");
    }

    [Fact]
    public async Task Delete_WithInvalidEntityType_ShouldReturnBadRequest()
    {
        // Arrange
        var input = new SyncDeleteInputDto
        {
            EntityType = "InvalidType",
            EntityIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseUrl}/delete", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
