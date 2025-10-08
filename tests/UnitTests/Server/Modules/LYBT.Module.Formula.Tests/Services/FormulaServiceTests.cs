using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Infrastructure.Data;
using LYBT.Module.Formula.Interfaces;
using LYBT.Module.Formula.Services;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FormulaEntity = LYBT.Entities.Formula.Formula;

namespace LYBT.Module.Formula.Tests.Services
{
    /// <summary>
    /// 方剂服务单元测试
    /// 测试方剂的创建、查询、更新、删除以及药材配比管理等核心业务逻辑
    /// </summary>
    public class FormulaServiceTests : TestBase
    {
        private readonly FormulaService _formulaService;
        private readonly Mock<IFormulaRepository> _repositoryMock;
        private readonly Mock<ILogger<FormulaService>> _loggerMock;
        private readonly AppDbContext _context;

        public FormulaServiceTests()
        {
            // 创建内存数据库上下文
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _repositoryMock = CreateMock<IFormulaRepository>();
            _loggerMock = CreateLoggerMock<FormulaService>();

            _formulaService = new FormulaService(
                _repositoryMock.Object,
                Mapper,
                _loggerMock.Object);
        }

        #region 创建方剂测试

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var herbId1 = Guid.NewGuid();
            var herbId2 = Guid.NewGuid();

            var createDto = new FormulaCreateDto
            {
                Name = "桂枝汤",
                Effect = "解肌发表，调和营卫",
                Usage = "水煎服，温服",
                Category = "解表剂",
                Description = "伤寒论经典名方",
                Preparation = "水煎",
                Indications = "外感风寒表虚证",
                Contraindications = "表实无汗者禁用",
                IsShared = false,
                Herbs = new List<FormulaHerbItemCreateDto>
                {
                    new() { HerbId = herbId1, Quantity = 9, SortOrder = 1 },
                    new() { HerbId = herbId2, Quantity = 9, SortOrder = 2 }
                }
            };

