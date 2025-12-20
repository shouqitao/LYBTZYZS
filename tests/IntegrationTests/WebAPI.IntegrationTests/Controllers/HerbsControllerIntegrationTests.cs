using FluentAssertions;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.WebAPI.IntegrationTests.Controllers
{
    /// <summary>
    /// HerbsController集成测试
    /// OpenSpec: optimize-integration-tests - Phase 2.2
    /// 测试18个API端点: CRUD + 批量操作 + 导入导出 + 引用检查
    /// </summary>
    public class HerbsControllerIntegrationTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;
        private const string BaseUrl = "/api/v1/herbs";

        // 测试数据ID
        private Guid _testHerbId;
        private Guid _testHerb2Id;

        public HerbsControllerIntegrationTests(ITestOutputHelper output) : base()
        {
            _output = output;
        }

        protected override void SeedBasicTestData(AppDbContext context)
        {
            base.SeedBasicTestData(context);

            // 创建测试药材数据
            _testHerbId = Guid.NewGuid();
            _testHerb2Id = Guid.NewGuid();

            var testHerbs = new List<Herb>
            {
                new Herb
                {
                    Id = _testHerbId,
                    Name = "集成测试黄芪",
                    PinYinCode = "JCHQ",
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
                },
                new Herb
                {
                    Id = _testHerb2Id,
                    Name = "集成测试当归",
                    PinYinCode = "JCDG",
                    Category = "补血药",
                    Origin = "甘肃岷县",
                    Spec = "全归",
                    Unit = "克",
                    Price = 0.8m,
                    CostPrice = 0.5m,
                    Effect = "补血活血，调经止痛",
                    Usage = "煎服，6-12g",
                    Status = CommonStatus.Enabled,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Set<Herb>().AddRange(testHerbs);
            context.SaveChanges();
        }

        #region GetList Tests

        [Fact]
        public async Task GetList_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = Factory.CreateClient();

            // Act
            var response = await client.GetAsync(BaseUrl);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetList_WithDefaultPagination_ShouldReturnPagedResults()
        {
            // Arrange & Act
            var response = await Client.GetAsync($"{BaseUrl}?page=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<HerbListDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task GetList_WithKeywordSearch_ShouldReturnFilteredResults()
        {
            // Arrange
            var keyword = "集成测试";

            // Act
            var response = await Client.GetAsync($"{BaseUrl}?page=1&pageSize=10&keyword={keyword}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<HerbListDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetList_WithCategoryFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            var category = "补气药";

            // Act
            var response = await Client.GetAsync($"{BaseUrl}?page=1&pageSize=10&category={category}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<HerbListDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetList_WithInvalidPageNumber_ShouldReturnBadRequest()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}?page=0&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetById_WithExistingId_ShouldReturnHerb()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/{_testHerbId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<HerbDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(_testHerbId);
            result.Data.Name.Should().Be("集成测试黄芪");
        }

        [Fact]
        public async Task GetById_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync($"{BaseUrl}/{nonExistingId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetById_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = Factory.CreateClient();

            // Act
            var response = await client.GetAsync($"{BaseUrl}/{_testHerbId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Create Tests

        [Fact]
        public async Task Create_WithValidData_ShouldCreateHerb()
        {
            // Arrange
            var newHerb = new HerbInputDto
            {
                Name = "新建测试药材",
                PinYinCode = "XJCSYC",
                Category = "清热药",
                Unit = "克",
                Price = 1.2m,
                CostPrice = 0.8m,
                Effect = "清热解毒",
                Usage = "煎服，10-15g"
            };

            // Act
            var response = await Client.PostAsJsonAsync(BaseUrl, newHerb);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<HerbDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().NotBeEmpty();
            result.Data.Name.Should().Be("新建测试药材");
        }

        [Fact]
        public async Task Create_WithoutName_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidHerb = new HerbInputDto
            {
                Name = "", // 空名称
                Unit = "克",
                Price = 1.0m
            };

            // Act
            var response = await Client.PostAsJsonAsync(BaseUrl, invalidHerb);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = Factory.CreateClient();
            var newHerb = new HerbInputDto
            {
                Name = "未认证测试药材",
                Unit = "克",
                Price = 1.0m
            };

            // Act
            var response = await client.PostAsJsonAsync(BaseUrl, newHerb);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_WithValidData_ShouldUpdateHerb()
        {
            // Arrange
            var updatedHerb = new HerbInputDto
            {
                Id = _testHerbId,
                Name = "集成测试黄芪(已更新)",
                PinYinCode = "JCHQ",
                Category = "补气药",
                Unit = "克",
                Price = 0.6m,
                Effect = "补气升阳，益卫固表(已更新)"
            };

            // Act
            var response = await Client.PutAsJsonAsync($"{BaseUrl}/{_testHerbId}", updatedHerb);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<HerbDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("集成测试黄芪(已更新)");
        }

        [Fact]
        public async Task Update_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            var updatedHerb = new HerbInputDto
            {
                Id = nonExistingId,
                Name = "不存在的药材",
                Unit = "克",
                Price = 1.0m
            };

            // Act
            var response = await Client.PutAsJsonAsync($"{BaseUrl}/{nonExistingId}", updatedHerb);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = Factory.CreateClient();
            var updatedHerb = new HerbInputDto
            {
                Id = _testHerbId,
                Name = "未认证更新",
                Unit = "克",
                Price = 1.0m
            };

            // Act
            var response = await client.PutAsJsonAsync($"{BaseUrl}/{_testHerbId}", updatedHerb);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_WithExistingId_ShouldSoftDeleteHerb()
        {
            // Act
            var response = await Client.DeleteAsync($"{BaseUrl}/{_testHerb2Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();

            // 验证药材已被软删除（无法再查询到）
            var getResponse = await Client.GetAsync($"{BaseUrl}/{_testHerb2Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var response = await Client.DeleteAsync($"{BaseUrl}/{nonExistingId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = Factory.CreateClient();

            // Act
            var response = await client.DeleteAsync($"{BaseUrl}/{_testHerbId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Batch Operations Tests

        [Fact]
        public async Task BatchDelete_WithValidIds_ShouldDeleteMultiple()
        {
            // Arrange
            var batchDto = new BatchDeleteInputDto
            {
                Ids = new List<Guid> { _testHerb2Id }
            };

            // Act
            var response = await Client.PostAsJsonAsync($"{BaseUrl}/batch-delete", batchDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<BatchOperationResultDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.SuccessCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task BatchDelete_WithEmptyIds_ShouldReturnBadRequest()
        {
            // Arrange
            var batchDto = new BatchDeleteInputDto
            {
                Ids = new List<Guid>()
            };

            // Act
            var response = await Client.PostAsJsonAsync($"{BaseUrl}/batch-delete", batchDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task BatchEnable_WithValidIds_ShouldEnableMultiple()
        {
            // Arrange
            var batchDto = new BatchDeleteInputDto
            {
                Ids = new List<Guid> { _testHerbId }
            };

            // Act
            var response = await Client.PostAsJsonAsync($"{BaseUrl}/batch-enable", batchDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<BatchOperationResultDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task BatchDisable_WithValidIds_ShouldDisableMultiple()
        {
            // Arrange
            var batchDto = new BatchDeleteInputDto
            {
                Ids = new List<Guid> { _testHerbId }
            };

            // Act
            var response = await Client.PostAsJsonAsync($"{BaseUrl}/batch-disable", batchDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<BatchOperationResultDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task BatchOperations_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = Factory.CreateClient();
            var batchDto = new BatchDeleteInputDto
            {
                Ids = new List<Guid> { _testHerbId }
            };

            // Act
            var response = await client.PostAsJsonAsync($"{BaseUrl}/batch-delete", batchDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Export Tests

        [Fact]
        public async Task Export_ShouldReturnExcelFile()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/export");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        [Fact]
        public async Task ExportTemplate_ShouldReturnTemplateFile()
        {
            // Act - ExportTemplate允许匿名访问
            var client = Factory.CreateClient();
            var response = await client.GetAsync($"{BaseUrl}/import-template");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        [Fact]
        public async Task GetAllForExport_ShouldReturnAllHerbs()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/export-all");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<HerbDetailDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        #endregion

        #region Reference Check Tests

        [Fact]
        public async Task CheckReference_WithExistingId_ShouldReturnReferenceStatus()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/{_testHerbId}/check-reference");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<HerbReferenceCheckDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task BatchCheckReference_WithValidIds_ShouldReturnResults()
        {
            // Arrange
            var request = new HerbBatchCheckReferenceInputDto
            {
                HerbIds = new List<Guid> { _testHerbId, _testHerb2Id }
            };

            // Act
            var response = await Client.PostAsJsonAsync($"{BaseUrl}/batch-check-reference", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<HerbReferenceCheckDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task BatchCheckReference_WithEmptyIds_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new HerbBatchCheckReferenceInputDto
            {
                HerbIds = new List<Guid>()
            };

            // Act
            var response = await Client.PostAsJsonAsync($"{BaseUrl}/batch-check-reference", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Status Operations Tests

        [Fact]
        public async Task ToggleStatus_ShouldChangeHerbStatus()
        {
            // Act
            var response = await Client.PostAsync($"{BaseUrl}/{_testHerbId}/toggle-status", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<HerbDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task ToggleStatus_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var response = await Client.PostAsync($"{BaseUrl}/{nonExistingId}/toggle-status", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Restore_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var response = await Client.PostAsync($"{BaseUrl}/{nonExistingId}/restore", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion
    }
}
