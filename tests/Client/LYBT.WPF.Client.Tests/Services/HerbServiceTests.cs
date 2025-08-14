using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Moq;
using Refit;
using Xunit;

namespace LYBT.WPF.Client.Tests.Services
{
    /// <summary>
    /// 中药材服务前端单元测试
    /// 测试中药材管理的核心功能
    /// </summary>
    public class HerbServiceTests
    {
        private readonly Mock<IHerbApiService> _mockApiService;
        private readonly HerbService _service;

        public HerbServiceTests()
        {
            _mockApiService = new Mock<IHerbApiService>();
            _service = new HerbService(_mockApiService.Object);
        }

        #region Test Data Factory Methods

        private HerbDto CreateTestHerbDto(Guid? id = null)
        {
            return new HerbDto
            {
                Id = id ?? Guid.NewGuid(),
                Name = "麻黄",
                PinYinCode = "MH",
                Origin = "内蒙古",
                Spec = "生麻黄",
                Unit = "g",
                Price = 12.50m,
                Effect = "发汗解表，宣肺平喘，利水消肿",
                Usage = "3-10g，先煎",
                Remark = "发汗力强，需谨慎使用",
                Status = CommonStatus.Enabled
            };
        }

        private HerbDetailDto CreateTestHerbDetailDto(Guid? id = null)
        {
            return new HerbDetailDto
            {
                Id = id ?? Guid.NewGuid(),
                Name = "人参",
                PinYinCode = "RS",
                WuBiCode = "WRCY",
                Origin = "东北长白山",
                Spec = "红参",
                Unit = "g",
                Price = 280.00m,
                Effect = "大补元气，复脉固脱，补脾益肺，生津止渴，安神益智",
                Usage = "3-9g，另煎兑服",
                Remark = "贵重药材，需单独保存",
                Status = CommonStatus.Enabled
            };
        }

        private HerbCreateDto CreateTestHerbCreateDto()
        {
            return new HerbCreateDto
            {
                Name = "黄芪",
                PinYinCode = "HQ",
                WuBiCode = "AMFC",
                Origin = "甘肃",
                Spec = "生黄芪",
                Unit = "g",
                Price = 35.00m,
                Stock = 5000,
                BatchNo = "202501001",
                ExpireDate = DateTime.Now.AddYears(2),
                Effect = "补气升阳，固表止汗，利水消肿，生津养血，行滞通痹，托毒排脓，敛疮生肌",
                Usage = "10-30g",
                Remark = "常用补气药",
                Status = CommonStatus.Enabled
            };
        }

        private HerbUpdateDto CreateTestHerbUpdateDto(Guid? id = null)
        {
            return new HerbUpdateDto
            {
                Id = id ?? Guid.NewGuid(),
                Name = "白术",
                PinYinCode = "BZ",
                WuBiCode = "RKI",
                Origin = "浙江",
                Spec = "生白术",
                Unit = "g",
                Price = 28.00m,
                Effect = "健脾益气，燥湿利水，止汗，安胎",
                Usage = "6-12g",
                Remark = "脾虚湿盛者常用",
                Status = CommonStatus.Enabled
            };
        }

        private HerbPagedQueryDto CreateTestHerbPagedQueryDto()
        {
            return new HerbPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 20,
                SearchKeyword = "麻黄",
                Name = "麻黄",
                Origin = "内蒙古",
                MinPrice = 10.00m,
                MaxPrice = 50.00m,
                Status = CommonStatus.Enabled
            };
        }

        private HerbImportDto CreateTestHerbImportDto()
        {
            return new HerbImportDto
            {
                Name = "甘草",
                Origin = "新疆",
                Spec = "生甘草",
                Unit = "g",
                Price = 18.00m,
                Stock = 1000,
                BatchNo = "202501002",
                ExpireDate = DateTime.Now.AddYears(2),
                Effect = "补脾益气，清热解毒，祛痰止咳，缓急止痛，调和诸药",
                Remark = "调和诸药常用"
            };
        }

        private PaginatedResult<HerbDto> CreateTestPaginatedResult()
        {
            return new PaginatedResult<HerbDto>
            {
                Items = new List<HerbDto> 
                { 
                    CreateTestHerbDto(),
                    CreateTestHerbDto() 
                },
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 20
            };
        }

