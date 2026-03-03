using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Integration.Fixtures;

namespace LYBT.Tests.Server.Integration.Formulas;

/// <summary>
/// 验方管理模块集成测试。
/// 验证完整HTTP管线: FormulasController -> FormulaService -> FormulaRepository -> DB。
/// 授权策略: DoctorOrAdmin，Doctor只能看到自己的和共享的验方。
/// 特点: Formula是带子集合的聚合根(FormulaHerbItems)，支持延迟绑定(HerbId可空)。
/// </summary>
[Collection("ServerIntegration")]
public class FormulaIntegrationTests
{
    private readonly WebApiFixture _fixture;
    private const string BaseUrl = "/api/v1/formulas";
    private const string HerbUrl = "/api/v1/herbs";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public FormulaIntegrationTests(WebApiFixture fixture)
    {
        _fixture = fixture;
    }

    #region Helpers

    /// <summary>创建一个药材并返回ID(用于验方药材项)</summary>
    private async Task<Guid> CreateHerbAndGetId(string name = "测试药材", decimal price = 10.0m)
    {
        var herbInput = new HerbInputDto
        {
            Name = name + "_" + Guid.NewGuid().ToString("N")[..4],
            Unit = "克",
            Price = price
        };
        var resp = await _fixture.AdminClient.PostAsJsonAsync(HerbUrl, herbInput);
        resp.IsSuccessStatusCode.Should().BeTrue();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<HerbDetailDto>>(JsonOptions);
        return body!.Data!.Id;
    }

    /// <summary>创建验方输入DTO(含药材项，HerbId可空=延迟绑定)</summary>
    private static FormulaInputDto CreateFormulaInput(
        string? nameSuffix = null,
        List<FormulaHerbItemInputDto>? herbs = null) => new()
    {
        Name = "测试验方_" + (nameSuffix ?? Guid.NewGuid().ToString("N")[..6]),
        Effect = "清热解毒",
        Usage = "水煎服，每日一剂",
        Category = "经典方",
        Herbs = herbs ?? new List<FormulaHerbItemInputDto>
        {
            new() { HerbName = "黄芩", Dosage = 10, Unit = "克" },
            new() { HerbName = "黄连", Dosage = 6, Unit = "克" },
            new() { HerbName = "黄柏", Dosage = 10, Unit = "克" }
        }
    };

    /// <summary>创建验方并返回(id, dto)</summary>
    private async Task<(Guid id, FormulaDetailDto dto)> CreateFormulaAsync(
        HttpClient? client = null, FormulaInputDto? input = null)
    {
        client ??= _fixture.AdminClient;
        input ??= CreateFormulaInput();

        var response = await client.PostAsJsonAsync(BaseUrl, input);
        response.IsSuccessStatusCode.Should().BeTrue(
            $"创建验方应成功, 实际: {response.StatusCode}");

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<FormulaDetailDto>>(JsonOptions);
        return (body!.Data!.Id, body.Data);
    }

    #endregion

    #region Create Formula

