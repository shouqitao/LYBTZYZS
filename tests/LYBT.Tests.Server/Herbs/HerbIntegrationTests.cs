using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Herbs;

/// <summary>
/// 药材管理模块集成测试。
/// 验证完整HTTP管线: HerbsController -> HerbService -> HerbRepository -> DB。
/// 授权策略: DoctorOrAdmin。
/// 特点: 价格字段(Price/CostPrice)、分类(Category)、状态切换(ToggleStatus)。
/// </summary>
public sealed class HerbIntegrationTests : IntegrationTestBase
{
    private const string BaseUrl = "/api/v1/herbs";

    public HerbIntegrationTests(ServerFixture fixture) : base(fixture) { }

    #region Helpers

    private static HerbInputDto CreateHerbInput(string? nameSuffix = null) => new()
    {
        Name = "测试药材_" + (nameSuffix ?? Guid.NewGuid().ToString("N")[..6]),
        Unit = "克",
        Price = 15.50m,
        Category = "补气药",
        Origin = "云南",
        Effect = "补中益气",
        Usage = "煎服"
    };

    private async Task<(Guid id, HerbDetailDto dto)> CreateHerbAsync(
        HttpClient? client = null, HerbInputDto? input = null)
    {
        client ??= await LoginAsAdminAsync();
        input ??= CreateHerbInput();

        var response = await client.PostAsJsonAsync(BaseUrl, input);
        response.IsSuccessStatusCode.Should().BeTrue(
            $"创建药材应成功, 实际: {response.StatusCode}");

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        return (body!.Data!.Id, body.Data);
    }

    #endregion

    #region Create Herb

    [Fact]
    public async Task CreateHerb_WithValidData_ShouldPersist()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var request = CreateHerbInput();
        request.CostPrice = 8.00m;
        request.Spec = "片状";

