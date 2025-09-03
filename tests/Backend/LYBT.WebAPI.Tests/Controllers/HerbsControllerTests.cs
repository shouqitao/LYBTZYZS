using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Herbs.Controllers;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.WebAPI.Tests.Controllers
{
    /// <summary>
    /// 药材控制器单元测试
    /// 测试优化后的RESTful接口和软删除功能
    /// </summary>
    public class HerbsControllerTests
    {
        private readonly Mock<IHerbService> _mockHerbService;
        private readonly Mock<ILogger<HerbsController>> _mockLogger;
        private readonly HerbsController _controller;

        public HerbsControllerTests()
        {
            _mockHerbService = new Mock<IHerbService>();
            _mockLogger = new Mock<ILogger<HerbsController>>();
            _controller = new HerbsController(_mockHerbService.Object, _mockLogger.Object);
        }

        #region GET /api/herbs Tests

        [Fact]
        public async Task GetHerbs_WithoutFilters_ReturnsOkResultWithAllHerbs()
        {
            // Arrange
            var expectedData = new PaginatedResult<HerbDto>
            {
                Items = new List<HerbDto>
                {
                    new HerbDto 
                    { 
                        Id = Guid.NewGuid(), 
                        Name = "人参", 
                        Origin = "东北",
                        Specification = "10g/包",
                        UnitPrice = 50.00m,
                        StockQuantity = 100,
                        IsActive = true
                    },
                    new HerbDto 
                    { 
                        Id = Guid.NewGuid(), 
                        Name = "当归", 
                        Origin = "甘肃",
                        Specification = "5g/包",
                        UnitPrice = 30.00m,
                        StockQuantity = 200,
                        IsActive = true
                    }
                },
                TotalCount = 2,
                PageNumber = 1,
                PageSize = 10
            };

            _mockHerbService.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>())
            ).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetHerbs();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<PaginatedResult<HerbDto>>().Subject;
            returnValue.Items.Should().HaveCount(2);
            returnValue.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetHerbs_WithPriceRangeFilter_ReturnsHerbsInPriceRange()
        {
            // Arrange
            decimal minPrice = 20.00m;
            decimal maxPrice = 50.00m;
            
            var expectedData = new PaginatedResult<HerbDto>
            {
                Items = new List<HerbDto>
                {
                    new HerbDto { Id = Guid.NewGuid(), Name = "当归", UnitPrice = 30.00m },
                    new HerbDto { Id = Guid.NewGuid(), Name = "人参", UnitPrice = 50.00m }
                },
                TotalCount = 2
            };

            _mockHerbService.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                minPrice,
                maxPrice,
                It.IsAny<bool?>(),
                It.IsAny<bool?>())
            ).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetHerbs(minPrice: minPrice, maxPrice: maxPrice);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<PaginatedResult<HerbDto>>().Subject;
            returnValue.Items.Should().OnlyContain(h => h.UnitPrice >= minPrice && h.UnitPrice <= maxPrice);
        }

        [Fact]
        public async Task GetHerbs_WithLowStockFilter_ReturnsOnlyLowStockHerbs()
        {
            // Arrange
            var expectedData = new PaginatedResult<HerbDto>
            {
                Items = new List<HerbDto>
                {
                    new HerbDto { Id = Guid.NewGuid(), Name = "低库存药材", StockQuantity = 5 }
                },
                TotalCount = 1
            };

            _mockHerbService.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                true, // lowStock = true
                It.IsAny<bool?>())
            ).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetHerbs(lowStock: true);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<PaginatedResult<HerbDto>>().Subject;
            returnValue.Items.Should().OnlyContain(h => h.StockQuantity < 10); // 假设10为低库存阈值
        }

        #endregion

        #region GET /api/herbs/{id} Tests

        [Fact]
        public async Task GetHerb_WithExistingId_ReturnsOkResultWithHerb()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var expectedHerb = new HerbDto
            {
                Id = herbId,
                Name = "人参",
                Origin = "东北",
                Specification = "10g/包",
                UnitPrice = 50.00m,
                StockQuantity = 100
            };

            _mockHerbService.Setup(x => x.GetByIdAsync(herbId))
                .ReturnsAsync(expectedHerb);

            // Act
            var result = await _controller.GetHerb(herbId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<HerbDto>().Subject;
            returnValue.Id.Should().Be(herbId);
            returnValue.Name.Should().Be("人参");
        }

        #endregion

        #region POST /api/herbs Tests

        [Fact]
        public async Task CreateHerb_WithValidData_ReturnsOkResultWithCreatedHerb()
        {
            // Arrange
            var createDto = new HerbCreateDto
            {
                Name = "新药材",
                Origin = "云南",
                Specification = "15g/包",
                UnitPrice = 45.00m,
                StockQuantity = 150,
                Unit = "克",
                Category = "补益类"
            };

            var createdHerb = new HerbDto
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Origin = createDto.Origin,
                Specification = createDto.Specification,
                UnitPrice = createDto.UnitPrice,
                StockQuantity = createDto.StockQuantity,
                IsActive = true
            };

            _mockHerbService.Setup(x => x.CreateAsync(createDto))
                .ReturnsAsync(createdHerb);

            // Act
            var result = await _controller.CreateHerb(createDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<HerbDto>().Subject;
            returnValue.Name.Should().Be("新药材");
            returnValue.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task CreateHerb_WithDuplicateName_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new HerbCreateDto
            {
                Name = "人参", // 已存在的药材名
                Origin = "东北",
                UnitPrice = 50.00m
            };

            _mockHerbService.Setup(x => x.CreateAsync(createDto))
                .ThrowsAsync(new InvalidOperationException("药材名称已存在"));

            // Act
            var act = async () => await _controller.CreateHerb(createDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("药材名称已存在");
        }

        #endregion

        #region PUT /api/herbs/{id} Tests

        [Fact]
        public async Task UpdateHerb_WithValidData_ReturnsOkResultWithUpdatedHerb()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var updateDto = new HerbEditDto
            {
                Id = herbId,
                Name = "更新后的人参",
                UnitPrice = 55.00m,
                StockQuantity = 120
            };

            var updatedHerb = new HerbDto
            {
                Id = herbId,
                Name = updateDto.Name,
                UnitPrice = updateDto.UnitPrice,
                StockQuantity = updateDto.StockQuantity
            };

            _mockHerbService.Setup(x => x.UpdateAsync(It.IsAny<HerbEditDto>()))
                .ReturnsAsync(updatedHerb);

            // Act
            var result = await _controller.UpdateHerb(herbId, updateDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<HerbDto>().Subject;
            returnValue.UnitPrice.Should().Be(55.00m);
            
            // 验证ID被正确设置
            _mockHerbService.Verify(x => x.UpdateAsync(It.Is<HerbEditDto>(dto => dto.Id == herbId)), Times.Once);
        }

        [Fact]
        public async Task UpdateHerb_UpdateStockQuantity_ReturnsOkResult()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var updateDto = new HerbEditDto
            {
                Id = herbId,
                StockQuantity = 50 // 只更新库存
            };

            var updatedHerb = new HerbDto
            {
                Id = herbId,
                Name = "人参",
                StockQuantity = 50
            };

            _mockHerbService.Setup(x => x.UpdateAsync(It.IsAny<HerbEditDto>()))
                .ReturnsAsync(updatedHerb);

            // Act
            var result = await _controller.UpdateHerb(herbId, updateDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<HerbDto>().Subject;
            returnValue.StockQuantity.Should().Be(50);
        }

        #endregion

        #region DELETE /api/herbs/{id} Tests

        [Fact]
        public async Task DeleteHerb_PerformsSoftDelete_ReturnsOkResultWithSuccessMessage()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            _mockHerbService.Setup(x => x.DeleteAsync(herbId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteHerb(herbId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { message = "药材删除成功" });
            
            // 验证是软删除
            _mockHerbService.Verify(x => x.DeleteAsync(herbId), Times.Once);
        }

        [Fact]
        public async Task DeleteHerb_WithNonExistingId_ReturnsBadRequest()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            _mockHerbService.Setup(x => x.DeleteAsync(herbId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteHerb(herbId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeEquivalentTo(new { message = "药材删除失败" });
        }

        #endregion

        #region POST /api/herbs/{id}/toggle-status Tests

        [Fact]
        public async Task ToggleStatus_ActiveHerb_ReturnsOkResultWithInactiveHerb()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var toggledHerb = new HerbDto
            {
                Id = herbId,
                Name = "人参",
                IsActive = false // 从活跃变为不活跃
            };

            _mockHerbService.Setup(x => x.ToggleStatusAsync(herbId))
                .ReturnsAsync(toggledHerb);

            // Act
            var result = await _controller.ToggleStatus(herbId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<HerbDto>().Subject;
            returnValue.IsActive.Should().BeFalse();
        }

        #endregion

        #region Integration and Edge Case Tests

        [Fact]
        public async Task GetHerbs_WithComplexFilters_CallsServiceWithAllParameters()
        {
            // Arrange
            var keyword = "参";
            var minPrice = 30.00m;
            var maxPrice = 100.00m;
            var lowStock = true;
            var isActive = true;

            _mockHerbService.Setup(x => x.GetPagedAsync(
                1, 10, keyword, minPrice, maxPrice, lowStock, isActive)
            ).ReturnsAsync(new PaginatedResult<HerbDto>());

            // Act
            await _controller.GetHerbs(1, 10, keyword, minPrice, maxPrice, lowStock, isActive);

            // Assert
            _mockHerbService.Verify(x => x.GetPagedAsync(
                1, 10, keyword, minPrice, maxPrice, lowStock, isActive), Times.Once);
        }

        [Fact]
        public async Task CreateHerb_WithNegativePrice_ThrowsValidationException()
        {
            // Arrange
            var createDto = new HerbCreateDto
            {
                Name = "无效药材",
                UnitPrice = -10.00m // 负数价格
            };

            _mockHerbService.Setup(x => x.CreateAsync(createDto))
                .ThrowsAsync(new ArgumentException("价格不能为负数"));

            // Act
            var act = async () => await _controller.CreateHerb(createDto);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("价格不能为负数");
        }

        #endregion
    }
}