            var formula = new FormulaEntity
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Effect = createDto.Effect,
                Usage = createDto.Usage,
                Category = createDto.Category
            };

            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<FormulaEntity>()))
                .ReturnsAsync(formula);

            // Act
            var result = await _formulaService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Name.Should().Be(createDto.Name);
            result.Data.Effect.Should().Be(createDto.Effect);

            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<FormulaEntity>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var createDto = new FormulaCreateDto
            {
                Name = "测试方剂",
                Effect = "测试功效",
                Usage = "测试用法",
                Herbs = new List<FormulaHerbItemCreateDto>
                {
                    new() { HerbId = Guid.NewGuid(), Quantity = 10, SortOrder = 1 }
                }
            };

            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<FormulaEntity>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _formulaService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("创建验方失败");
        }

        #endregion

        #region 查询方剂测试

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnFormula()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var formula = new FormulaEntity
            {
                Id = formulaId,
                Name = "小柴胡汤",
                Effect = "和解少阳",
                Usage = "温服一升，日三服",
                Category = "和解剂"
            };

            _repositoryMock.Setup(x => x.GetByIdWithHerbsAsync(formulaId))
                .ReturnsAsync(formula);

            // Act
            var result = await _formulaService.GetByIdAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(formulaId);
            result.Data.Name.Should().Be(formula.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.GetByIdWithHerbsAsync(formulaId))
                .ReturnsAsync((FormulaEntity?)null);

            // Act
            var result = await _formulaService.GetByIdAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("验方不存在");
        }

        [Fact]
        public async Task SearchAsync_WithSearchTerm_ShouldReturnMatchingFormulas()
        {
            // Arrange
            var searchTerm = "柴胡";
            var formulas = new List<FormulaEntity>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "小柴胡汤",
                    Effect = "和解少阳",
                    Category = "和解剂"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "大柴胡汤",
                    Effect = "和解少阳，内泻热结",
                    Category = "和解剂"
                }
            };

            var pagedResult = new PagedResult<FormulaEntity>
            {
                Items = formulas,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 100
            };

            _repositoryMock.Setup(x => x.GetPagedWithDetailsAsync(1, 100, searchTerm))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _formulaService.SearchAsync(searchTerm);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task SearchAsync_WithEmptyKeyword_ShouldReturnEmptyList()
        {
            // Arrange
            var emptyKeyword = "";

            // Act
            var result = await _formulaService.SearchAsync(emptyKeyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();

            _repositoryMock.Verify(x => x.GetPagedWithDetailsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SearchAsync_WithWhitespaceKeyword_ShouldReturnEmptyList()
        {
            // Arrange
            var whitespaceKeyword = "   ";

            // Act
            var result = await _formulaService.SearchAsync(whitespaceKeyword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPagedAsync_WithValidParameters_ShouldReturnPagedResult()
        {
            // Arrange
            var formulas = new List<FormulaEntity>
            {
                new() { Id = Guid.NewGuid(), Name = "方剂1", Effect = "功效1" },
                new() { Id = Guid.NewGuid(), Name = "方剂2", Effect = "功效2" }
            };

            var pagedResult = new PagedResult<FormulaEntity>
            {
                Items = formulas,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 20
            };

            _repositoryMock.Setup(x => x.GetPagedWithDetailsAsync(1, 20, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _formulaService.GetPagedAsync(1, 20, null);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedAsync_WithZeroPage_ShouldStillWork()
        {
            // Arrange
            var pagedResult = new PagedResult<FormulaEntity>
            {
                Items = new List<FormulaEntity>(),
                TotalCount = 0,
                CurrentPage = 0,
                PageSize = 20
            };

            _repositoryMock.Setup(x => x.GetPagedWithDetailsAsync(0, 20, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _formulaService.GetPagedAsync(0, 20, null);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region 更新方剂测试

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var updateDto = new FormulaUpdateDto
            {
                Id = formulaId,
                Name = "小柴胡汤（加减）",
                Effect = "和解少阳，疏肝解郁",
                Usage = "温服，日三次",
                Herbs = new List<FormulaHerbItemUpdateDto>
                {
                    new() { HerbId = Guid.NewGuid(), Quantity = 9, SortOrder = 1 }
                }
            };

            var existingFormula = new FormulaEntity
            {
                Id = formulaId,
                Name = "小柴胡汤",
                Effect = "和解少阳",
                Category = "和解剂"
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(formulaId))
                .ReturnsAsync(existingFormula);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<FormulaEntity>()))
                .ReturnsAsync(existingFormula);

            // Act
            var result = await _formulaService.UpdateAsync(formulaId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(formulaId);

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<FormulaEntity>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var updateDto = new FormulaUpdateDto
            {
                Id = formulaId,
                Name = "测试方剂",
                Effect = "测试功效",
                Usage = "测试用法",
                Herbs = new List<FormulaHerbItemUpdateDto>()
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(formulaId))
                .ReturnsAsync((FormulaEntity?)null);

            // Act
            var result = await _formulaService.UpdateAsync(formulaId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("验方不存在");

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<FormulaEntity>()), Times.Never);
        }

        #endregion

        #region 删除方剂测试

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            var formulaId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.DeleteAsync(formulaId))
                .ReturnsAsync(true);

            // Act
            var result = await _formulaService.DeleteAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            _repositoryMock.Verify(x => x.DeleteAsync(formulaId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithRepositoryFailure_ShouldReturnFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.DeleteAsync(formulaId))
                .ReturnsAsync(false);

            // Act
            var result = await _formulaService.DeleteAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("删除失败");
        }

        #endregion

        #region 克隆方剂测试

        [Fact]
        public async Task CloneFormulaAsync_WithValidId_ShouldReturnClonedFormula()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var originalFormula = new FormulaEntity
            {
                Id = formulaId,
                Name = "桂枝汤",
                Effect = "解肌发表，调和营卫",
                Usage = "温服",
                Category = "解表剂",
                FormulaType = LYBT.Entities.Formula.FormulaType.Classic,
                IsShared = true
            };

            _repositoryMock.Setup(x => x.GetByIdWithHerbsAsync(formulaId))
                .ReturnsAsync(originalFormula);

            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<FormulaEntity>()))
                .ReturnsAsync((FormulaEntity f) => f);

            _repositoryMock.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _formulaService.CloneFormulaAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Name.Should().Contain("副本");
            result.Data.Name.Should().Contain(originalFormula.Name);

            _repositoryMock.Verify(x => x.GetByIdWithHerbsAsync(formulaId), Times.Once);
            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<FormulaEntity>()), Times.Once);
            _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CloneFormulaAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.GetByIdWithHerbsAsync(formulaId))
                .ReturnsAsync((FormulaEntity?)null);

            // Act
            var result = await _formulaService.CloneFormulaAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("未找到要克隆的处方");

            _repositoryMock.Verify(x => x.GetByIdWithHerbsAsync(formulaId), Times.Once);
            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<FormulaEntity>()), Times.Never);
        }

        #endregion

        public override void Dispose()
        {
            _context?.Dispose();
            base.Dispose();
        }
    }
}
