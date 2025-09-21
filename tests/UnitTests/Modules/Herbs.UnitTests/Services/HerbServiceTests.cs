using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
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

        public HerbServiceTests()
        {
            _mockQueryService = new Mock<IHerbQueryService>();
            _mockBusinessService = new Mock<IHerbBusinessService>();
            _herbService = new HerbService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_Throw_When_QueryService_Is_Null()
        {
            var action = () => new HerbService(null!, _mockBusinessService.Object);
            action.Should().Throw<ArgumentNullException>().WithParameterName("queryService");
        }

        [Fact]
        public void Constructor_Should_Throw_When_BusinessService_Is_Null()
        {
            var action = () => new HerbService(_mockQueryService.Object, null!);
            action.Should().Throw<ArgumentNullException>().WithParameterName("businessService");
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
            var createDto = new HerbCreateDto { Name = "当归", Properties = "甘、辛，温" };
            var createdHerb = new HerbDto { Id = Guid.NewGuid(), Name = "当归" };
            var expectedResult = ServiceResult<HerbDto>.Success(createdHerb);

            _mockBusinessService.Setup(x => x.CreateAsync(createDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.CreateAsync(createDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CreateAsync(createDto), Times.Once);
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
            var result = await _herbService.UpdateAsync(updateDto);

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

            _mockBusinessService.Setup(x => x.DeleteAsync(herbId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.DeleteAsync(herbId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.DeleteAsync(herbId), Times.Once);
        }

        #endregion

        #region 药材分类测试

        [Fact]
        public async Task GetByCategoryAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var category = "补血药";
            var herbs = new List<HerbDto>
            {
                new() { Name = "当归", Category = category },
                new() { Name = "熟地黄", Category = category }
            };
            var expectedResult = ServiceResult<List<HerbDto>>.Success(herbs);

            _mockQueryService.Setup(x => x.GetByCategoryAsync(category)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetByCategoryAsync(category);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByCategoryAsync(category), Times.Once);
        }

        [Fact]
        public async Task GetCategoriesAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var categories = new List<string> { "补血药", "补气药", "清热药" };
            var expectedResult = ServiceResult<List<string>>.Success(categories);

            _mockQueryService.Setup(x => x.GetCategoriesAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetCategoriesAsync();

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetCategoriesAsync(), Times.Once);
        }

        #endregion

        #region 兼容性检查测试

        [Fact]
        public async Task CheckCompatibilityAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var herbIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var compatibilityResult = new HerbCompatibilityResult { IsCompatible = true };
            var expectedResult = ServiceResult<HerbCompatibilityResult>.Success(compatibilityResult);

            _mockBusinessService.Setup(x => x.CheckCompatibilityAsync(herbIds)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.CheckCompatibilityAsync(herbIds);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CheckCompatibilityAsync(herbIds), Times.Once);
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