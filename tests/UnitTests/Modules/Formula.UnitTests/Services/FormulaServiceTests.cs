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

        #region 补充查询操作测试

        [Fact]
        public async Task SearchFormulasAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var query = new PagedQueryBaseDto { PageIndex = 1, PageSize = 10, Keyword = "四君子" };
            var expectedResult = ServiceResult<PagedResult<FormulaDto>>.Success(new PagedResult<FormulaDto>());

            _mockQueryService.Setup(x => x.SearchFormulasAsync(query)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.SearchFormulasAsync(query);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.SearchFormulasAsync(query), Times.Once);
        }

        [Fact]
        public async Task GetFormulasAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var keyword = "四君子";
            var category = "内科方";
            var expectedResult = ServiceResult<List<FormulaDto>>.Success(new List<FormulaDto>());

            _mockQueryService.Setup(x => x.GetFormulasAsync(keyword, category)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.GetFormulasAsync(keyword, category);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetFormulasAsync(keyword, category), Times.Once);
        }

        [Fact]
        public async Task GetAllFormulasAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var formulas = new List<FormulaDto>
            {
                new() { Id = Guid.NewGuid(), Name = "四君子汤" },
                new() { Id = Guid.NewGuid(), Name = "六君子汤" }
            };
            var expectedResult = ServiceResult<List<FormulaDto>>.Success(formulas);

            _mockQueryService.Setup(x => x.GetAllFormulasAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.GetAllFormulasAsync();

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetAllFormulasAsync(), Times.Once);
        }

        [Fact]
        public async Task GetTemplatesAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var templates = new List<FormulaDto>
            {
                new() { Id = Guid.NewGuid(), Name = "四君子汤", IsTemplate = true }
            };
            var expectedResult = ServiceResult<List<FormulaDto>>.Success(templates);

            _mockQueryService.Setup(x => x.GetTemplatesAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.GetTemplatesAsync();

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetTemplatesAsync(), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_Should_Return_True_When_Formula_Exists()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var formulaDto = new FormulaDto { Id = formulaId, Name = "四君子汤" };
            var expectedResult = ServiceResult<FormulaDto>.Success(formulaDto);

            _mockQueryService.Setup(x => x.GetByIdAsync(formulaId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.ExistsAsync(formulaId);

            // Assert
            result.Should().BeTrue();
            _mockQueryService.Verify(x => x.GetByIdAsync(formulaId), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_Should_Return_False_When_Formula_Not_Exists()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var expectedResult = ServiceResult<FormulaDto>.Failure("验方不存在");

            _mockQueryService.Setup(x => x.GetByIdAsync(formulaId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.ExistsAsync(formulaId);

            // Assert
            result.Should().BeFalse();
            _mockQueryService.Verify(x => x.GetByIdAsync(formulaId), Times.Once);
        }

        [Fact]
        public async Task IsNameDuplicatedAsync_Should_Return_True_When_Name_Exists()
        {
            // Arrange
            var name = "四君子汤";
            var formulas = new List<FormulaDto>
            {
                new() { Id = Guid.NewGuid(), Name = "四君子汤" },
                new() { Id = Guid.NewGuid(), Name = "六君子汤" }
            };
            var expectedResult = ServiceResult<List<FormulaDto>>.Success(formulas);

            _mockQueryService.Setup(x => x.GetAllFormulasAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.IsNameDuplicatedAsync(name);

            // Assert
            result.Should().BeTrue();
            _mockQueryService.Verify(x => x.GetAllFormulasAsync(), Times.Once);
        }

        [Fact]
        public async Task IsNameDuplicatedAsync_Should_Return_False_When_Name_Not_Exists()
        {
            // Arrange
            var name = "新验方";
            var formulas = new List<FormulaDto>
            {
                new() { Id = Guid.NewGuid(), Name = "四君子汤" }
            };
            var expectedResult = ServiceResult<List<FormulaDto>>.Success(formulas);

            _mockQueryService.Setup(x => x.GetAllFormulasAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.IsNameDuplicatedAsync(name);

            // Assert
            result.Should().BeFalse();
            _mockQueryService.Verify(x => x.GetAllFormulasAsync(), Times.Once);
        }

        [Fact]
        public async Task IsNameDuplicatedAsync_Should_Exclude_Specified_Id()
        {
            // Arrange
            var name = "四君子汤";
            var excludeId = Guid.NewGuid();
            var formulas = new List<FormulaDto>
            {
                new() { Id = excludeId, Name = "四君子汤" }, // 这个要被排除
                new() { Id = Guid.NewGuid(), Name = "六君子汤" }
            };
            var expectedResult = ServiceResult<List<FormulaDto>>.Success(formulas);

            _mockQueryService.Setup(x => x.GetAllFormulasAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.IsNameDuplicatedAsync(name, excludeId);

            // Assert
            result.Should().BeFalse(); // 排除自己后，没有重复
            _mockQueryService.Verify(x => x.GetAllFormulasAsync(), Times.Once);
        }

        [Fact]
        public async Task IsNameDuplicatedAsync_Should_Return_False_When_Name_Is_Empty()
        {
            // Act
            var result = await _formulaService.IsNameDuplicatedAsync("");

            // Assert
            result.Should().BeFalse();
            _mockQueryService.Verify(x => x.GetAllFormulasAsync(), Times.Never);
        }

        #endregion

        #region 状态管理和业务操作测试

        [Fact]
        public async Task EnableAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var expectedResult = ServiceResult.Success();

            _mockBusinessService.Setup(x => x.EnableAsync(formulaId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.EnableAsync(formulaId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.EnableAsync(formulaId), Times.Once);
        }

        [Fact]
        public async Task DisableAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var expectedResult = ServiceResult.Success();

            _mockBusinessService.Setup(x => x.DisableAsync(formulaId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.DisableAsync(formulaId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.DisableAsync(formulaId), Times.Once);
        }

        [Fact]
        public async Task ToggleStatusAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.ToggleStatusAsync(formulaId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.ToggleStatusAsync(formulaId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.ToggleStatusAsync(formulaId), Times.Once);
        }

        [Fact]
        public async Task CopyAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var newName = "新验方名称";
            var copiedFormula = new FormulaDto { Id = Guid.NewGuid(), Name = newName };
            var expectedResult = ServiceResult<FormulaDto>.Success(copiedFormula);

            _mockBusinessService.Setup(x => x.CopyAsync(formulaId, newName)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.CopyAsync(formulaId, newName);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CopyAsync(formulaId, newName), Times.Once);
        }

        [Fact]
        public async Task AnalyzeFormulaAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var analysisResult = new FormulaAnalysisResult { FormulaId = formulaId, Summary = "分析结果" };
            var expectedResult = ServiceResult<FormulaAnalysisResult>.Success(analysisResult);

            _mockBusinessService.Setup(x => x.AnalyzeFormulaAsync(formulaId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.AnalyzeFormulaAsync(formulaId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.AnalyzeFormulaAsync(formulaId), Times.Once);
        }

        [Fact]
        public async Task CreateFromPrescriptionAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var prescriptionId = Guid.NewGuid();
            var name = "从处方创建的验方";
            var createdFormula = new FormulaDto { Id = Guid.NewGuid(), Name = name };
            var expectedResult = ServiceResult<FormulaDto>.Success(createdFormula);

            _mockBusinessService.Setup(x => x.CreateFromPrescriptionAsync(prescriptionId, name)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.CreateFromPrescriptionAsync(prescriptionId, name);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CreateFromPrescriptionAsync(prescriptionId, name), Times.Once);
        }

        [Fact]
        public async Task ShareFormulaAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.ShareFormulaAsync(formulaId, operatorId, operatorName)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.ShareFormulaAsync(formulaId, operatorId, operatorName);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.ShareFormulaAsync(formulaId, operatorId, operatorName), Times.Once);
        }

        [Fact]
        public async Task UnshareFormulaAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.UnshareFormulaAsync(formulaId, operatorId, operatorName)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.UnshareFormulaAsync(formulaId, operatorId, operatorName);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.UnshareFormulaAsync(formulaId, operatorId, operatorName), Times.Once);
        }

        [Fact]
        public async Task CloneFormulaAsync_Should_Delegate_To_CopyAsync()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var newName = "克隆的验方";
            var userId = Guid.NewGuid();
            var clonedFormula = new FormulaDto { Id = Guid.NewGuid(), Name = newName };
            var expectedResult = ServiceResult<FormulaDto>.Success(clonedFormula);

            _mockBusinessService.Setup(x => x.CopyAsync(formulaId, newName)).ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.CloneFormulaAsync(formulaId, newName, userId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CopyAsync(formulaId, newName), Times.Once);
        }

        #endregion

        #region 批量操作测试

        [Fact]
        public async Task ImportFormulasAsync_Should_Return_Not_Supported_Failure()
        {
            // Arrange
            var formulas = new List<FormulaCreateDto>
            {
                new() { Name = "四君子汤", Effect = "益气健脾", Usage = "水煎服", Herbs = new List<FormulaHerbItemCreateDto>() }
            };

            // Act
            var result = await _formulaService.ImportFormulasAsync(formulas);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("简单诊所版本暂不支持验方批量导入功能");
        }

        [Fact]
        public async Task ExportFormulasAsync_Should_Return_Not_Supported_Failure()
        {
            // Arrange
            var query = new PagedQueryBaseDto { Keyword = "四君子" };

            // Act
            var result = await _formulaService.ExportFormulasAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("简单诊所版本暂不支持验方批量导出功能");
        }

        #endregion

        #region 异常处理测试

        [Fact]
        public async Task CreateAsync_Should_Rethrow_Exception()
        {
            // Arrange
            var createDto = new FormulaCreateDto { Name = "四君子汤", Effect = "益气健脾", Usage = "水煎服", Herbs = new List<FormulaHerbItemCreateDto>() };
            var exception = new InvalidOperationException("测试异常");

            _mockBusinessService.Setup(x => x.CreateAsync(createDto)).ThrowsAsync(exception);

            // Act & Assert
            var act = async () => await _formulaService.CreateAsync(createDto);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("测试异常");
        }

        [Fact]
        public async Task UpdateAsync_Should_Rethrow_Exception()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var updateDto = new FormulaUpdateDto { Id = formulaId, Name = "加味四君子汤", Herbs = new List<FormulaHerbItemUpdateDto>() };
            var exception = new InvalidOperationException("测试异常");

            _mockBusinessService.Setup(x => x.UpdateAsync(formulaId, updateDto)).ThrowsAsync(exception);

            // Act & Assert
            var act = async () => await _formulaService.UpdateAsync(formulaId, updateDto);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("测试异常");
        }

        [Fact]
        public async Task DeleteAsync_Should_Rethrow_Exception()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var exception = new InvalidOperationException("测试异常");

            _mockBusinessService.Setup(x => x.DeleteAsync(formulaId)).ThrowsAsync(exception);

            // Act & Assert
            var act = async () => await _formulaService.DeleteAsync(formulaId);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("测试异常");
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