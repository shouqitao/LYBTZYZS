using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Integration.Fixtures;

namespace LYBT.Tests.Server.Integration.Herbs;

/// <summary>
/// 药材管理模块集成测试。
/// 验证完整HTTP管线: HerbsController -> HerbService -> HerbRepository -> DB。
/// 授权策略: DoctorOrAdmin。
/// 特点: 价格字段(Price/CostPrice)、分类(Category)、状态切换(ToggleStatus)。
/// </summary>
[Collection("ServerIntegration")]
public class HerbIntegrationTests
{
    private readonly WebApiFixture _fixture;
    private const string BaseUrl = "/api/v1/herbs";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public HerbIntegrationTests(WebApiFixture fixture)
    {
        _fixture = fixture;
    }

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
        client ??= _fixture.AdminClient;
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
        var request = CreateHerbInput();
        request.CostPrice = 8.00m;
        request.Spec = "片状";

        // Act
        var response = await _fixture.AdminClient
            .PostAsJsonAsync(BaseUrl, request);

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
        var request = new HerbInputDto
        {
            Name = "极简药材_" + Guid.NewGuid().ToString("N")[..6],
            Unit = "克",
            Price = 10.00m
        };

        // Act
        var response = await _fixture.AdminClient
            .PostAsJsonAsync(BaseUrl, request);

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
        var request = CreateHerbInput("Doctor创建");

        // Act
        var response = await _fixture.DoctorClient
            .PostAsJsonAsync(BaseUrl, request);

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

        // Act
        var response = await _fixture.AdminClient.GetAsync(BaseUrl);

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

        // Act
        var response = await _fixture.AdminClient
            .GetAsync($"{BaseUrl}?keyword={uniqueTag}");

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

        // Act
        var response = await _fixture.AdminClient
            .GetAsync($"{BaseUrl}?category={input.Category}");

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

        // Act
        var response = await _fixture.AdminClient
            .GetAsync($"{BaseUrl}/{herbId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        body!.Data!.Id.Should().Be(herbId);
    }

    [Fact]
    public async Task GetHerb_NonExistentId_ShouldReturn404()
    {
        var response = await _fixture.AdminClient
            .GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHerbs_InvalidPagination_ShouldReturn400()
    {
        var response = await _fixture.AdminClient
            .GetAsync($"{BaseUrl}?page=0&pageSize=20");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Update Herb

    [Fact]
    public async Task UpdateHerb_ShouldModifyFields()
    {
        // Arrange
        var (herbId, _) = await CreateHerbAsync();

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
        var response = await _fixture.AdminClient
            .PutAsJsonAsync($"{BaseUrl}/{herbId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        body!.Data!.Price.Should().Be(25.00m);
        body.Data.CostPrice.Should().Be(12.50m);
        body.Data.Category.Should().Be("补血药");
        body.Data.Effect.Should().Be("补血活血");

        // Verify persistence
        var getResp = await _fixture.AdminClient.GetAsync($"{BaseUrl}/{herbId}");
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
        var (herbId, _) = await CreateHerbAsync();

        // Act
        var response = await _fixture.AdminClient
            .DeleteAsync($"{BaseUrl}/{herbId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await _fixture.AdminClient.GetAsync($"{BaseUrl}/{herbId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound, "软删除后应查不到");
    }

    #endregion

    #region Toggle Status

    [Fact]
    public async Task ToggleStatus_ShouldChangeHerbStatus()
    {
        // Arrange
        var (herbId, created) = await CreateHerbAsync();
        var originalStatus = created.Status;

        // Act
        var response = await _fixture.AdminClient
            .PostAsync($"{BaseUrl}/{herbId}/toggle-status", null);

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
        var (herbId, _) = await CreateHerbAsync();

        // 软删除
        await _fixture.AdminClient.DeleteAsync($"{BaseUrl}/{herbId}");
        var getAfterDel = await _fixture.AdminClient.GetAsync($"{BaseUrl}/{herbId}");
        getAfterDel.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Act
        var response = await _fixture.AdminClient
            .PostAsync($"{BaseUrl}/{herbId}/restore", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue();

        var getAfterRestore = await _fixture.AdminClient.GetAsync($"{BaseUrl}/{herbId}");
        getAfterRestore.StatusCode.Should().Be(HttpStatusCode.OK, "恢复后应能访问");
    }

    #endregion

    #region Batch Operations

    [Fact]
    public async Task BatchDelete_MultipleHerbs_ShouldSoftDeleteAll()
    {
        // Arrange
        var ids = new List<Guid>();
        for (int i = 0; i < 3; i++)
        {
            var (id, _) = await CreateHerbAsync();
            ids.Add(id);
        }

        // Act
        var response = await _fixture.AdminClient
            .PostAsJsonAsync($"{BaseUrl}/batch-delete", new { Ids = ids });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        foreach (var id in ids)
        {
            var get = await _fixture.AdminClient.GetAsync($"{BaseUrl}/{id}");
            get.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task BatchDelete_EmptyList_ShouldReturn400()
    {
        var response = await _fixture.AdminClient
            .PostAsJsonAsync($"{BaseUrl}/batch-delete", new { Ids = new List<Guid>() });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Authorization

    [Fact]
    public async Task AnonymousRequest_ShouldReturn401()
    {
        var response = await _fixture.AnonymousClient.GetAsync(BaseUrl);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Price Validation

    [Fact]
    public async Task CreateHerb_WithLowPrice_ShouldSucceed()
    {
        // Arrange - Price=1.00 (低价但有效)
        var request = new HerbInputDto
        {
            Name = "低价药材_" + Guid.NewGuid().ToString("N")[..6],
            Unit = "克",
            Price = 1.00m
        };

        // Act
        var response = await _fixture.AdminClient
            .PostAsJsonAsync(BaseUrl, request);

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
        var (herbId, _) = await CreateHerbAsync();

        // Act - 更新价格
        var updateReq = new HerbInputDto
        {
            Id = herbId,
            Name = "改价药材_" + Guid.NewGuid().ToString("N")[..4],
            Unit = "克",
            Price = 99.99m,
            CostPrice = 50.00m
        };
        var response = await _fixture.AdminClient
            .PutAsJsonAsync($"{BaseUrl}/{herbId}", updateReq);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        body!.Data!.Price.Should().Be(99.99m);
        body.Data.CostPrice.Should().Be(50.00m);
    }

    #endregion
}
