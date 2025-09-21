using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Formula.Interfaces;
using LYBT.Module.Formula.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Moq;
using Xunit;

namespace LYBT.Module.Formula.Tests.Services
{
    /// <summary>
    /// FormulaService 完整单元测试 - UltraThink双层架构
    /// </summary>
    public class FormulaServiceTests
    {
        private readonly FormulaService _formulaService;
        private readonly Mock<IFormulaQueryService> _mockQueryService;
        private readonly Mock<IFormulaBusinessService> _mockBusinessService;

        public FormulaServiceTests()
        {
            _mockQueryService = new Mock<IFormulaQueryService>();
            _mockBusinessService = new Mock<IFormulaBusinessService>();
            _formulaService = new FormulaService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_Throw_When_QueryService_Is_Null()
        {
            var action = () => new FormulaService(null!, _mockBusinessService.Object);
            action.Should().Throw<ArgumentNullException>().WithParameterName("queryService");
        }

        [Fact]
        public void Constructor_Should_Throw_When_BusinessService_Is_Null()
        {
            var action = () => new FormulaService(_mockQueryService.Object, null!);
            action.Should().Throw<ArgumentNullException>().WithParameterName("businessService");
        }

        #endregion

        #region 查询操作测试

        [Fact]
        public async Task GetPagedAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var query = new FormulaQueryDto { PageIndex = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<FormulaDto>>.Success(new PagedResult<FormulaDto>());

            _mockQueryService.Setup(x => x.GetPagedAsync(query)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.GetPagedAsync(query);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var formulaDto = new FormulaDto { Id = formulaId, Name = "四君子汤" };
            var expectedResult = ServiceResult<FormulaDto>.Success(formulaDto);

            _mockQueryService.Setup(x => x.GetByIdAsync(formulaId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.GetByIdAsync(formulaId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdAsync(formulaId), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var keyword = "四君子";
            var formulas = new List<FormulaDto>
            {
                new() { Name = "四君子汤" },
                new() { Name = "四君子丸" }
            };
            var expectedResult = ServiceResult<List<FormulaDto>>.Success(formulas);

            _mockQueryService.Setup(x => x.SearchAsync(keyword)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.SearchAsync(keyword);

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
            var createDto = new FormulaCreateDto { Name = "四君子汤", Effect = "益气健脾", Usage = "水煎服", Herbs = new List<FormulaHerbItemCreateDto>() };
            var createdFormula = new FormulaDto { Id = Guid.NewGuid(), Name = "四君子汤" };
            var expectedResult = ServiceResult<FormulaDto>.Success(createdFormula);

            _mockBusinessService.Setup(x => x.CreateAsync(createDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.CreateAsync(createDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CreateAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var updateDto = new FormulaUpdateDto { Id = formulaId, Name = "加味四君子汤", Herbs = new List<FormulaHerbItemUpdateDto>() };
            var updatedFormula = new FormulaDto { Id = formulaId, Name = "加味四君子汤" };
            var expectedResult = ServiceResult<FormulaDto>.Success(updatedFormula);

            _mockBusinessService.Setup(x => x.UpdateAsync(formulaId, updateDto)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.UpdateAsync(formulaId, updateDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.UpdateAsync(formulaId, updateDto), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.DeleteAsync(formulaId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.DeleteAsync(formulaId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.DeleteAsync(formulaId), Times.Once);
        }

        #endregion


        #region 方剂分类测试

        [Fact]
        public async Task GetByTypeAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var formulaType = "经方";
            var formulas = new List<FormulaDto>
            {
                new() { Name = "四君子汤" },
                new() { Name = "六君子汤" }
            };
            var expectedResult = ServiceResult<List<FormulaDto>>.Success(formulas);

            _mockQueryService.Setup(x => x.GetByTypeAsync(formulaType)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.GetByTypeAsync(formulaType);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByTypeAsync(formulaType), Times.Once);
        }

        [Fact]
        public async Task GetCategoriesAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var categories = new List<string> { "内科方", "外科方", "妇科方" };
            var expectedResult = ServiceResult<List<string>>.Success(categories);

            _mockQueryService.Setup(x => x.GetCategoriesAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.GetCategoriesAsync();

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetCategoriesAsync(), Times.Once);
        }

        #endregion


        #region 边界值测试

        [Fact]
        public void FormulaService_Should_Implement_IFormulaService()
        {
            _formulaService.Should().BeAssignableTo<IFormulaService>();
        }

        #endregion
    }
}