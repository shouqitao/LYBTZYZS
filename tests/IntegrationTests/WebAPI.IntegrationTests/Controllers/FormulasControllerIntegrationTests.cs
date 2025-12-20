using FluentAssertions;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.WebAPI.IntegrationTests.Controllers
{
    /// <summary>
    /// FormulasController集成测试
    /// OpenSpec: optimize-integration-tests - Phase 2.1
    /// 测试15个API端点: CRUD + 批量操作 + 导入导出 + 验证流程
    /// </summary>
    public class FormulasControllerIntegrationTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;
        private const string BaseUrl = "/api/v1/formulas";

        // 测试数据ID
        private Guid _testFormulaId;
        private Guid _testFormula2Id;
        private Guid _testHerbId;

        public FormulasControllerIntegrationTests(ITestOutputHelper output) : base()
        {
            _output = output;
        }

        protected override void SeedBasicTestData(AppDbContext context)
        {
            base.SeedBasicTestData(context);

            // 使用真实数据库，查询现有药材或创建测试药材
            var existingHerb = context.Set<Herb>().FirstOrDefault(h => !h.IsDeleted);
            if (existingHerb != null)
            {
                _testHerbId = existingHerb.Id;
            }
            else
            {
                // 如果没有药材，创建一个测试药材
                _testHerbId = Guid.NewGuid();
                var testHerb = new Herb
                {
                    Id = _testHerbId,
                    Name = "测试黄芪",
                    PinYinCode = "CSHQ",
                    Category = "补气药",
                    Unit = "克",
                    Price = 0.5m,
                    Status = CommonStatus.Enabled,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.Set<Herb>().Add(testHerb);
            }

            // 创建测试验方数据
            _testFormulaId = Guid.NewGuid();
            _testFormula2Id = Guid.NewGuid();

            var testFormulas = new List<Formula>
            {
                new Formula
                {
                    Id = _testFormulaId,
                    Name = "测试验方一",
                    Effect = "补气健脾",
                    Indication = "气虚乏力",
                    Usage = "水煎服",
                    Category = "补益剂",
                    Status = CommonStatus.Enabled,
                    ValidationStatus = FormulaValidationStatus.Validated,
                    IsShared = true,
                    FormulaType = FormulaType.Experience,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Herbs = new List<FormulaHerbItem>
                    {
                        new FormulaHerbItem
                        {
                            Id = Guid.NewGuid(),
                            FormulaId = _testFormulaId,
                            HerbId = _testHerbId,
                            HerbName = "黄芪",
                            Dosage = 30,
                            Unit = "克",
                            IsValidated = true
                        }
                    }
                },
                new Formula
                {
                    Id = _testFormula2Id,
                    Name = "测试验方二",
                    Effect = "活血化瘀",
                    Indication = "血瘀疼痛",
                    Usage = "水煎服",
                    Category = "理血剂",
                    Status = CommonStatus.Enabled,
                    ValidationStatus = FormulaValidationStatus.Draft,
                    IsShared = false,
                    FormulaType = FormulaType.Classic,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Herbs = new List<FormulaHerbItem>
                    {
                        new FormulaHerbItem
                        {
                            Id = Guid.NewGuid(),
                            FormulaId = _testFormula2Id,
                            HerbId = _testHerbId,
                            HerbName = "当归",
                            Dosage = 15,
                            Unit = "克",
                            IsValidated = true
                        }
                    }
                }
            };

            context.Set<Formula>().AddRange(testFormulas);
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

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<FormulaListDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCountGreaterThan(0);
            result.Data.CurrentPage.Should().Be(1);
            result.Data.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetList_WithKeywordSearch_ShouldReturnFilteredResults()
        {
            // Arrange
            var keyword = "测试验方";

            // Act
            var response = await Client.GetAsync($"{BaseUrl}?page=1&pageSize=10&keyword={keyword}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<FormulaListDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetList_WithCategoryFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            var category = "补益剂";

            // Act
            var response = await Client.GetAsync($"{BaseUrl}?page=1&pageSize=10&category={category}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<FormulaListDto>>>();
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
        public async Task GetById_WithExistingId_ShouldReturnFormula()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/{_testFormulaId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<FormulaDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(_testFormulaId);
            result.Data.Name.Should().Be("测试验方一");
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
            var response = await client.GetAsync($"{BaseUrl}/{_testFormulaId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Add Tests

        [Fact]
        public async Task Add_WithValidData_ShouldCreateFormula()
        {
            // Arrange
            var newFormula = new FormulaInputDto
            {
                Name = "新建测试验方",
                Effect = "清热解毒",
                Usage = "水煎服，每日一剂",
                Category = "清热剂",
                IsShared = true,
                Herbs = new List<FormulaHerbItemInputDto>
                {
                    new FormulaHerbItemInputDto
                    {
                        HerbName = "金银花",
                        Dosage = 15,
                        Unit = "克"
                    }
                }
            };

            // Act
            var response = await Client.PostAsJsonAsync(BaseUrl, newFormula);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<FormulaDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().NotBeEmpty();
            result.Data.Name.Should().Be("新建测试验方");
        }

        [Fact]
        public async Task Add_WithoutHerbs_ShouldReturnBadRequest()
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
            var response = await Client.PostAsJsonAsync(BaseUrl, invalidFormula);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Add_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = Factory.CreateClient();
            var newFormula = new FormulaInputDto
            {
                Name = "未认证测试",
                Herbs = new List<FormulaHerbItemInputDto>
                {
                    new FormulaHerbItemInputDto { HerbName = "测试药材", Dosage = 10, Unit = "克" }
                }
            };

            // Act
            var response = await client.PostAsJsonAsync(BaseUrl, newFormula);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_WithValidData_ShouldUpdateFormula()
        {
            // Arrange
            var updatedFormula = new FormulaInputDto
            {
                Id = _testFormulaId,
                Name = "测试验方一(已更新)",
                Effect = "补气健脾(已更新)",
                Usage = "水煎服",
                Category = "补益剂",
                IsShared = true,
                Herbs = new List<FormulaHerbItemInputDto>
                {
                    new FormulaHerbItemInputDto
                    {
                        HerbName = "黄芪",
                        Dosage = 45,
                        Unit = "克"
                    }
                }
            };

            // Act
            var response = await Client.PutAsJsonAsync($"{BaseUrl}/{_testFormulaId}", updatedFormula);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<FormulaDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("测试验方一(已更新)");
        }

        [Fact]
        public async Task Update_WithNonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            var updatedFormula = new FormulaInputDto
            {
                Id = nonExistingId,
                Name = "不存在的验方",
                Herbs = new List<FormulaHerbItemInputDto>
                {
                    new FormulaHerbItemInputDto { HerbName = "测试", Dosage = 10, Unit = "克" }
                }
            };

            // Act
            var response = await Client.PutAsJsonAsync($"{BaseUrl}/{nonExistingId}", updatedFormula);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Update_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = Factory.CreateClient();
            var updatedFormula = new FormulaInputDto
            {
                Id = _testFormulaId,
                Name = "未认证更新",
                Herbs = new List<FormulaHerbItemInputDto>
                {
                    new FormulaHerbItemInputDto { HerbName = "测试", Dosage = 10, Unit = "克" }
                }
            };

            // Act
            var response = await client.PutAsJsonAsync($"{BaseUrl}/{_testFormulaId}", updatedFormula);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_WithExistingId_ShouldSoftDeleteFormula()
        {
            // Act
            var response = await Client.DeleteAsync($"{BaseUrl}/{_testFormula2Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();

            // 验证验方已被软删除（无法再查询到）
            var getResponse = await Client.GetAsync($"{BaseUrl}/{_testFormula2Id}");
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
            var response = await client.DeleteAsync($"{BaseUrl}/{_testFormulaId}");

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
                Ids = new List<Guid> { _testFormula2Id }
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
                Ids = new List<Guid> { _testFormulaId }
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
                Ids = new List<Guid> { _testFormulaId }
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
                Ids = new List<Guid> { _testFormulaId }
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

        #endregion

        #region Validation Flow Tests

        [Fact]
        public async Task GetPendingValidation_ShouldReturnDraftFormulas()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/pending-validation");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<FormulaDetailDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task ToggleStatus_ShouldChangeFormulaStatus()
        {
            // Act
            var response = await Client.PostAsync($"{BaseUrl}/{_testFormulaId}/toggle-status", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<FormulaDetailDto>>();
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
