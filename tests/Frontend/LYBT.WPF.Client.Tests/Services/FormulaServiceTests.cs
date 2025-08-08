using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Core.Models.Formulas;
using LYBT.WPF.Client.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Moq;
using Refit;
using Xunit;

namespace LYBT.WPF.Client.Tests.Services
{
    /// <summary>
    /// 验方管理服务前端单元测试
    /// 测试验方/处方模板管理的核心功能
    /// </summary>
    public class FormulaServiceTests
    {
        private readonly Mock<IFormulaApiService> _mockApiService;
        private readonly FormulaService _service;

        public FormulaServiceTests()
        {
            _mockApiService = new Mock<IFormulaApiService>();
            _service = new FormulaService(_mockApiService.Object);
        }

        #region Test Data Factory Methods

        private FormulaDto CreateTestFormulaDto(Guid? id = null)
        {
            return new FormulaDto
            {
                Id = id ?? Guid.NewGuid(),
                Name = "感冒清热方",
                Effect = "清热解毒，疏风散热",
                Usage = "每日一剂，分两次服用",
                IsShared = true,
                Remark = "经典感冒方",
                CreateTime = DateTime.Now.AddDays(-30),
                UpdateTime = DateTime.Now.AddDays(-1)
            };
        }

        private FormulaDetailDto CreateTestFormulaDetailDto(Guid? id = null)
        {
            return new FormulaDetailDto
            {
                Id = id ?? Guid.NewGuid(),
                Name = "六味地黄丸",
                Indications = "肾阴虚证",
                Effect = "滋阴补肾",
                Usage = "每日二次，每次9g",
                Instructions = "温开水送服",
                Contraindications = "脾虚便溏者慎用",
                IsShared = true,
                Remark = "经典补肾方",
                CreateTime = DateTime.Now.AddDays(-60),
                UpdateTime = DateTime.Now.AddDays(-5),
                CreatedByName = "张医生",
                Herbs = new List<FormulaHerbItemDto>
                {
                    new FormulaHerbItemDto
                    {
                        Id = Guid.NewGuid(),
                        HerbId = Guid.NewGuid(),
                        HerbName = "熟地黄",
                        Quantity = 24,
                        Unit = "g",
                        Usage = "君药",
                        Price = 35.00m
                    },
                    new FormulaHerbItemDto
                    {
                        Id = Guid.NewGuid(),
                        HerbId = Guid.NewGuid(),
                        HerbName = "山茱萸",
                        Quantity = 12,
                        Unit = "g",
                        Usage = "臣药",
                        Price = 45.00m
                    }
                }
            };
        }

        private FormulaCreateDto CreateTestFormulaCreateDto()
        {
            return new FormulaCreateDto
            {
                Name = "补中益气汤",
                Indications = "脾胃气虚，中气下陷",
                Effect = "补中益气，升阳举陷",
                Usage = "每日一剂，水煎服",
                Instructions = "饭前服用",
                Contraindications = "阴虚火旺者慎用",
                IsShared = true,
                Remark = "治疗中气不足的经典方",
                Herbs = new List<FormulaHerbItemCreateDto>
                {
                    new FormulaHerbItemCreateDto
                    {
                        HerbId = Guid.NewGuid(),
                        Quantity = 30,
                        Usage = "君药",
                        SortOrder = 1
                    },
                    new FormulaHerbItemCreateDto
                    {
                        HerbId = Guid.NewGuid(),
                        Quantity = 10,
                        Usage = "臣药",
                        SortOrder = 2
                    }
                }
            };
        }