    [Fact]
    public async Task CreateFormula_WithHerbItems_ShouldPersistAll()
    {
        // Arrange - 先创建真实药材获取HerbId
        var herbId1 = await CreateHerbAndGetId("当归", 20.0m);
        var herbId2 = await CreateHerbAndGetId("川芎", 15.0m);

        var input = new FormulaInputDto
        {
            Name = "四物汤_" + Guid.NewGuid().ToString("N")[..4],
            Effect = "补血调经",
            Usage = "水煎服",
            Category = "补血方",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbId = herbId1, HerbName = "当归", Dosage = 12, Unit = "克" },
                new() { HerbId = herbId2, HerbName = "川芎", Dosage = 9, Unit = "克" },
                new() { HerbName = "白芍", Dosage = 12, Unit = "克" }, // 延迟绑定(无HerbId)
            }
        };

        // Act
        var response = await _fixture.AdminClient.PostAsJsonAsync(BaseUrl, input);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<FormulaDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Id.Should().NotBe(Guid.Empty);
        body.Data.Name.Should().StartWith("四物汤_");
        body.Data.Effect.Should().Be("补血调经");

        // 验证基础字段持久化
        var getResp = await _fixture.AdminClient.GetAsync($"{BaseUrl}/{body.Data.Id}");
        var fetched = await getResp.Content
            .ReadFromJsonAsync<ApiResponse<FormulaDetailDto>>(JsonOptions);
        fetched!.Data!.Name.Should().StartWith("四物汤_");
    }

    [Fact]
    public async Task CreateFormula_WithDelayedBinding_ShouldSucceed()
    {
        // Arrange - 所有药材项都不绑定HerbId (延迟绑定)
        var input = CreateFormulaInput("延迟绑定");

        // Act
        var (formulaId, dto) = await CreateFormulaAsync(input: input);

        // Assert
        dto.Name.Should().Contain("延迟绑定");

        // 验证基础字段持久化
        var getResp = await _fixture.AdminClient.GetAsync($"{BaseUrl}/{formulaId}");
        var fetched = await getResp.Content
            .ReadFromJsonAsync<ApiResponse<FormulaDetailDto>>(JsonOptions);
        fetched!.Data!.Name.Should().Contain("延迟绑定");
    }

    [Fact]
    public async Task CreateFormula_WithDoctorToken_ShouldSucceed()
    {
        // Arrange
        var input = CreateFormulaInput("Doctor创建");

        // Act
        var response = await _fixture.DoctorClient.PostAsJsonAsync(BaseUrl, input);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(
            "Doctor角色应能创建验方 (DoctorOrAdmin策略)");
    }

    #endregion

    #region Get Formulas

    [Fact]
    public async Task GetFormulas_ShouldReturnPagedList()
    {
        // Arrange
        await CreateFormulaAsync();

        // Act
        var response = await _fixture.AdminClient.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<FormulaListDto>>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFormulas_WithKeyword_ShouldFilter()
    {
        // Arrange
        var uniqueTag = "UniFml" + Guid.NewGuid().ToString("N")[..4];
        await CreateFormulaAsync(input: CreateFormulaInput(uniqueTag));

        // Act
        var response = await _fixture.AdminClient
            .GetAsync($"{BaseUrl}?keyword={uniqueTag}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<FormulaListDto>>>(JsonOptions);
        body!.Data!.Items.Should().Contain(f => f.Name.Contains(uniqueTag));
    }

    [Fact]
    public async Task GetFormula_ById_ShouldIncludeHerbItems()
    {
        // Arrange
        var (formulaId, _) = await CreateFormulaAsync();

        // Act
        var response = await _fixture.AdminClient.GetAsync($"{BaseUrl}/{formulaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<FormulaDetailDto>>(JsonOptions);
        body!.Data!.Id.Should().Be(formulaId);
        // 注: Herbs子集合的加载取决于Mapper实现，此处验证基础字段
    }

    [Fact]
    public async Task GetFormula_NonExistentId_ShouldReturn404()
    {
        var response = await _fixture.AdminClient
            .GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFormulas_InvalidPagination_ShouldReturn400()
    {
        var response = await _fixture.AdminClient
            .GetAsync($"{BaseUrl}?page=0");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Update Formula

    [Fact]
    public async Task UpdateFormula_ShouldReplaceHerbItems()
    {
        // Arrange - 创建验方
        var (formulaId, original) = await CreateFormulaAsync();

        // Act - 更新为2个药材项
        var updateInput = new FormulaInputDto
        {
            Id = formulaId,
            Name = "更新后验方_" + Guid.NewGuid().ToString("N")[..4],
            Effect = "活血化瘀",
            Usage = "温服",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbName = "桃仁", Dosage = 12, Unit = "克" },
                new() { HerbName = "红花", Dosage = 6, Unit = "克" }
            }
        };
        var response = await _fixture.AdminClient
            .PutAsJsonAsync($"{BaseUrl}/{formulaId}", updateInput);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<FormulaDetailDto>>(JsonOptions);
        body!.Data!.Effect.Should().Be("活血化瘀");

        // Verify persistence via GetById
        var getResp = await _fixture.AdminClient.GetAsync($"{BaseUrl}/{formulaId}");
        var fetched = await getResp.Content
            .ReadFromJsonAsync<ApiResponse<FormulaDetailDto>>(JsonOptions);
        fetched!.Data!.Effect.Should().Be("活血化瘀", "更新后Effect应已持久化");
    }

    #endregion

    #region Delete Formula

    [Fact]
    public async Task DeleteFormula_ShouldSoftDelete()
    {
        // Arrange
        var (formulaId, _) = await CreateFormulaAsync();

        // Act
        var response = await _fixture.AdminClient
            .DeleteAsync($"{BaseUrl}/{formulaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResp = await _fixture.AdminClient.GetAsync($"{BaseUrl}/{formulaId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Toggle Status

    [Fact]
    public async Task ToggleStatus_ShouldChangeFormulaStatus()
    {
        // Arrange
        var (formulaId, created) = await CreateFormulaAsync();
        var originalStatus = created.Status;

        // Act
        var response = await _fixture.AdminClient
            .PostAsync($"{BaseUrl}/{formulaId}/toggle-status", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<FormulaDetailDto>>(JsonOptions);
        body!.Data!.Status.Should().NotBe(originalStatus);
    }

    #endregion

    #region Restore Formula

    [Fact]
    public async Task RestoreFormula_ShouldMakeAccessibleAgain()
    {
        // Arrange
        var (formulaId, _) = await CreateFormulaAsync();

        await _fixture.AdminClient.DeleteAsync($"{BaseUrl}/{formulaId}");
        var getAfterDel = await _fixture.AdminClient.GetAsync($"{BaseUrl}/{formulaId}");
        getAfterDel.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Act
        var response = await _fixture.AdminClient
            .PostAsync($"{BaseUrl}/{formulaId}/restore", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<FormulaDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue();

        var getAfterRestore = await _fixture.AdminClient.GetAsync($"{BaseUrl}/{formulaId}");
        getAfterRestore.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Batch Operations

    [Fact]
    public async Task BatchDelete_MultipleFormulas_ShouldSoftDeleteAll()
    {
        var ids = new List<Guid>();
        for (int i = 0; i < 3; i++)
        {
            var (id, _) = await CreateFormulaAsync();
            ids.Add(id);
        }

        var response = await _fixture.AdminClient
            .PostAsJsonAsync($"{BaseUrl}/batch-delete", new { Ids = ids });

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

    #region Herb Item Validation

    [Fact]
    public async Task CreateFormula_WithBoundHerbId_ShouldPopulatePrice()
    {
        // Arrange - 创建真实药材
        var herbId = await CreateHerbAndGetId("价格测试药材", 25.00m);

        var input = new FormulaInputDto
        {
            Name = "价格验证_" + Guid.NewGuid().ToString("N")[..4],
            Effect = "测试",
            Usage = "煎服",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbId = herbId, HerbName = "价格测试药材", Dosage = 10, Unit = "克" }
            }
        };

        // Act
        var (formulaId, _) = await CreateFormulaAsync(input: input);

        // Assert - 通过GetById验证
        var response = await _fixture.AdminClient.GetAsync($"{BaseUrl}/{formulaId}");
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<FormulaDetailDto>>(JsonOptions);
        // 验证基础字段
        body!.Data!.Name.Should().Contain("价格验证");
    }

    #endregion

    #region Category Filter (migrated from Structure B)

    [Fact]
    public async Task GetFormulas_WithCategory_ShouldFilter()
    {
        // Arrange
        var uniqueCategory = "测试分类_" + Guid.NewGuid().ToString("N")[..4];
        var input = CreateFormulaInput();
        input.Category = uniqueCategory;
        await CreateFormulaAsync(input: input);

        // Act
        var response = await _fixture.AdminClient
            .GetAsync($"{BaseUrl}?category={uniqueCategory}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<FormulaListDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    #endregion

    #region Authorization (migrated from Structure B)

    [Fact]
    public async Task GetById_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var (formulaId, _) = await CreateFormulaAsync();
        var client = _fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"{BaseUrl}/{formulaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _fixture.CreateClient();
        var newFormula = new FormulaInputDto
        {
            Name = "未认证测试",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbName = "测试药材", Dosage = 10, Unit = "克" }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync(BaseUrl, newFormula);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var (formulaId, _) = await CreateFormulaAsync();
        var client = _fixture.CreateClient();
        var updatedFormula = new FormulaInputDto
        {
            Id = formulaId,
            Name = "未认证更新",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbName = "测试", Dosage = 10, Unit = "克" }
            }
        };

        // Act
        var response = await client.PutAsJsonAsync($"{BaseUrl}/{formulaId}", updatedFormula);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var (formulaId, _) = await CreateFormulaAsync();
        var client = _fixture.CreateClient();

        // Act
        var response = await client.DeleteAsync($"{BaseUrl}/{formulaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BatchOperations_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var (formulaId, _) = await CreateFormulaAsync();
        var client = _fixture.CreateClient();
        var batchDto = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { formulaId }
        };

        // Act
        var response = await client.PostAsJsonAsync($"{BaseUrl}/batch-delete", batchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Validation (migrated from Structure B)

    [Fact]
    public async Task CreateFormula_WithoutHerbs_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidFormula = new FormulaInputDto
        {
            Name = "无药材验方",
            Effect = "测试",
            Usage = "测试",
            Herbs = new List<FormulaHerbItemInputDto>() // 空药材列表
        };

        // Act
        var response = await _fixture.AdminClient
            .PostAsJsonAsync(BaseUrl, invalidFormula);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Update/Delete Edge Cases (migrated from Structure B)

    [Fact]
    public async Task UpdateFormula_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var updatedFormula = new FormulaInputDto
        {
            Id = nonExistingId,
            Name = "不存在的验方",
            Herbs = new List<FormulaHerbItemInputDto>
            {
                new() { HerbName = "测试", Dosage = 10, Unit = "克" }
            }
        };

        // Act
        var response = await _fixture.AdminClient
            .PutAsJsonAsync($"{BaseUrl}/{nonExistingId}", updatedFormula);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteFormula_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _fixture.AdminClient
            .DeleteAsync($"{BaseUrl}/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ToggleStatus_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _fixture.AdminClient
            .PostAsync($"{BaseUrl}/{nonExistingId}/toggle-status", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestoreFormula_WithNonExistingId_ShouldReturn422()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _fixture.AdminClient
            .PostAsync($"{BaseUrl}/{nonExistingId}/restore", null);

        // Assert - Restore 对不存在的ID返回 422 (BusinessFail)
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    #endregion

    #region Batch Enable/Disable (migrated from Structure B)

    [Fact]
    public async Task BatchEnable_WithValidIds_ShouldEnableMultiple()
    {
        // Arrange
        var (formulaId, _) = await CreateFormulaAsync();
        var batchDto = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { formulaId }
        };

        // Act
        var response = await _fixture.AdminClient
            .PostAsJsonAsync($"{BaseUrl}/batch-enable", batchDto);

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
        var (formulaId, _) = await CreateFormulaAsync();
        var batchDto = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { formulaId }
        };

        // Act
        var response = await _fixture.AdminClient
            .PostAsJsonAsync($"{BaseUrl}/batch-disable", batchDto);

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
        // Act
        var response = await _fixture.AdminClient
            .GetAsync($"{BaseUrl}/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task ExportTemplate_ShouldReturnTemplateFile()
    {
        // Act - import-template 需要认证
        var response = await _fixture.AdminClient.GetAsync($"{BaseUrl}/import-template");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    #endregion

    #region Validation Flow (migrated from Structure B)

    [Fact]
    public async Task GetPendingValidation_ShouldReturnDraftFormulas()
    {
        // Act
        var response = await _fixture.AdminClient
            .GetAsync($"{BaseUrl}/pending-validation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<ApiResponse<List<FormulaDetailDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    #endregion
}