        private CommonStatusUpdateDto CreateTestCommonStatusUpdateDto()
        {
            return new CommonStatusUpdateDto
            {
                Id = Guid.NewGuid(),
                Status = CommonStatus.Enabled,
                IsEnabled = true,
                Reason = "状态更新测试"
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

        #region SearchHerbsAsync Tests

        [Fact]
        public async Task SearchHerbsAsync_WithValidQuery_ReturnsPagedResult()
        {
            // Arrange
            var query = CreateTestHerbPagedQueryDto();
            var paginatedResult = CreateTestPaginatedResult();
            var apiResponse = CreateSuccessApiResponse(paginatedResult);

            _mockApiService
                .Setup(x => x.GetHerbsAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<decimal?>(),
                    It.IsAny<decimal?>(), It.IsAny<bool?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.SearchHerbsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(20);
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task SearchHerbsAsync_WhenApiCallFails_ReturnsEmptyResultWithError()
        {
            // Arrange
            var query = CreateTestHerbPagedQueryDto();
            var apiResponse = CreateFailureApiResponse<PaginatedResult<HerbDto>>();

            _mockApiService
                .Setup(x => x.GetHerbsAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<decimal?>(),
                    It.IsAny<decimal?>(), It.IsAny<bool?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.SearchHerbsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task SearchHerbsAsync_WhenUnauthorized_ReturnsSpecificErrorMessage()
        {
            // Arrange
            var query = CreateTestHerbPagedQueryDto();
            var apiException = await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Get,
                new HttpResponseMessage(HttpStatusCode.Unauthorized),
                new RefitSettings());

            _mockApiService
                .Setup(x => x.GetHerbsAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<decimal?>(),
                    It.IsAny<decimal?>(), It.IsAny<bool?>()))
                .ThrowsAsync(apiException);

            // Act
            var result = await _service.SearchHerbsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.ErrorMessage.Should().Contain("未授权访问");
        }

        #endregion

        #region GetHerbsAsync Tests

        [Fact]
        public async Task GetHerbsAsync_ReturnsHerbList()
        {
            // Arrange
            var paginatedResult = CreateTestPaginatedResult();
            var apiResponse = CreateSuccessApiResponse(paginatedResult);

            _mockApiService
                .Setup(x => x.GetHerbsAsync(1, 1000, null, null, null, null, null, null, null, null, null))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetHerbsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.First().Name.Should().Be("麻黄");
        }

        [Fact]
        public async Task GetHerbsAsync_WhenApiCallFails_ThrowsException()
        {
            // Arrange
            _mockApiService
                .Setup(x => x.GetHerbsAsync(1, 1000, null, null, null, null, null, null, null, null, null))
                .ThrowsAsync(new Exception("API错误"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _service.GetHerbsAsync());
            exception.Message.Should().Contain("获取药材列表失败");
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsHerbInfo()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var detailDto = CreateTestHerbDetailDto(herbId);
            var apiResponse = CreateSuccessApiResponse(detailDto);

            _mockApiService
                .Setup(x => x.GetHerbByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetByIdAsync(herbId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(herbId);
            result.Name.Should().Be("人参");
            result.Price.Should().Be(280.00m);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var apiResponse = CreateFailureApiResponse<HerbDetailDto>();

            _mockApiService
                .Setup(x => x.GetHerbByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetByIdAsync(herbId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CreateHerbAsync Tests

        [Fact]
        public async Task CreateHerbAsync_WithValidDto_ReturnsSuccess()
        {
            // Arrange
            var createDto = CreateTestHerbCreateDto();
            var createdDto = CreateTestHerbDto();
            var apiResponse = CreateSuccessApiResponse(createdDto);

            _mockApiService
                .Setup(x => x.CreateHerbAsync(It.IsAny<HerbCreateDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.CreateHerbAsync(createDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockApiService.Verify(x => x.CreateHerbAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task CreateHerbAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var createDto = CreateTestHerbCreateDto();

            _mockApiService
                .Setup(x => x.CreateHerbAsync(It.IsAny<HerbCreateDto>()))
                .ThrowsAsync(new Exception("创建失败"));

            // Act
            var result = await _service.CreateHerbAsync(createDto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region UpdateHerbAsync Tests

        [Fact]
        public async Task UpdateHerbAsync_WithValidDto_ReturnsSuccess()
        {
            // Arrange
            var updateDto = CreateTestHerbUpdateDto();
            var updatedDto = CreateTestHerbDto(updateDto.Id);
            var apiResponse = CreateSuccessApiResponse(updatedDto);

            _mockApiService
                .Setup(x => x.UpdateHerbAsync(It.IsAny<Guid>(), It.IsAny<HerbUpdateDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.UpdateHerbAsync(updateDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockApiService.Verify(x => x.UpdateHerbAsync(updateDto.Id, updateDto), Times.Once);
        }

        [Fact]
        public async Task UpdateHerbAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var updateDto = CreateTestHerbUpdateDto();

            _mockApiService
                .Setup(x => x.UpdateHerbAsync(It.IsAny<Guid>(), It.IsAny<HerbUpdateDto>()))
                .ThrowsAsync(new Exception("更新失败"));

            // Act
            var result = await _service.UpdateHerbAsync(updateDto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region DeleteHerbAsync Tests

        [Fact]
        public async Task DeleteHerbAsync_WithValidId_CallsToggleStatus()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var apiResponse = CreateSuccessApiResponse(new object());

            _mockApiService
                .Setup(x => x.ToggleStatusAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.DeleteHerbAsync(herbId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            // 验证调用的是 ToggleStatusAsync 而不是 DELETE
            _mockApiService.Verify(x => x.ToggleStatusAsync(herbId), Times.Once);
        }

        [Fact]
        public async Task DeleteHerbAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var herbId = Guid.NewGuid();

            _mockApiService
                .Setup(x => x.ToggleStatusAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("删除失败"));

            // Act
            var result = await _service.DeleteHerbAsync(herbId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region UpdateStatusAsync Tests

        [Fact]
        public async Task UpdateStatusAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var statusDto = CreateTestCommonStatusUpdateDto();
            var apiResponse = CreateSuccessApiResponse(new object());

            _mockApiService
                .Setup(x => x.UpdateStatusAsync(It.IsAny<CommonStatusUpdateDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.UpdateStatusAsync(herbId, statusDto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockApiService.Verify(x => x.UpdateStatusAsync(statusDto), Times.Once);
        }

        #endregion

        #region GetAvailableHerbsAsync Tests

        [Fact]
        public async Task GetAvailableHerbsAsync_ReturnsAvailableHerbs()
        {
            // Arrange
            var herbList = new List<HerbDto> { CreateTestHerbDto(), CreateTestHerbDto() };
            var apiResponse = CreateSuccessApiResponse(herbList);

            _mockApiService
                .Setup(x => x.GetAvailableHerbsAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetAvailableHerbsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        #endregion

        #region GetOutOfStockHerbsAsync Tests

        [Fact]
        public async Task GetOutOfStockHerbsAsync_ReturnsOutOfStockHerbs()
        {
            // Arrange
            var herbList = new List<HerbDto> { CreateTestHerbDto() };
            var apiResponse = CreateSuccessApiResponse(herbList);

            _mockApiService
                .Setup(x => x.GetOutOfStockHerbsAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetOutOfStockHerbsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
        }

        #endregion

        #region GetExpiringHerbsAsync Tests

        [Fact]
        public async Task GetExpiringHerbsAsync_WithDefaultDays_ReturnsExpiringHerbs()
        {
            // Arrange
            var herbList = new List<HerbDto> { CreateTestHerbDto() };
            var apiResponse = CreateSuccessApiResponse(herbList);

            _mockApiService
                .Setup(x => x.GetExpiringHerbsAsync(30))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetExpiringHerbsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            _mockApiService.Verify(x => x.GetExpiringHerbsAsync(30), Times.Once);
        }

        [Fact]
        public async Task GetExpiringHerbsAsync_WithCustomDays_UsesProvidedValue()
        {
            // Arrange
            const int customDays = 60;
            var herbList = new List<HerbDto> { CreateTestHerbDto() };
            var apiResponse = CreateSuccessApiResponse(herbList);

            _mockApiService
                .Setup(x => x.GetExpiringHerbsAsync(customDays))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetExpiringHerbsAsync(customDays);

            // Assert
            result.Should().NotBeNull();
            _mockApiService.Verify(x => x.GetExpiringHerbsAsync(customDays), Times.Once);
        }

        #endregion

        #region GetStatisticsAsync Tests

        [Fact]
        public async Task GetStatisticsAsync_ReturnsStatisticsDictionary()
        {
            // Arrange
            var statistics = new Dictionary<int, int>
            {
                { 0, 100 }, // 启用
                { 1, 20 }   // 禁用
            };
            var apiResponse = CreateSuccessApiResponse(statistics);

            _mockApiService
                .Setup(x => x.GetStatisticsAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Should().Be(100);
            result[1].Should().Be(20);
        }

        #endregion

        #region ImportHerbsAsync Tests

        [Fact]
        public async Task ImportHerbsAsync_WithValidHerbs_ReturnsImportedCount()
        {
            // Arrange
            var importList = new List<HerbImportDto> 
            { 
                CreateTestHerbImportDto(),
                CreateTestHerbImportDto() 
            };
            var apiResponse = CreateSuccessApiResponse(2);

            _mockApiService
                .Setup(x => x.ImportHerbsAsync(It.IsAny<List<HerbImportDto>>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.ImportHerbsAsync(importList);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(2);
        }

        #endregion

        #region ExportHerbsAsync Tests

        [Fact]
        public async Task ExportHerbsAsync_ReturnsAllHerbsForExport()
        {
            // Arrange
            var detailList = new List<HerbDetailDto> 
            { 
                CreateTestHerbDetailDto(),
                CreateTestHerbDetailDto() 
            };
            var apiResponse = CreateSuccessApiResponse(detailList);

            _mockApiService
                .Setup(x => x.ExportHerbsAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.ExportHerbsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.First().Name.Should().Be("人参");
        }

        #endregion
    }
}