        private FormulaUpdateDto CreateTestFormulaUpdateDto(Guid? id = null)
        {
            return new FormulaUpdateDto
            {
                Id = id ?? Guid.NewGuid(),
                Name = "四物汤",
                Indications = "血虚证",
                Effect = "补血调经",
                Usage = "每日一剂，水煎服",
                Instructions = "月经后服用效果更佳",
                Contraindications = "脾虚湿盛者慎用",
                IsShared = true,
                Remark = "妇科补血经典方",
                Herbs = new List<FormulaHerbItemUpdateDto>
                {
                    new FormulaHerbItemUpdateDto
                    {
                        Id = Guid.NewGuid(),
                        HerbId = Guid.NewGuid(),
                        Quantity = 12,
                        Usage = "君药",
                        SortOrder = 1
                    },
                    new FormulaHerbItemUpdateDto
                    {
                        Id = Guid.NewGuid(),
                        HerbId = Guid.NewGuid(),
                        Quantity = 15,
                        Usage = "臣药",
                        SortOrder = 2
                    }
                }
            };
        }

        private PaginationRequest CreateTestPaginationRequest()
        {
            return new PaginationRequest
            {
                CurrentPage = 1,
                PageSize = 20,
                SearchKeyword = "感冒"
            };
        }

        private PaginatedResult<FormulaDto> CreateTestPaginatedResult()
        {
            return new PaginatedResult<FormulaDto>
            {
                Items = new List<FormulaDto> 
                { 
                    CreateTestFormulaDto(),
                    CreateTestFormulaDto() 
                },
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 20
            };
        }

        /// <summary>
        /// 创建成功的 ApiResponse
        /// </summary>
        private ApiResponse<T> CreateSuccessApiResponse<T>(T content)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return new ApiResponse<T>(response, content, new RefitSettings());
        }

