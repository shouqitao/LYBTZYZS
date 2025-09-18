using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Formula.Interfaces;
using LYBT.Module.Formula.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Formula.Tests
{
    /// <summary>
    /// FormulaService 简化单元测试 - UltraThink双层架构适配
    /// 专注于测试核心功能，Mock QueryService和BusinessService
    /// </summary>
    public class SimpleFormulaServiceTests
    {
        private readonly FormulaService _formulaService;
        private readonly Mock<IFormulaQueryService> _mockQueryService;
        private readonly Mock<IFormulaBusinessService> _mockBusinessService;

        public SimpleFormulaServiceTests()
        {
            // UltraThink双层架构Mock配置
            _mockQueryService = new Mock<IFormulaQueryService>();
            _mockBusinessService = new Mock<IFormulaBusinessService>();

            // 创建 FormulaService 实例 (主Service委托模式)
            _formulaService = new FormulaService(
                _mockQueryService.Object,
                _mockBusinessService.Object);
        }

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_Should_Return_Paged_Result()
        {
            // Arrange
            var query = new FormulaQueryDto
            {
                PageIndex = 1,
                PageSize = 10
            };

            var formulas = new List<FormulaDto>
            {
                new() { Id = Guid.NewGuid(), Name = "桂枝汤", Effect = "发汗解肌" },
                new() { Id = Guid.NewGuid(), Name = "麻黄汤", Effect = "发汗散寒" }
            };

            var expectedResult = ServiceResult<PagedResult<FormulaDto>>.Success(new PagedResult<FormulaDto>
            {
                Items = formulas,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 10
            });

            _mockQueryService
                .Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
        }

        #endregion

        #region GetFormulasAsync 测试

        [Fact]
        public async Task GetFormulasAsync_Should_Return_Formulas()
        {
            // Arrange
            var keyword = "桂枝";
            var category = "解表剂";
            var formulas = new List<FormulaDto>
            {
                new() { Id = Guid.NewGuid(), Name = "桂枝汤", Effect = "发汗解肌" },
                new() { Id = Guid.NewGuid(), Name = "桂枝加葛根汤", Effect = "发汗解肌" }
            };

            var expectedResult = ServiceResult<List<FormulaDto>>.Success(formulas);

            _mockQueryService
                .Setup(x => x.GetFormulasAsync(keyword, category))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.GetFormulasAsync(keyword, category);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
        }

        #endregion

        #region SearchAsync 测试

        [Fact]
        public async Task SearchAsync_Should_Return_Matching_Formulas()
        {
            // Arrange
            var keyword = "桂";
            var formulas = new List<FormulaDto>
            {
                new() { Id = Guid.NewGuid(), Name = "桂枝汤", Effect = "发汗解肌" },
                new() { Id = Guid.NewGuid(), Name = "桂枝加葛根汤", Effect = "发汗解肌" }
            };

            var expectedResult = ServiceResult<List<FormulaDto>>.Success(formulas);

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.SearchAsync(keyword);

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
            var keyword = "不存在的验方";
            var expectedResult = ServiceResult<List<FormulaDto>>.Success(new List<FormulaDto>());

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().BeEmpty();
        }

        #endregion

        #region GetCategoriesAsync 测试

        [Fact]
        public async Task GetCategoriesAsync_Should_Return_Categories()
        {
            // Arrange
            var categories = new List<string>
            {
                "解表剂", "清热剂", "泻下剂", "和解剂", "温里剂"
            };

            var expectedResult = ServiceResult<List<string>>.Success(categories);

            _mockQueryService
                .Setup(x => x.GetCategoriesAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.GetCategoriesAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(5);
        }

        #endregion

        #region CopyAsync 测试

        [Fact]
        public async Task CopyAsync_Should_Return_Success_When_BusinessService_Succeeds()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var newName = "复制的桂枝汤";
            var copiedFormula = new FormulaDto
            {
                Id = Guid.NewGuid(),
                Name = newName,
                Effect = "发汗解肌"
            };

            _mockBusinessService
                .Setup(x => x.CopyAsync(formulaId, newName))
                .ReturnsAsync(ServiceResult<FormulaDto>.Success(copiedFormula));

            // Act
            var result = await _formulaService.CopyAsync(formulaId, newName);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be(newName);
        }

        #endregion

        #region ShareFormulaAsync 测试

        [Fact]
        public async Task ShareFormulaAsync_Should_Return_Success_When_BusinessService_Succeeds()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();
            var operatorName = "测试医生";

            _mockBusinessService
                .Setup(x => x.ShareFormulaAsync(formulaId, operatorId, operatorName))
                .ReturnsAsync(ServiceResult<bool>.Success(true));

            // Act
            var result = await _formulaService.ShareFormulaAsync(formulaId, operatorId, operatorName);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        #endregion

        #region GetTemplatesAsync 测试

        [Fact]
        public async Task GetTemplatesAsync_Should_Return_Templates()
        {
            // Arrange
            var templates = new List<FormulaDto>
            {
                new() { Id = Guid.NewGuid(), Name = "桂枝汤", Effect = "发汗解肌", IsShared = true },
                new() { Id = Guid.NewGuid(), Name = "麻黄汤", Effect = "发汗散寒", IsShared = true }
            };

            var expectedResult = ServiceResult<List<FormulaDto>>.Success(templates);

            _mockQueryService
                .Setup(x => x.GetTemplatesAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.GetTemplatesAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
            result.Data.Should().OnlyContain(f => f.IsShared == true);
        }

        #endregion

        #region 异常分支和边界值测试 (成功经验应用)

        [Fact]
        public async Task GetPagedAsync_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange - 业务失败分支测试
            var query = new FormulaQueryDto
            {
                PageIndex = 1,
                PageSize = 10
            };
            var expectedResult = ServiceResult<PagedResult<FormulaDto>>.Failure("查询服务异常");

            _mockQueryService
                .Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("查询服务异常");
        }

        [Fact]
        public async Task SearchAsync_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange - 查询失败测试
            var keyword = "桂枝";
            var expectedResult = ServiceResult<List<FormulaDto>>.Failure("搜索服务异常");

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("搜索服务异常");
        }

        [Fact]
        public async Task SearchAsync_With_Empty_Keyword_Should_Return_Empty_List()
        {
            // Arrange - 空值测试
            var keyword = string.Empty;
            var expectedResult = ServiceResult<List<FormulaDto>>.Success(new List<FormulaDto>());

            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.SearchAsync(keyword);

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
            var query = new FormulaQueryDto
            {
                PageIndex = 1,
                PageSize = 999999 // 极端大值
            };

            var formulas = new List<FormulaDto>
            {
                new() { Id = Guid.NewGuid(), Name = "验方1", Effect = "功效1" },
                new() { Id = Guid.NewGuid(), Name = "验方2", Effect = "功效2" }
            };

            var expectedResult = ServiceResult<PagedResult<FormulaDto>>.Success(new PagedResult<FormulaDto>
            {
                Items = formulas,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 999999
            });

            _mockQueryService
                .Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _formulaService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.PageSize.Should().Be(999999);
        }

        [Fact]
        public async Task CopyAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange - 业务操作失败测试
            var formulaId = Guid.NewGuid();
            var newName = "复制的验方";

            _mockBusinessService
                .Setup(x => x.CopyAsync(formulaId, newName))
                .ReturnsAsync(ServiceResult<FormulaDto>.Failure("复制验方失败"));

            // Act
            var result = await _formulaService.CopyAsync(formulaId, newName);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("复制验方失败");
        }

        [Fact]
        public async Task ShareFormulaAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange - 分享功能失败测试
            var formulaId = Guid.NewGuid();
            var operatorId = Guid.NewGuid();
            var operatorName = "测试医生";

            _mockBusinessService
                .Setup(x => x.ShareFormulaAsync(formulaId, operatorId, operatorName))
                .ReturnsAsync(ServiceResult<bool>.Failure("分享验方失败"));

            // Act
            var result = await _formulaService.ShareFormulaAsync(formulaId, operatorId, operatorName);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("分享验方失败");
        }

        [Fact]
        public async Task GetFormulasAsync_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange - 获取验方失败测试
            var keyword = "桂枝";
            var category = "解表剂";

            _mockQueryService
                .Setup(x => x.GetFormulasAsync(keyword, category))
                .ReturnsAsync(ServiceResult<List<FormulaDto>>.Failure("获取验方列表失败"));

            // Act
            var result = await _formulaService.GetFormulasAsync(keyword, category);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("获取验方列表失败");
        }

        [Fact]
        public async Task GetCategoriesAsync_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange - 获取分类失败测试
            _mockQueryService
                .Setup(x => x.GetCategoriesAsync())
                .ReturnsAsync(ServiceResult<List<string>>.Failure("获取验方分类失败"));

            // Act
            var result = await _formulaService.GetCategoriesAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("获取验方分类失败");
        }

        #endregion
    }
}
