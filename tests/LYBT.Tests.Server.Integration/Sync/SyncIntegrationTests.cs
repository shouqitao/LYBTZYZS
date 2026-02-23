using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Entities.Common;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Sync;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Integration.Fixtures;

namespace LYBT.Tests.Server.Integration.Sync;

/// <summary>
/// 数据同步模块集成测试。
/// 覆盖6个API端点: entity-types, metadata, compare, upload, download, delete。
/// 使用Checksum比对策略验证同步逻辑。
/// </summary>
[Collection("ServerIntegration")]
public class SyncIntegrationTests
{
    private readonly WebApiFixture _fixture;
    private const string BaseUrl = "/api/v1/sync";

    /// <summary>JSON选项: 枚举用字符串序列化 (API返回 "ServerOnly" 而非 0)</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SyncIntegrationTests(WebApiFixture fixture)
    {
        _fixture = fixture;
    }

    #region GetEntityTypes

    [Fact]
    public async Task GetEntityTypes_WithAuth_ShouldReturn3Types()
    {
        // Act
        var response = await _fixture.AdminClient.GetAsync($"{BaseUrl}/entity-types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<string>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().HaveCount(3);
        result.Data.Should().Contain("Herb");
        result.Data.Should().Contain("Patient");
        result.Data.Should().Contain("Formula");
    }

    [Fact]
    public async Task GetEntityTypes_WithoutAuth_ShouldReturn401()
    {
        // Act
        var response = await _fixture.AnonymousClient.GetAsync($"{BaseUrl}/entity-types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetEntityTypes_WithDoctorAuth_ShouldReturn200()
    {
        // Arrange - DoctorOrAdmin策略允许Doctor访问
        // Act
        var response = await _fixture.DoctorClient.GetAsync($"{BaseUrl}/entity-types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region GetMetadata

    [Fact]
    public async Task GetMetadata_Herb_ShouldReturnMetadataWithChecksum()
    {
        // Arrange - 先创建测试药材
        await SeedHerb("元数据测试黄芪", "YDSJ");

        // Act
        var response = await _fixture.AdminClient.GetAsync($"{BaseUrl}/metadata?entityType=Herb");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<SyncMetadataDto>>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var metadata = result.Data!.First();
        metadata.EntityId.Should().NotBeEmpty();
        metadata.Checksum.Should().NotBeNullOrEmpty();
        metadata.Checksum.Should().HaveLength(64); // SHA256 hex string
    }

    [Fact]
    public async Task GetMetadata_InvalidType_ShouldReturnBusinessFail()
    {
        // Act
        var response = await _fixture.AdminClient.GetAsync($"{BaseUrl}/metadata?entityType=InvalidType");

        // Assert - BusinessFail 返回 422 + Success=false
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetMetadata_EmptyType_ShouldReturn400()
    {
        // Act
        var response = await _fixture.AdminClient.GetAsync($"{BaseUrl}/metadata?entityType=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Compare

    [Fact]
    public async Task Compare_EmptyLocal_ShouldReturnServerOnlyDiffs()
    {
        // Arrange - 确保服务器有数据
        await SeedHerb("比对测试黄芪", "BDCS");

        var input = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>()
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/compare", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncCompareResultDto>>(JsonOptions);
        result!.Success.Should().BeTrue();
        result.Data!.Diffs.Should().Contain(d => d.DiffType == SyncDiffType.ServerOnly);
        result.Data.ServerTotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Compare_LocalOnlyEntity_ShouldReturnLocalOnlyDiff()
    {
        // Arrange - 本地有但服务器没有的实体ID
        var localOnlyId = Guid.NewGuid();
        var input = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>
            {
                new()
                {
                    EntityId = localOnlyId,
                    Checksum = "0000000000000000000000000000000000000000000000000000000000000000",
                    LastModifiedAt = DateTime.UtcNow
                }
            }
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/compare", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncCompareResultDto>>(JsonOptions);
        result!.Success.Should().BeTrue();
        result.Data!.Diffs.Should().Contain(d =>
            d.EntityId == localOnlyId && d.DiffType == SyncDiffType.LocalOnly);
    }

    [Fact]
    public async Task Compare_IdenticalChecksum_ShouldNotAppearInDiffs()
    {
        // Arrange - 获取服务器端Checksum
        await SeedHerb("Checksum一致测试", "CSYZ");

        var metaResponse = await _fixture.AdminClient.GetAsync($"{BaseUrl}/metadata?entityType=Herb");
        var metaResult = await metaResponse.Content.ReadFromJsonAsync<ApiResponse<List<SyncMetadataDto>>>();
        var serverMeta = metaResult!.Data!.First();

        var input = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>
            {
                new()
                {
                    EntityId = serverMeta.EntityId,
                    Checksum = serverMeta.Checksum,
                    LastModifiedAt = DateTime.UtcNow
                }
            }
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/compare", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncCompareResultDto>>(JsonOptions);
        result!.Success.Should().BeTrue();
        // Checksum相同的实体不应出现在Modified差异中
        result.Data!.Diffs.Should().NotContain(d =>
            d.EntityId == serverMeta.EntityId && d.DiffType == SyncDiffType.Modified);
    }

    [Fact]
    public async Task Compare_ModifiedChecksum_ShouldReturnModifiedDiff()
    {
        // Arrange - 使用错误的Checksum模拟本地修改
        await SeedHerb("修改比对测试", "XGBD");

        var metaResponse = await _fixture.AdminClient.GetAsync($"{BaseUrl}/metadata?entityType=Herb");
        var metaResult = await metaResponse.Content.ReadFromJsonAsync<ApiResponse<List<SyncMetadataDto>>>();
        var serverMeta = metaResult!.Data!.First();

        var input = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>
            {
                new()
                {
                    EntityId = serverMeta.EntityId,
                    Checksum = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
                    LastModifiedAt = DateTime.UtcNow
                }
            }
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/compare", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncCompareResultDto>>(JsonOptions);
        result!.Success.Should().BeTrue();
        result.Data!.Diffs.Should().Contain(d =>
            d.EntityId == serverMeta.EntityId && d.DiffType == SyncDiffType.Modified);
    }

    [Fact]
    public async Task Compare_InvalidEntityType_ShouldReturnBusinessFail()
    {
        // Arrange
        var input = new SyncCompareInputDto
        {
            EntityType = "InvalidType",
            LocalEntities = new List<LocalEntityMetadata>()
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/compare", input);

        // Assert - BusinessFail 返回 422 + Success=false
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeFalse();
    }

    #endregion

    #region Upload

    [Fact]
    public async Task Upload_NewHerb_ShouldCreateAndReturnSuccess()
    {
        // Arrange
        var newHerbId = Guid.NewGuid();
        var newHerb = new
        {
            id = newHerbId,
            name = "上传新药材",
            pinYinCode = "SCXYC",
            category = "补气药",
            origin = "甘肃",
            spec = "统货",
            unit = "克",
            price = 1.5,
            costPrice = 0.8,
            effect = "补气升阳",
            usage = "煎服",
            status = 1,
            isDeleted = false,
            createdAt = DateTime.UtcNow
        };

        var input = new SyncUploadInputDto
        {
            EntityType = "Herb",
            Entities = new List<JsonElement> { JsonSerializer.SerializeToElement(newHerb) },
            OverwriteConflicts = false
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/upload", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncUploadResultDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(1);
        result.Data.ErrorCount.Should().Be(0);

        // 验证可下载
        var downloadInput = new SyncDownloadInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { newHerbId }
        };
        var downloadResponse = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/download", downloadInput);
        var downloadResult = await downloadResponse.Content.ReadFromJsonAsync<ApiResponse<SyncDownloadResultDto>>();
        downloadResult!.Data!.Count.Should().Be(1);
    }

    [Fact]
    public async Task Upload_NewPatient_ShouldCreateAndReturnSuccess()
    {
        // Arrange
        var newPatientId = Guid.NewGuid();
        var newPatient = new
        {
            id = newPatientId,
            name = "上传测试患者",
            pinYinCode = "SCCS",
            gender = 1,
            birthDate = "1990-05-15",
            phoneNumber = "13900139000",
            address = "同步测试地址",
            status = 1,
            isDeleted = false,
            createdAt = DateTime.UtcNow
        };

        var input = new SyncUploadInputDto
        {
            EntityType = "Patient",
            Entities = new List<JsonElement> { JsonSerializer.SerializeToElement(newPatient) },
            OverwriteConflicts = false
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/upload", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncUploadResultDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task Upload_OverwriteConflicts_ShouldUpdateExisting()
    {
        // Arrange - 先种子一个药材
        var herbId = await SeedHerb("覆盖冲突测试", "FGCT");

        // 上传相同ID但不同数据
        var updatedHerb = new
        {
            id = herbId,
            name = "覆盖后的药材名",
            pinYinCode = "FGHYCM",
            category = "清热药",
            unit = "克",
            price = 2.0,
            status = 1,
            isDeleted = false,
            createdAt = DateTime.UtcNow
        };

        var input = new SyncUploadInputDto
        {
            EntityType = "Herb",
            Entities = new List<JsonElement> { JsonSerializer.SerializeToElement(updatedHerb) },
            OverwriteConflicts = true
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/upload", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncUploadResultDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task Upload_EmptyEntities_ShouldReturn400()
    {
        // Arrange
        var input = new SyncUploadInputDto
        {
            EntityType = "Herb",
            Entities = new List<JsonElement>()
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/upload", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Upload_InvalidEntityType_ShouldReturnBusinessFail()
    {
        // Arrange
        var input = new SyncUploadInputDto
        {
            EntityType = "InvalidType",
            Entities = new List<JsonElement>
            {
                JsonSerializer.SerializeToElement(new { id = Guid.NewGuid(), name = "test" })
            }
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/upload", input);

        // Assert - BusinessFail 返回 422 + Success=false
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeFalse();
    }

    #endregion

    #region Download

    [Fact]
    public async Task Download_ExistingHerb_ShouldReturnEntityData()
    {
        // Arrange - 种子并获取ID
        var herbId = await SeedHerb("下载测试黄芪", "XZCS");

        var input = new SyncDownloadInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { herbId }
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/download", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncDownloadResultDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.Count.Should().Be(1);
        result.Data.Entities.Should().HaveCount(1);
        result.Data.EntityType.Should().Be("Herb");
    }

    [Fact]
    public async Task Download_NonExistentId_ShouldReturnEmpty()
    {
        // Arrange
        var input = new SyncDownloadInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/download", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncDownloadResultDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.Count.Should().Be(0);
    }

    [Fact]
    public async Task Download_EmptyIdList_ShouldReturn400()
    {
        // Arrange
        var input = new SyncDownloadInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid>()
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/download", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Download_Patient_ShouldReturnPatientData()
    {
        // Arrange
        var patientId = await SeedPatient("下载测试患者", "XZCS");

        var input = new SyncDownloadInputDto
        {
            EntityType = "Patient",
            EntityIds = new List<Guid> { patientId }
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/download", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncDownloadResultDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.Count.Should().Be(1);
        result.Data.EntityType.Should().Be("Patient");
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_FormulaNoReferences_ShouldSucceed()
    {
        // Arrange - 上传一个Formula
        var formulaId = Guid.NewGuid();
        var formula = new
        {
            id = formulaId,
            name = "删除测试方剂",
            category = "补益剂",
            effect = "测试功效",
            status = 1,
            isDeleted = false,
            createdAt = DateTime.UtcNow
        };

        var uploadInput = new SyncUploadInputDto
        {
            EntityType = "Formula",
            Entities = new List<JsonElement> { JsonSerializer.SerializeToElement(formula) },
            OverwriteConflicts = false
        };
        await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/upload", uploadInput);

        // Act
        var deleteInput = new SyncDeleteInputDto
        {
            EntityType = "Formula",
            EntityIds = new List<Guid> { formulaId }
        };
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/delete", deleteInput);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncDeleteResultDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.Success.Should().Contain(formulaId);
        result.Data.Rejected.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_HerbWithPrescriptionReference_ShouldReject()
    {
        // Arrange - 种子药材并创建关联处方
        var herbId = await SeedHerbWithPrescriptionReference();

        // Act
        var deleteInput = new SyncDeleteInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { herbId }
        };
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/delete", deleteInput);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SyncDeleteResultDto>>();
        result!.Success.Should().BeTrue();
        // 有引用的药材应该被拒绝删除
        result.Data!.Rejected.Should().Contain(r => r.EntityId == herbId);
        result.Data.Rejected.First(r => r.EntityId == herbId).Reason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Delete_EmptyIdList_ShouldReturn400()
    {
        // Arrange
        var input = new SyncDeleteInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid>()
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/delete", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_InvalidType_ShouldReturnBusinessFail()
    {
        // Arrange
        var input = new SyncDeleteInputDto
        {
            EntityType = "InvalidType",
            EntityIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/delete", input);

        // Assert - BusinessFail 返回 422 + Success=false
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Success.Should().BeFalse();
    }

    #endregion

    #region Full Sync Workflow

    [Fact]
    public async Task FullSyncWorkflow_CompareUploadDownload_ShouldWork()
    {
        // 1. 比对 - 发送空本地列表，获取服务器端所有差异
        var compareInput = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>()
        };
        var compareResponse = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/compare", compareInput);
        compareResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. 上传 - 创建新药材
        var newId = Guid.NewGuid();
        var uploadInput = new SyncUploadInputDto
        {
            EntityType = "Herb",
            Entities = new List<JsonElement>
            {
                JsonSerializer.SerializeToElement(new
                {
                    id = newId,
                    name = "全流程同步测试",
                    pinYinCode = "QLCTB",
                    category = "补气药",
                    unit = "克",
                    price = 1.0,
                    status = 1,
                    isDeleted = false,
                    createdAt = DateTime.UtcNow
                })
            },
            OverwriteConflicts = false
        };
        var uploadResponse = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/upload", uploadInput);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<SyncUploadResultDto>>();
        uploadResult!.Data!.SuccessCount.Should().Be(1);

        // 3. 下载 - 验证刚上传的数据可以下载
        var downloadInput = new SyncDownloadInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { newId }
        };
        var downloadResponse = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/download", downloadInput);
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var downloadResult = await downloadResponse.Content.ReadFromJsonAsync<ApiResponse<SyncDownloadResultDto>>();
        downloadResult!.Data!.Count.Should().Be(1);

        // 4. 再次比对 - 使用正确Checksum，应无差异
        var metaResponse = await _fixture.AdminClient.GetAsync($"{BaseUrl}/metadata?entityType=Herb");
        var metaResult = await metaResponse.Content.ReadFromJsonAsync<ApiResponse<List<SyncMetadataDto>>>();
        var uploadedMeta = metaResult!.Data!.FirstOrDefault(m => m.EntityId == newId);
        uploadedMeta.Should().NotBeNull();

        var finalCompare = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>
            {
                new()
                {
                    EntityId = newId,
                    Checksum = uploadedMeta!.Checksum,
                    LastModifiedAt = DateTime.UtcNow
                }
            }
        };
        var finalResponse = await _fixture.AdminClient.PostAsJsonAsync($"{BaseUrl}/compare", finalCompare);
        var finalResult = await finalResponse.Content.ReadFromJsonAsync<ApiResponse<SyncCompareResultDto>>(JsonOptions);
        finalResult!.Data!.Diffs.Should().NotContain(d =>
            d.EntityId == newId && d.DiffType == SyncDiffType.Modified);
    }

    #endregion

    #region Helpers

    /// <summary>通过API创建药材并返回ID</summary>
    private async Task<Guid> SeedHerb(string name, string pinYinCode)
    {
        var herbId = Guid.NewGuid();
        await _fixture.SeedAsync(async db =>
        {
            db.Set<Herb>().Add(new Herb
            {
                Id = herbId,
                Name = name,
                PinYinCode = pinYinCode,
                Category = "补气药",
                Unit = "克",
                Price = 0.5m,
                Status = CommonStatus.Enabled,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });
        return herbId;
    }

    /// <summary>通过DB种子患者并返回ID</summary>
    private async Task<Guid> SeedPatient(string name, string pinYinCode)
    {
        var patientId = Guid.NewGuid();
        await _fixture.SeedAsync(async db =>
        {
            db.Set<Patient>().Add(new Patient
            {
                Id = patientId,
                Name = name,
                PinYinCode = pinYinCode,
                Gender = Gender.Male,
                Status = CommonStatus.Enabled,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });
        return patientId;
    }

    /// <summary>种子药材+含该药材的处方(建立引用关系)</summary>
    private async Task<Guid> SeedHerbWithPrescriptionReference()
    {
        var herbId = Guid.NewGuid();
        await _fixture.SeedAsync(async db =>
        {
            // 创建药材
            var herb = new Herb
            {
                Id = herbId,
                Name = "有引用的药材",
                PinYinCode = "YYDYC",
                Category = "清热药",
                Unit = "克",
                Price = 1.0m,
                Status = CommonStatus.Enabled,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Set<Herb>().Add(herb);

            // 创建医案+处方，处方中引用该药材
            var caseId = Guid.NewGuid();

            // 先创建一个Patient使外键有效
            var patientId = Guid.NewGuid();
            db.Set<Patient>().Add(new Patient
            {
                Id = patientId,
                Name = "引用测试患者",
                PinYinCode = "YYCS",
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            var medicalCase = new LYBT.Entities.MedicalCases.MedicalCase
            {
                Id = caseId,
                PatientId = patientId,
                UserId = WebApiFixture.DoctorUserId,
                CaseNumber = $"MC-{DateTime.UtcNow:yyyyMMdd}-REF",
                CaseStatus = LYBT.Shared.Models.Enums.MedicalCaseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = WebApiFixture.AdminUserId
            };
            db.Set<LYBT.Entities.MedicalCases.MedicalCase>().Add(medicalCase);

            var prescription = new LYBT.Entities.Prescriptions.Prescription
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = caseId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = WebApiFixture.AdminUserId
            };
            db.Set<LYBT.Entities.Prescriptions.Prescription>().Add(prescription);

            var prescriptionItem = new LYBT.Entities.Prescriptions.PrescriptionItem
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescription.Id,
                HerbId = herbId,
                HerbName = "有引用的药材",
                Dosage = 10,
                Unit = "克"
            };
            db.Set<LYBT.Entities.Prescriptions.PrescriptionItem>().Add(prescriptionItem);

            await db.SaveChangesAsync();
        });
        return herbId;
    }

    #endregion
}
