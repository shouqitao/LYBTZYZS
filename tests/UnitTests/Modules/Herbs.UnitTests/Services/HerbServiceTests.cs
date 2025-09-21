using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Herbs.Tests.Services
{
    /// <summary>
    /// HerbService 完整单元测试 - UltraThink双层架构
    /// </summary>
    public class HerbServiceTests
    {
        private readonly HerbService _herbService;
        private readonly Mock<IHerbQueryService> _mockQueryService;
        private readonly Mock<IHerbBusinessService> _mockBusinessService;
        private readonly Mock<ILogger<HerbService>> _mockLogger;

        public HerbServiceTests()
        {
            _mockQueryService = new Mock<IHerbQueryService>();
            _mockBusinessService = new Mock<IHerbBusinessService>();
            _mockLogger = new Mock<ILogger<HerbService>>();
            _herbService = new HerbService(_mockQueryService.Object, _mockBusinessService.Object, _mockLogger.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_Throw_When_QueryService_Is_Null()
        {
            var action = () => new HerbService(null!, _mockBusinessService.Object, _mockLogger.Object);
            action.Should().Throw<ArgumentNullException>().WithParameterName("queryService");
        }

        [Fact]
        public void Constructor_Should_Throw_When_BusinessService_Is_Null()
        {
            var action = () => new HerbService(_mockQueryService.Object, null!, _mockLogger.Object);
            action.Should().Throw<ArgumentNullException>().WithParameterName("businessService");
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            var action = () => new HerbService(_mockQueryService.Object, _mockBusinessService.Object, null!);
            action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        #endregion

        #region 查询操作测试

        [Fact]
        public async Task GetPagedAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var query = new HerbSearchDto { PageIndex = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<HerbDto>>.Success(new PagedResult<HerbDto>());

            _mockQueryService.Setup(x => x.GetPagedAsync(query)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetPagedAsync(query);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var herbDto = new HerbDto { Id = herbId, Name = "当归" };
            var expectedResult = ServiceResult<HerbDto>.Success(herbDto);

            _mockQueryService.Setup(x => x.GetByIdAsync(herbId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetByIdAsync(herbId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdAsync(herbId), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var keyword = "当归";
            var herbs = new List<HerbDto>
            {
                new() { Name = "当归" },
                new() { Name = "当归尾" }
            };
            var expectedResult = ServiceResult<List<HerbDto>>.Success(herbs);

            _mockQueryService.Setup(x => x.SearchAsync(keyword)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.SearchAsync(keyword);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.SearchAsync(keyword), Times.Once);
        }

        #endregion

        #region 业务操作测试

        [Fact]
        public async Task CreateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var createDto = new HerbCreateDto { Name = "当归", Origin = "甘肃", Price = 0.8m, Unit = "g" };
            var createdHerb = new HerbDto { Id = Guid.NewGuid(), Name = "当归" };
            var expectedResult = ServiceResult<HerbDto>.Success(createdHerb);

            _mockBusinessService.Setup(x => x.CreateHerbWithAutoCodeAsync(createDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.CreateAsync(createDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CreateHerbWithAutoCodeAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var updateDto = new HerbUpdateDto { Id = herbId, Name = "当归片" };
            var updatedHerb = new HerbDto { Id = herbId, Name = "当归片" };
            var expectedResult = ServiceResult<HerbDto>.Success(updatedHerb);

            _mockBusinessService.Setup(x => x.UpdateAsync(herbId, updateDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.UpdateAsync(herbId, updateDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.UpdateAsync(herbId, updateDto), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.SoftDeleteAsync(herbId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.DeleteAsync(herbId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.SoftDeleteAsync(herbId), Times.Once);
        }

        #endregion

        #region 药材分类测试

        [Fact]
        public async Task GetByPriceRangeAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var minPrice = 1.0m;
            var maxPrice = 10.0m;
            var herbs = new List<HerbDto>
            {
                new() { Name = "当归", Price = 5.0m },
                new() { Name = "黄芪", Price = 8.0m }
            };
            var expectedResult = ServiceResult<List<HerbDto>>.Success(herbs);

            _mockQueryService.Setup(x => x.GetByPriceRangeAsync(minPrice, maxPrice)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetByPriceRangeAsync(minPrice, maxPrice);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByPriceRangeAsync(minPrice, maxPrice), Times.Once);
        }

        [Fact]
        public async Task GetAvailableHerbsAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var herbs = new List<HerbDto>
            {
                new() { Name = "当归", Price = 5.0m },
                new() { Name = "黄芪", Price = 8.0m }
            };
            var expectedResult = ServiceResult<List<HerbDto>>.Success(herbs);

            _mockQueryService.Setup(x => x.GetAvailableHerbsAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetAvailableHerbsAsync();

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetAvailableHerbsAsync(), Times.Once);
        }

        #endregion

        #region 状态管理测试

        [Fact]
        public async Task SetStatusAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var isActive = true;
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.SetStatusAsync(herbId, isActive)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.SetStatusAsync(herbId, isActive);

            // Assert
            result.Should().BeTrue();
            _mockBusinessService.Verify(x => x.SetStatusAsync(herbId, isActive), Times.Once);
        }

        #endregion

        #region 边界值测试

        [Fact]
        public void HerbService_Should_Implement_IHerbService()
        {
            _herbService.Should().BeAssignableTo<IHerbService>();
        }

        #endregion
    }
}