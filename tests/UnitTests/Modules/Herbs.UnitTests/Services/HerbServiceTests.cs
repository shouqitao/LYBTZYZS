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

        #region 补充查询操作测试

        [Fact]
        public async Task GetAllAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var herbs = new List<HerbDto>
            {
                new() { Id = Guid.NewGuid(), Name = "当归" },
                new() { Id = Guid.NewGuid(), Name = "黄芪" }
            };
            var expectedResult = ServiceResult<List<HerbDto>>.Success(herbs);

            _mockQueryService.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetAllAsync();

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdsAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var herbs = new List<HerbDto>
            {
                new() { Id = ids[0], Name = "当归" },
                new() { Id = ids[1], Name = "黄芪" }
            };
            var expectedResult = ServiceResult<List<HerbDto>>.Success(herbs);

            _mockQueryService.Setup(x => x.GetByIdsAsync(ids)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetByIdsAsync(ids);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdsAsync(ids), Times.Once);
        }

        [Fact]
        public async Task GetHerbsAsync_Should_Delegate_To_GetAllAsync()
        {
            // Arrange
            var herbs = new List<HerbDto>
            {
                new() { Id = Guid.NewGuid(), Name = "当归" }
            };
            var expectedResult = ServiceResult<List<HerbDto>>.Success(herbs);

            _mockQueryService.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetHerbsAsync();

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task SearchByNameAsync_Should_Delegate_To_SearchAsync()
        {
            // Arrange
            var name = "当归";
            var herbs = new List<HerbDto>
            {
                new() { Name = "当归" },
                new() { Name = "当归尾" }
            };
            var expectedResult = ServiceResult<List<HerbDto>>.Success(herbs);

            _mockQueryService.Setup(x => x.SearchAsync(name)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.SearchByNameAsync(name);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.SearchAsync(name), Times.Once);
        }

        [Fact]
        public async Task GetListAsync_Should_Return_All_When_Query_Is_Null()
        {
            // Arrange
            var herbs = new List<HerbDto>
            {
                new() { Id = Guid.NewGuid(), Name = "当归" }
            };
            var expectedResult = ServiceResult<List<HerbDto>>.Success(herbs);

            _mockQueryService.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetListAsync(null);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetListAsync_Should_Return_Items_From_Paged_Result_When_Query_Provided()
        {
            // Arrange
            var query = new HerbSearchDto { PageIndex = 1, PageSize = 10 };
            var herbs = new List<HerbDto>
            {
                new() { Id = Guid.NewGuid(), Name = "当归" }
            };
            var pagedResult = new PagedResult<HerbDto>
            {
                Items = herbs,
                TotalCount = 1,
                CurrentPage = 1,
                PageSize = 10
            };
            var expectedResult = ServiceResult<PagedResult<HerbDto>>.Success(pagedResult);

            _mockQueryService.Setup(x => x.GetPagedAsync(query)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetListAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(herbs);
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        [Fact]
        public async Task GetListAsync_Should_Return_Failure_When_Paged_Query_Fails()
        {
            // Arrange
            var query = new HerbSearchDto { PageIndex = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<HerbDto>>.Failure("查询失败");

            _mockQueryService.Setup(x => x.GetPagedAsync(query)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.GetListAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("查询失败");
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        #endregion

        #region 批量操作和导入导出测试

        [Fact]
        public async Task ImportHerbsAsync_Should_Return_Not_Supported_Failure()
        {
            // Arrange
            var herbs = new List<HerbCreateDto>
            {
                new() { Name = "当归", Origin = "甘肃", Price = 0.8m, Unit = "g" }
            };

            // Act
            var result = await _herbService.ImportHerbsAsync(herbs);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("简单诊所版本暂不支持药材批量导入功能");
        }

        [Fact]
        public async Task ExportHerbsAsync_Should_Export_CSV_Successfully()
        {
            // Arrange
            var query = new PagedQueryBaseDto { Keyword = "当归" };
            var herbs = new List<HerbDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "当归",
                    Origin = "甘肃",
                    Spec = "片",
                    Unit = "g",
                    Price = 0.8m,
                    IsEnabled = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "当归尾",
                    Origin = "甘肃",
                    Spec = "段",
                    Unit = "g",
                    Price = 0.9m,
                    IsEnabled = false
                }
            };
            var expectedResult = ServiceResult<List<HerbDto>>.Success(herbs);

            _mockQueryService.Setup(x => x.GetAvailableHerbsAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.ExportHerbsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            var csvContent = System.Text.Encoding.UTF8.GetString(result.Data!);
            csvContent.Should().Contain("药材名称,产地,规格,单位,价格,状态");
            csvContent.Should().Contain("当归,甘肃,片,g,0.8,启用");
            csvContent.Should().Contain("当归尾,甘肃,段,g,0.9,禁用");
        }

        [Fact]
        public async Task ExportHerbsAsync_Should_Return_Failure_When_Query_Fails()
        {
            // Arrange
            var query = new PagedQueryBaseDto { Keyword = "test" };
            var expectedResult = ServiceResult<List<HerbDto>>.Failure("获取药材列表失败");

            _mockQueryService.Setup(x => x.GetAvailableHerbsAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.ExportHerbsAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("获取药材列表失败");
        }

        [Fact]
        public async Task BatchUpdateStatusAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var status = true;
            var reason = "批量启用";
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.BatchUpdateStatusAsync(ids, status, reason))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.BatchUpdateStatusAsync(ids, status, reason);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.BatchUpdateStatusAsync(ids, status, reason), Times.Once);
        }

        [Fact]
        public async Task EnableAsync_Should_Set_Status_To_True()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.SetStatusAsync(herbId, true)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.EnableAsync(herbId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockBusinessService.Verify(x => x.SetStatusAsync(herbId, true), Times.Once);
        }

        [Fact]
        public async Task DisableAsync_Should_Set_Status_To_False()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.SetStatusAsync(herbId, false)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.DisableAsync(herbId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockBusinessService.Verify(x => x.SetStatusAsync(herbId, false), Times.Once);
        }

        [Fact]
        public async Task EnableAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Failure("启用失败");

            _mockBusinessService.Setup(x => x.SetStatusAsync(herbId, true)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.EnableAsync(herbId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("启用失败");
        }

        [Fact]
        public async Task DisableAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange
            var herbId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Failure("禁用失败");

            _mockBusinessService.Setup(x => x.SetStatusAsync(herbId, false)).ReturnsAsync(expectedResult);

            // Act
            var result = await _herbService.DisableAsync(herbId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("禁用失败");
        }

        [Fact]
        public async Task GetImportTemplateAsync_Should_Return_Template_Content()
        {
            // Act
            var result = await _herbService.GetImportTemplateAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            var templateContent = System.Text.Encoding.UTF8.GetString(result.Data!);
            templateContent.Should().Contain("药材导入模板 - UltraThink精简版");
            templateContent.Should().Contain("必填列：药材名称, 产地, 规格, 单位, 价格");
            templateContent.Should().Contain("可选列：功效, 用法, 备注, 状态(Enabled/Disabled)");
        }

        #endregion

        #region 统计和遗留方法测试

        [Fact]
        public async Task GetStatisticsAsync_Should_Return_Empty_Dictionary()
        {
            // Act
            var result = await _herbService.GetStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
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