        // Act
        var response = await admin.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Id.Should().NotBe(Guid.Empty);
        body.Data.Name.Should().Be(request.Name);
        body.Data.Price.Should().Be(15.50m);
        body.Data.CostPrice.Should().Be(8.00m);
        body.Data.Unit.Should().Be("克");
        body.Data.Category.Should().Be("补气药");
        body.Data.Origin.Should().Be("云南");
        body.Data.Effect.Should().Be("补中益气");
        // 注: 药材的PinYinCode由Service层按需生成，可能为null
    }

    [Fact]
    public async Task CreateHerb_WithMinimalData_ShouldSucceed()
    {
        // Arrange - 仅必填字段: Name, Unit, Price
        var admin = await LoginAsAdminAsync();
        var request = new HerbInputDto
        {
            Name = "极简药材_" + Guid.NewGuid().ToString("N")[..6],
            Unit = "克",
            Price = 10.00m
        };

        // Act
        var response = await admin.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Price.Should().Be(10.00m);
    }

    [Fact]
    public async Task CreateHerb_WithDoctorToken_ShouldSucceed()
    {
        // Arrange
        var doctor = await LoginAsDoctorAsync();
        var request = CreateHerbInput("Doctor创建");

        // Act
        var response = await doctor.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(
            "Doctor角色应能创建药材 (DoctorOrAdmin策略)");
    }

    #endregion

    #region Get Herbs

    [Fact]
    public async Task GetHerbs_ShouldReturnPagedList()
    {
        // Arrange
        await CreateHerbAsync();
        var admin = await LoginAsAdminAsync();

        // Act
        var response = await admin.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<HerbListDto>>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetHerbs_WithKeyword_ShouldFilter()
    {
        // Arrange
        var uniqueTag = "UniHerb" + Guid.NewGuid().ToString("N")[..4];
        var input = CreateHerbInput(uniqueTag);
        await CreateHerbAsync(input: input);
        var admin = await LoginAsAdminAsync();

        // Act
        var response = await admin.GetAsync($"{BaseUrl}?keyword={uniqueTag}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<HerbListDto>>>(JsonOptions);
        body!.Data!.Items.Should().Contain(h => h.Name.Contains(uniqueTag));
    }

    [Fact]
    public async Task GetHerbs_WithCategory_ShouldFilter()
    {
        // Arrange
        var input = CreateHerbInput();
        input.Category = "清热药_" + Guid.NewGuid().ToString("N")[..4];
        await CreateHerbAsync(input: input);
        var admin = await LoginAsAdminAsync();

        // Act
        var response = await admin.GetAsync($"{BaseUrl}?category={input.Category}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<HerbListDto>>>(JsonOptions);
        body!.Success.Should().BeTrue();
        // 分类筛选应返回结果（至少包含刚创建的）
        body.Data!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetHerb_ById_ShouldReturnDetail()
    {
        // Arrange
        var (herbId, _) = await CreateHerbAsync();
        var admin = await LoginAsAdminAsync();

        // Act
        var response = await admin.GetAsync($"{BaseUrl}/{herbId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        body!.Data!.Id.Should().Be(herbId);
    }

    [Fact]
    public async Task GetHerb_NonExistentId_ShouldReturn404()
    {
        var admin = await LoginAsAdminAsync();
        var response = await admin.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHerbs_InvalidPagination_ShouldReturn400()
    {
        var admin = await LoginAsAdminAsync();
        var response = await admin.GetAsync($"{BaseUrl}?page=0&pageSize=20");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Update Herb

    [Fact]
    public async Task UpdateHerb_ShouldModifyFields()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (herbId, _) = await CreateHerbAsync(client: admin);

        var updateRequest = new HerbInputDto
        {
            Id = herbId,
            Name = "更新后药材_" + Guid.NewGuid().ToString("N")[..4],
            Unit = "克",
            Price = 25.00m,
            CostPrice = 12.50m,
            Category = "补血药",
            Effect = "补血活血"
        };

        // Act
        var response = await admin.PutAsJsonAsync($"{BaseUrl}/{herbId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        body!.Data!.Price.Should().Be(25.00m);
        body.Data.CostPrice.Should().Be(12.50m);
        body.Data.Category.Should().Be("补血药");
        body.Data.Effect.Should().Be("补血活血");

        // Verify persistence
        var getResp = await admin.GetAsync($"{BaseUrl}/{herbId}");
        var fetched = await getResp.Content
            .ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        fetched!.Data!.Price.Should().Be(25.00m, "价格更新应已持久化");
    }

    #endregion

    #region Delete Herb

    [Fact]
    public async Task DeleteHerb_ShouldSoftDelete()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (herbId, _) = await CreateHerbAsync(client: admin);

        // Act
        var response = await admin.DeleteAsync($"{BaseUrl}/{herbId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await admin.GetAsync($"{BaseUrl}/{herbId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound, "软删除后应查不到");
    }

    #endregion

    #region Toggle Status

    [Fact]
    public async Task ToggleStatus_ShouldChangeHerbStatus()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (herbId, created) = await CreateHerbAsync(client: admin);
        var originalStatus = created.Status;

        // Act
        var response = await admin.PostAsync($"{BaseUrl}/{herbId}/toggle-status", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        body!.Data!.Status.Should().NotBe(originalStatus, "状态应已切换");
    }

    #endregion

    #region Restore Herb

    [Fact]
    public async Task RestoreHerb_ShouldMakeAccessibleAgain()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (herbId, _) = await CreateHerbAsync(client: admin);

        // 软删除
        await admin.DeleteAsync($"{BaseUrl}/{herbId}");
        var getAfterDel = await admin.GetAsync($"{BaseUrl}/{herbId}");
        getAfterDel.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Act
        var response = await admin.PostAsync($"{BaseUrl}/{herbId}/restore", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue();

        var getAfterRestore = await admin.GetAsync($"{BaseUrl}/{herbId}");
        getAfterRestore.StatusCode.Should().Be(HttpStatusCode.OK, "恢复后应能访问");
    }

    #endregion

    #region Batch Operations

    [Fact]
    public async Task BatchDelete_MultipleHerbs_ShouldSoftDeleteAll()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var ids = new List<Guid>();
        for (int i = 0; i < 3; i++)
        {
            var (id, _) = await CreateHerbAsync(client: admin);
            ids.Add(id);
        }

        // Act
        var response = await admin
            .PostAsJsonAsync($"{BaseUrl}/batch-delete", new { Ids = ids });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        foreach (var id in ids)
        {
            var get = await admin.GetAsync($"{BaseUrl}/{id}");
            get.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task BatchDelete_EmptyList_ShouldReturn400()
    {
        var admin = await LoginAsAdminAsync();
        var response = await admin
            .PostAsJsonAsync($"{BaseUrl}/batch-delete", new { Ids = new List<Guid>() });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Authorization

    [Fact]
    public async Task AnonymousRequest_ShouldReturn401()
    {
        var response = await AnonymousClient.GetAsync(BaseUrl);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Price Validation

    [Fact]
    public async Task CreateHerb_WithLowPrice_ShouldSucceed()
    {
        // Arrange - Price=1.00 (低价但有效)
        var admin = await LoginAsAdminAsync();
        var request = new HerbInputDto
        {
            Name = "低价药材_" + Guid.NewGuid().ToString("N")[..6],
            Unit = "克",
            Price = 1.00m
        };

        // Act
        var response = await admin.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        body!.Data!.Price.Should().Be(1.00m);
    }

    [Fact]
    public async Task UpdateHerb_PriceChange_ShouldPersist()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (herbId, _) = await CreateHerbAsync(client: admin);

        // Act - 更新价格
        var updateReq = new HerbInputDto
        {
            Id = herbId,
            Name = "改价药材_" + Guid.NewGuid().ToString("N")[..4],
            Unit = "克",
            Price = 99.99m,
            CostPrice = 50.00m
        };
        var response = await admin.PutAsJsonAsync($"{BaseUrl}/{herbId}", updateReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        body!.Data!.Price.Should().Be(99.99m);
        body.Data.CostPrice.Should().Be(50.00m);
    }

    #endregion

    #region Authorization (migrated from Structure B)

    [Fact]
    public async Task GetById_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var (herbId, _) = await CreateHerbAsync();

        // Act
        var response = await AnonymousClient.GetAsync($"{BaseUrl}/{herbId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var newHerb = new HerbInputDto
        {
            Name = "未认证测试药材",
            Unit = "克",
            Price = 1.0m
        };

        // Act
        var response = await AnonymousClient.PostAsJsonAsync(BaseUrl, newHerb);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var (herbId, _) = await CreateHerbAsync();
        var updatedHerb = new HerbInputDto
        {
            Id = herbId,
            Name = "未认证更新",
            Unit = "克",
            Price = 1.0m
        };

        // Act
        var response = await AnonymousClient.PutAsJsonAsync($"{BaseUrl}/{herbId}", updatedHerb);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var (herbId, _) = await CreateHerbAsync();

        // Act
        var response = await AnonymousClient.DeleteAsync($"{BaseUrl}/{herbId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BatchOperations_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var (herbId, _) = await CreateHerbAsync();
        var batchDto = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { herbId }
        };

        // Act
        var response = await AnonymousClient
            .PostAsJsonAsync($"{BaseUrl}/batch-delete", batchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Validation (migrated from Structure B)

    [Fact]
    public async Task CreateHerb_WithoutName_ShouldReturnBadRequest()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var invalidHerb = new HerbInputDto
        {
            Name = "", // 空名称
            Unit = "克",
            Price = 1.0m
        };

        // Act
        var response = await admin.PostAsJsonAsync(BaseUrl, invalidHerb);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Update/Delete Edge Cases (migrated from Structure B)

    [Fact]
    public async Task UpdateHerb_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var nonExistingId = Guid.NewGuid();
        var updatedHerb = new HerbInputDto
        {
            Id = nonExistingId,
            Name = "不存在的药材",
            Unit = "克",
            Price = 1.0m
        };

        // Act
        var response = await admin.PutAsJsonAsync($"{BaseUrl}/{nonExistingId}", updatedHerb);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteHerb_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await admin.DeleteAsync($"{BaseUrl}/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ToggleStatus_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await admin.PostAsync($"{BaseUrl}/{nonExistingId}/toggle-status", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestoreHerb_WithNonExistingId_ShouldReturn422()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await admin.PostAsync($"{BaseUrl}/{nonExistingId}/restore", null);

        // Assert - Restore 对不存在的ID返回 422 (BusinessFail)
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    #endregion

    #region Batch Enable/Disable (migrated from Structure B)

    [Fact]
    public async Task BatchEnable_WithValidIds_ShouldEnableMultiple()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (herbId, _) = await CreateHerbAsync(client: admin);
        var batchDto = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { herbId }
        };

        // Act
        var response = await admin.PostAsJsonAsync($"{BaseUrl}/batch-enable", batchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<ApiResponse<BatchOperationResultDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task BatchDisable_WithValidIds_ShouldDisableMultiple()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (herbId, _) = await CreateHerbAsync(client: admin);
        var batchDto = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { herbId }
        };

        // Act
        var response = await admin.PostAsJsonAsync($"{BaseUrl}/batch-disable", batchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<ApiResponse<BatchOperationResultDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    #endregion

    #region Export (migrated from Structure B)

    [Fact]
    public async Task Export_ShouldReturnExcelFile()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();

        // Act
        var response = await admin.GetAsync($"{BaseUrl}/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task ExportTemplate_ShouldReturnTemplateFile()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();

        // Act - import-template 需要认证
        var response = await admin.GetAsync($"{BaseUrl}/import-template");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task GetAllForExport_ShouldReturnAllHerbs()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();

        // Act
        var response = await admin.GetAsync($"{BaseUrl}/export-all");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<ApiResponse<List<HerbDetailDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    #endregion

    #region Reference Check (migrated from Structure B)

    [Fact]
    public async Task CheckReference_WithExistingId_ShouldReturnReferenceStatus()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (herbId, _) = await CreateHerbAsync(client: admin);

        // Act
        var response = await admin.GetAsync($"{BaseUrl}/{herbId}/check-reference");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<ApiResponse<HerbReferenceCheckDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task BatchCheckReference_WithValidIds_ShouldReturnResults()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var (herbId1, _) = await CreateHerbAsync(client: admin);
        var (herbId2, _) = await CreateHerbAsync(client: admin);
        var request = new HerbBatchCheckReferenceInputDto
        {
            HerbIds = new List<Guid> { herbId1, herbId2 }
        };

        // Act
        var response = await admin
            .PostAsJsonAsync($"{BaseUrl}/batch-check-reference", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<ApiResponse<List<HerbReferenceCheckDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task BatchCheckReference_WithEmptyIds_ShouldReturnBadRequest()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var request = new HerbBatchCheckReferenceInputDto
        {
            HerbIds = new List<Guid>()
        };

        // Act
        var response = await admin
            .PostAsJsonAsync($"{BaseUrl}/batch-check-reference", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