        /// <summary>
        /// 创建失败的 ApiResponse
        /// </summary>
        private ApiResponse<T> CreateFailureApiResponse<T>()
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            return new ApiResponse<T>(response, default(T), new RefitSettings());
        }

        #endregion

        #region SearchFormulasAsync Tests

        [Fact]
        public async Task SearchFormulasAsync_WithValidQuery_ReturnsPagedResult()
        {
            // Arrange
            var query = CreateTestPaginationRequest();
            var paginatedResult = CreateTestPaginatedResult();
            var apiResponse = CreateSuccessApiResponse(paginatedResult);

            _mockApiService
                .Setup(x => x.GetPagedFormulasAsync(It.IsAny<PaginationRequest>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.SearchFormulasAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(20);
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task SearchFormulasAsync_WhenApiCallFails_ReturnsEmptyResultWithError()
        {
            // Arrange
            var query = CreateTestPaginationRequest();
            var apiResponse = CreateFailureApiResponse<PaginatedResult<FormulaDto>>();

            _mockApiService
                .Setup(x => x.GetPagedFormulasAsync(It.IsAny<PaginationRequest>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.SearchFormulasAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.ErrorMessage.Should().Be("获取验方模板失败");
        }

        [Fact]
        public async Task SearchFormulasAsync_WhenExceptionThrown_ReturnsErrorMessage()
        {
            // Arrange
            var query = CreateTestPaginationRequest();

            _mockApiService
                .Setup(x => x.GetPagedFormulasAsync(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(new Exception("网络错误"));

            // Act
            var result = await _service.SearchFormulasAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.ErrorMessage.Should().Contain("分页查询验方模板时发生错误");
        }

        #endregion

        #region GetListAsync Tests

        [Fact]
        public async Task GetListAsync_WithoutFilters_ReturnsAllFormulas()
        {
            // Arrange
            var paginatedResult = CreateTestPaginatedResult();
            var apiResponse = CreateSuccessApiResponse(paginatedResult);

            _mockApiService
                .Setup(x => x.GetFormulasAsync(null, null))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetListAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data!.First().Name.Should().Be("感冒清热方");
        }

        [Fact]
        public async Task GetListAsync_WithKeywordFilter_ReturnsFilteredFormulas()
        {
            // Arrange
            var paginatedResult = CreateTestPaginatedResult();
            var apiResponse = CreateSuccessApiResponse(paginatedResult);

            _mockApiService
                .Setup(x => x.GetFormulasAsync("感冒", null))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetListAsync("感冒");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockApiService.Verify(x => x.GetFormulasAsync("感冒", null), Times.Once);
        }

        [Fact]
        public async Task GetListAsync_WithCategoryFilter_ReturnsFilteredFormulas()
        {
            // Arrange
            var paginatedResult = CreateTestPaginatedResult();
            var apiResponse = CreateSuccessApiResponse(paginatedResult);

            _mockApiService
                .Setup(x => x.GetFormulasAsync(null, "补益类"))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetListAsync(null, "补益类");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockApiService.Verify(x => x.GetFormulasAsync(null, "补益类"), Times.Once);
        }

        [Fact]
        public async Task GetListAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            _mockApiService
                .Setup(x => x.GetFormulasAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("API错误"));

            // Act
            var result = await _service.GetListAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsFormulaDetail()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var detailDto = CreateTestFormulaDetailDto(formulaId);
            var apiResponse = CreateSuccessApiResponse(detailDto);

            _mockApiService
                .Setup(x => x.GetFormulaByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetByIdAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(formulaId);
            result.Data.Name.Should().Be("六味地黄丸");
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ReturnsFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var apiResponse = CreateFailureApiResponse<FormulaDetailDto>();

            _mockApiService
                .Setup(x => x.GetFormulaByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetByIdAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidDto_ReturnsCreatedFormula()
        {
            // Arrange
            var createDto = CreateTestFormulaCreateDto();
            var createdDto = CreateTestFormulaDto();
            var apiResponse = CreateSuccessApiResponse(createdDto);

            _mockApiService
                .Setup(x => x.CreateFormulaAsync(It.IsAny<FormulaCreateDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("感冒清热方");
            _mockApiService.Verify(x => x.CreateFormulaAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var createDto = CreateTestFormulaCreateDto();

            _mockApiService
                .Setup(x => x.CreateFormulaAsync(It.IsAny<FormulaCreateDto>()))
                .ThrowsAsync(new Exception("创建失败"));

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateName_ReturnsFailure()
        {
            // Arrange
            var createDto = CreateTestFormulaCreateDto();
            var apiException = await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Post,
                new HttpResponseMessage(HttpStatusCode.Conflict),
                new RefitSettings());

            _mockApiService
                .Setup(x => x.CreateFormulaAsync(It.IsAny<FormulaCreateDto>()))
                .ThrowsAsync(apiException);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidDto_ReturnsUpdatedFormula()
        {
            // Arrange
            var updateDto = CreateTestFormulaUpdateDto();
            var updatedDto = CreateTestFormulaDto(updateDto.Id);
            var apiResponse = CreateSuccessApiResponse(updatedDto);

            _mockApiService
                .Setup(x => x.UpdateFormulaAsync(It.IsAny<Guid>(), It.IsAny<FormulaUpdateDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            _mockApiService.Verify(x => x.UpdateFormulaAsync(updateDto.Id, updateDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var updateDto = CreateTestFormulaUpdateDto();

            _mockApiService
                .Setup(x => x.UpdateFormulaAsync(It.IsAny<Guid>(), It.IsAny<FormulaUpdateDto>()))
                .ThrowsAsync(new Exception("更新失败"));

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithValidId_ReturnsSuccess()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var apiResponse = CreateSuccessApiResponse(true);

            _mockApiService
                .Setup(x => x.DeleteFormulaAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.DeleteAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockApiService.Verify(x => x.DeleteFormulaAsync(formulaId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();

            _mockApiService
                .Setup(x => x.DeleteFormulaAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("删除失败"));

            // Act
            var result = await _service.DeleteAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task DeleteAsync_WhenFormulaInUse_ReturnsFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var apiException = await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Delete,
                new HttpResponseMessage(HttpStatusCode.Conflict),
                new RefitSettings());

            _mockApiService
                .Setup(x => x.DeleteFormulaAsync(It.IsAny<Guid>()))
                .ThrowsAsync(apiException);

            // Act
            var result = await _service.DeleteAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        #endregion

        #region BatchDeleteAsync Tests

        [Fact]
        public async Task BatchDeleteAsync_WithValidIds_ReturnsDeletedCount()
        {
            // Arrange
            var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            var apiResponse = CreateSuccessApiResponse(3);

            _mockApiService
                .Setup(x => x.BatchDeleteFormulasAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.BatchDeleteAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(3);
            _mockApiService.Verify(x => x.BatchDeleteFormulasAsync(ids), Times.Once);
        }

        [Fact]
        public async Task BatchDeleteAsync_WithEmptyList_ReturnsZero()
        {
            // Arrange
            var ids = new List<Guid>();
            var apiResponse = CreateSuccessApiResponse(0);

            _mockApiService
                .Setup(x => x.BatchDeleteFormulasAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.BatchDeleteAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(0);
        }

        [Fact]
        public async Task BatchDeleteAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var ids = new List<Guid> { Guid.NewGuid() };

            _mockApiService
                .Setup(x => x.BatchDeleteFormulasAsync(It.IsAny<List<Guid>>()))
                .ThrowsAsync(new Exception("批量删除失败"));

            // Act
            var result = await _service.BatchDeleteAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region CopyAsync Tests

        [Fact]
        public async Task CopyAsync_WithValidIdAndName_ReturnsCopiedFormula()
        {
            // Arrange
            var sourceId = Guid.NewGuid();
            var newName = "感冒清热方（副本）";
            var copiedDto = CreateTestFormulaDto();
            var apiResponse = CreateSuccessApiResponse(copiedDto);

            _mockApiService
                .Setup(x => x.CopyFormulaAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.CopyAsync(sourceId, newName);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            _mockApiService.Verify(x => x.CopyFormulaAsync(sourceId, newName), Times.Once);
        }

        [Fact]
        public async Task CopyAsync_WithDuplicateName_ReturnsFailure()
        {
            // Arrange
            var sourceId = Guid.NewGuid();
            var newName = "已存在的方名";
            var apiException = await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Post,
                new HttpResponseMessage(HttpStatusCode.Conflict),
                new RefitSettings());

            _mockApiService
                .Setup(x => x.CopyFormulaAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ThrowsAsync(apiException);

            // Act
            var result = await _service.CopyAsync(sourceId, newName);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task CopyAsync_WhenSourceNotFound_ReturnsFailure()
        {
            // Arrange
            var sourceId = Guid.NewGuid();
            var newName = "新方名";
            var apiException = await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Post,
                new HttpResponseMessage(HttpStatusCode.NotFound),
                new RefitSettings());

            _mockApiService
                .Setup(x => x.CopyFormulaAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ThrowsAsync(apiException);

            // Act
            var result = await _service.CopyAsync(sourceId, newName);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        #endregion

        #region ToggleStatusAsync Tests

        [Fact]
        public async Task ToggleStatusAsync_WithValidId_ReturnsSuccess()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var apiResponse = CreateSuccessApiResponse(true);

            _mockApiService
                .Setup(x => x.ToggleFormulaStatusAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.ToggleStatusAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockApiService.Verify(x => x.ToggleFormulaStatusAsync(formulaId), Times.Once);
        }

        [Fact]
        public async Task ToggleStatusAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();

            _mockApiService
                .Setup(x => x.ToggleFormulaStatusAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("状态切换失败"));

            // Act
            var result = await _service.ToggleStatusAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region GetCategoriesAsync Tests

        [Fact]
        public async Task GetCategoriesAsync_ReturnsCategories()
        {
            // Arrange
            var categories = new List<string> { "补益类", "解表类", "清热类", "温里类" };
            var apiResponse = CreateSuccessApiResponse(categories);

            _mockApiService
                .Setup(x => x.GetCategoriesAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetCategoriesAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(4);
            result.Data.Should().Contain("补益类");
        }

        [Fact]
        public async Task GetCategoriesAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            _mockApiService
                .Setup(x => x.GetCategoriesAsync())
                .ThrowsAsync(new Exception("获取分类失败"));

            // Act
            var result = await _service.GetCategoriesAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetCategoriesAsync_WhenNoCategoriesExist_ReturnsEmptyList()
        {
            // Arrange
            var categories = new List<string>();
            var apiResponse = CreateSuccessApiResponse(categories);

            _mockApiService
                .Setup(x => x.GetCategoriesAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetCategoriesAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        #endregion
    }
}