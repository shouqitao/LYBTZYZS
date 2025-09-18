using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Herbs.Tests
{
    /// <summary>
    /// HerbService 简化单元测试 - UltraThink双层架构适配
    /// 专注于测试核心功能，Mock QueryService和BusinessService
    /// </summary>
    public class SimpleHerbServiceTests
    {
        private readonly HerbService _herbService;
        private readonly Mock<IHerbQueryService> _mockQueryService;
        private readonly Mock<IHerbBusinessService> _mockBusinessService;
        private readonly Mock<ILogger<HerbService>> _mockLogger;

        public SimpleHerbServiceTests()
        {
            // UltraThink双层架构Mock配置
            _mockQueryService = new Mock<IHerbQueryService>();
            _mockBusinessService = new Mock<IHerbBusinessService>();
            _mockLogger = new Mock<ILogger<HerbService>>();

            // 创建 HerbService 实例 (主Service委托模式)
            _herbService = new HerbService(
                _mockQueryService.Object,
                _mockBusinessService.Object,
                _mockLogger.Object);
        }

        #region GetAllAsync 测试

        [Fact]
        public async Task GetAllAsync_Should_Return_All_Herbs()
        {
            // Arrange
            var herbs = new List<HerbDto>
            {
                new() { Id = Guid.NewGuid(), Name = "甘草", Price = 5.00m },
                new() { Id = Guid.NewGuid(), Name = "黄芪", Price = 8.00m },
                new() { Id = Guid.NewGuid(), Name = "当归", Price = 12.00m }
            };

            var expectedResult = ServiceResult<List<HerbDto>>.Success(herbs);

            _mockQueryService
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(3);
        }

        #endregion

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_Should_Return_Paged_Result()
        {
            // Arrange
            var query = new HerbPagedQueryDto
            {
                PageIndex = 1,
                PageSize = 10
            };

            var herbs = new List<HerbDto>
            {
                new() { Id = Guid.NewGuid(), Name = "甘草", Price = 5.00m },
                new() { Id = Guid.NewGuid(), Name = "黄芪", Price = 8.00m }
            };

            var expectedResult = ServiceResult<PagedResult<HerbDto>>.Success(new PagedResult<HerbDto>
            {
                Items = herbs,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 10
            });

            _mockQueryService
                .Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
        }

        #endregion

        #region SearchAsync 测试

        [Fact]
        public async Task SearchAsync_Should_Return_Matching_Herbs()
        {
            // Arrange
            var keyword = "甘";
            var herbs = new List<HerbDto>
            {
                new() { Id = Guid.NewGuid(), Name = "甘草", Price = 5.00m },
                new() { Id = Guid.NewGuid(), Name = "甘松", Price = 15.00m }
            };

            var expectedResult = ServiceResult<List<HerbDto>>.Success(herbs);

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Empty_When_No_Match()
        {
            // Arrange
            var keyword = "不存在的药材";
            var expectedResult = ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        #endregion

        #region GetAvailableHerbsAsync 测试

        [Fact]
        public async Task GetAvailableHerbsAsync_Should_Return_Available_Herbs()
        {
            // Arrange
            var availableHerbs = new List<HerbDto>
            {
                new() { Id = Guid.NewGuid(), Name = "甘草", Price = 5.00m, Status = CommonStatus.Enabled },
                new() { Id = Guid.NewGuid(), Name = "黄芪", Price = 8.00m, Status = CommonStatus.Enabled }
            };

            var expectedResult = ServiceResult<List<HerbDto>>.Success(availableHerbs);

            _mockQueryService
                .Setup(x => x.GetAvailableHerbsAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetAvailableHerbsAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
            result.Data.Should().OnlyContain(h => h.Status == CommonStatus.Enabled);
        }

        #endregion

        #region GetByPriceRangeAsync 测试

        [Fact]
        public async Task GetByPriceRangeAsync_Should_Return_Herbs_In_Price_Range()
        {
            // Arrange
            var minPrice = 5.00m;
            var maxPrice = 10.00m;
            var herbs = new List<HerbDto>
            {
                new() { Id = Guid.NewGuid(), Name = "甘草", Price = 5.00m },
                new() { Id = Guid.NewGuid(), Name = "黄芪", Price = 8.00m }
            };

            var expectedResult = ServiceResult<List<HerbDto>>.Success(herbs);

            _mockQueryService
                .Setup(x => x.GetByPriceRangeAsync(minPrice, maxPrice))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetByPriceRangeAsync(minPrice, maxPrice);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
            result.Data.Should().OnlyContain(h => h.Price >= minPrice && h.Price <= maxPrice);
        }

        #endregion

        #region 异常分支和边界值测试 (成功经验应用)

        [Fact]
        public async Task GetPagedAsync_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange - 业务失败分支测试
            var query = new HerbPagedQueryDto { PageIndex = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<HerbDto>>.Failure("查询服务异常");

            _mockQueryService
                .Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("查询服务异常");
        }

        [Fact]
        public async Task SearchAsync_With_Empty_Keyword_Should_Return_Empty_List()
        {
            // Arrange - 空值测试
            var keyword = string.Empty;
            var expectedResult = ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByPriceRangeAsync_With_Invalid_Range_Should_Return_Empty()
        {
            // Arrange - 边界值测试：无效价格范围
            var minPrice = 100.00m;
            var maxPrice = 50.00m; // 最小价格大于最大价格
            var expectedResult = ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());

            _mockQueryService
                .Setup(x => x.GetByPriceRangeAsync(minPrice, maxPrice))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetByPriceRangeAsync(minPrice, maxPrice);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPagedAsync_With_Large_PageSize_Should_Handle_Gracefully()
        {
            // Arrange - 极端值测试：大分页尺寸
            var query = new HerbPagedQueryDto { PageIndex = 1, PageSize = 999999 };
            var herbs = new List<HerbDto>
            {
                new() { Id = Guid.NewGuid(), Name = "中药材1", Price = 10.00m },
                new() { Id = Guid.NewGuid(), Name = "中药材2", Price = 20.00m }
            };

            var expectedResult = ServiceResult<PagedResult<HerbDto>>.Success(new PagedResult<HerbDto>
            {
                Items = herbs,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 999999
            });

            _mockQueryService
                .Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.PageSize.Should().Be(999999);
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange - 搜索失败测试
            var keyword = "甘草";
            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(ServiceResult<List<HerbDto>>.Failure("搜索服务异常"));

            // Act
            var result = await _herbService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("搜索服务异常");
        }


        #endregion
    }
}
