using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Services;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FormulaEntity = LYBT.Entities.Formulas.Formula;

namespace LYBT.Module.Formulas.Tests.Services
{
    /// <summary>
    /// 方剂服务单元测试
    /// 测试方剂的创建、查询、更新、删除以及药材配比管理等核心业务逻辑
    /// OpenSpec: decouple-server-modules - 使用ICrossModuleQueryService替代IHerbRepository
    /// </summary>
    public class FormulaServiceTests : TestBase
    {
        private readonly FormulaService _formulaService;
        private readonly Mock<IFormulaRepository> _repositoryMock;
        private readonly Mock<ICrossModuleQueryService> _crossModuleQueryMock;
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
            _crossModuleQueryMock = CreateMock<ICrossModuleQueryService>();
            _loggerMock = CreateLoggerMock<FormulaService>();

            _formulaService = new FormulaService(
                _repositoryMock.Object,
                _crossModuleQueryMock.Object,
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

            var createDto = new FormulaInputDto
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
                Herbs = new List<FormulaHerbItemInputDto>
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
            var createDto = new FormulaInputDto
            {
                Name = "测试方剂",
                Effect = "测试功效",
                Usage = "测试用法",
                Herbs = new List<FormulaHerbItemInputDto>
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
            var updateDto = new FormulaInputDto
            {
                Id = formulaId,
                Name = "小柴胡汤（加减）",
                Effect = "和解少阳，疏肝解郁",
                Usage = "温服，日三次",
                Herbs = new List<FormulaHerbItemInputDto>
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

            _repositoryMock.Setup(x => x.GetByIdWithHerbsAsync(formulaId))
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
            var updateDto = new FormulaInputDto
            {
                Id = formulaId,
                Name = "测试方剂",
                Effect = "测试功效",
                Usage = "测试用法",
                Herbs = new List<FormulaHerbItemInputDto>()
            };

            _repositoryMock.Setup(x => x.GetByIdWithHerbsAsync(formulaId))
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

        #region 待验证验方查询测试

        [Fact]
        public async Task GetPendingValidationFormulasAsync_ShouldReturnOnlyDraftFormulas()
        {
            // Arrange
            var formulas = new List<FormulaEntity>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "待验证验方1",
                    Effect = "功效1",
                    ValidationStatus = LYBT.Shared.Models.Enums.FormulaValidationStatus.Draft,
                    Herbs = new List<LYBT.Entities.Formulas.FormulaHerbItem>()
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "已验证验方",
                    Effect = "功效2",
                    ValidationStatus = LYBT.Shared.Models.Enums.FormulaValidationStatus.Validated,
                    Herbs = new List<LYBT.Entities.Formulas.FormulaHerbItem>()
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "待验证验方2",
                    Effect = "功效3",
                    ValidationStatus = LYBT.Shared.Models.Enums.FormulaValidationStatus.Draft,
                    Herbs = new List<LYBT.Entities.Formulas.FormulaHerbItem>()
                }
            };

            _repositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(formulas);

            // Act
            var result = await _formulaService.GetPendingValidationFormulasAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.Should().OnlyContain(f => f.ValidationStatus == LYBT.Shared.Models.Enums.FormulaValidationStatus.Draft);
            result.Data.Should().NotContain(f => f.Name == "已验证验方");

            _repositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetPendingValidationFormulasAsync_WithNoFormulas_ShouldReturnEmptyList()
        {
            // Arrange
            _repositoryMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<FormulaEntity>());

            // Act
            var result = await _formulaService.GetPendingValidationFormulasAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPendingValidationFormulasAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            _repositoryMock.Setup(x => x.GetAllAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _formulaService.GetPendingValidationFormulasAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("获取待验证验方列表失败");
        }

        #endregion

        #region 验证验方药材测试

        [Fact]
        public async Task ValidateFormulaHerbAsync_WithValidData_ShouldUpdateHerbAndReturnSuccess()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var herbItemId = Guid.NewGuid();
            var selectedHerbId = Guid.NewGuid();

            var formula = new FormulaEntity
            {
                Id = formulaId,
                Name = "测试验方",
                ValidationStatus = LYBT.Shared.Models.Enums.FormulaValidationStatus.Draft,
                Herbs = new List<LYBT.Entities.Formulas.FormulaHerbItem>
                {
                    new()
                    {
                        Id = herbItemId,
                        HerbName = "未验证药材",
                        IsValidated = false,
                        Quantity = 10
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "其他药材",
                        IsValidated = false,
                        Quantity = 5
                    }
                }
            };

            // OpenSpec: decouple-server-modules - 使用HerbBasicDto替代Herb实体
            var selectedHerbDto = new HerbBasicDto
            {
                Id = selectedHerbId,
                Name = "人参"
            };

            _repositoryMock.Setup(x => x.GetByIdWithHerbsAsync(formulaId))
                .ReturnsAsync(formula);

            var crossModuleQueryMock = CreateMock<ICrossModuleQueryService>();
            crossModuleQueryMock.Setup(x => x.GetHerbBasicInfoAsync(selectedHerbId))
                .ReturnsAsync(selectedHerbDto);

            var formulaService = new FormulaService(
                _repositoryMock.Object,
                crossModuleQueryMock.Object,
                Mapper,
                _loggerMock.Object);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<FormulaEntity>()))
                .ReturnsAsync(formula);

            _repositoryMock.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await formulaService.ValidateFormulaHerbAsync(formulaId, herbItemId, selectedHerbId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            var herbItem = formula.Herbs.First(h => h.Id == herbItemId);
            herbItem.HerbId.Should().Be(selectedHerbId);
            herbItem.HerbName.Should().Be("人参");
            herbItem.IsValidated.Should().BeTrue();

            // 验方状态应该还是Draft（因为还有其他未验证药材）
            formula.ValidationStatus.Should().Be(LYBT.Shared.Models.Enums.FormulaValidationStatus.Draft);

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<FormulaEntity>()), Times.Once);
            _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ValidateFormulaHerbAsync_WhenAllHerbsValidated_ShouldUpdateFormulaStatusToValidated()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var herbItemId = Guid.NewGuid();
            var selectedHerbId = Guid.NewGuid();

            var formula = new FormulaEntity
            {
                Id = formulaId,
                Name = "测试验方",
                ValidationStatus = LYBT.Shared.Models.Enums.FormulaValidationStatus.Draft,
                Herbs = new List<LYBT.Entities.Formulas.FormulaHerbItem>
                {
                    new()
                    {
                        Id = herbItemId,
                        HerbName = "最后一个未验证药材",
                        IsValidated = false,
                        Quantity = 10
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "已验证药材1",
                        IsValidated = true,
                        Quantity = 5
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "已验证药材2",
                        IsValidated = true,
                        Quantity = 8
                    }
                }
            };

            // OpenSpec: decouple-server-modules - 使用HerbBasicDto替代Herb实体
            var selectedHerbDto = new HerbBasicDto
            {
                Id = selectedHerbId,
                Name = "当归"
            };

            _repositoryMock.Setup(x => x.GetByIdWithHerbsAsync(formulaId))
                .ReturnsAsync(formula);

            var crossModuleQueryMock = CreateMock<ICrossModuleQueryService>();
            crossModuleQueryMock.Setup(x => x.GetHerbBasicInfoAsync(selectedHerbId))
                .ReturnsAsync(selectedHerbDto);

            var formulaService = new FormulaService(
                _repositoryMock.Object,
                crossModuleQueryMock.Object,
                Mapper,
                _loggerMock.Object);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<FormulaEntity>()))
                .ReturnsAsync(formula);

            _repositoryMock.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await formulaService.ValidateFormulaHerbAsync(formulaId, herbItemId, selectedHerbId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            // 所有药材都已验证，验方状态应该更新为Validated
            formula.ValidationStatus.Should().Be(LYBT.Shared.Models.Enums.FormulaValidationStatus.Validated);
            formula.Herbs.Should().OnlyContain(h => h.IsValidated);
        }

        [Fact]
        public async Task ValidateFormulaHerbAsync_WithNonExistentFormula_ShouldReturnFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var herbItemId = Guid.NewGuid();
            var selectedHerbId = Guid.NewGuid();

            _repositoryMock.Setup(x => x.GetByIdWithHerbsAsync(formulaId))
                .ReturnsAsync((FormulaEntity?)null);

            var crossModuleQueryMock = CreateMock<ICrossModuleQueryService>();
            var formulaService = new FormulaService(
                _repositoryMock.Object,
                crossModuleQueryMock.Object,
                Mapper,
                _loggerMock.Object);

            // Act
            var result = await formulaService.ValidateFormulaHerbAsync(formulaId, herbItemId, selectedHerbId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("验方不存在");

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<FormulaEntity>()), Times.Never);
        }

        [Fact]
        public async Task ValidateFormulaHerbAsync_WithNonExistentHerbItem_ShouldReturnFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var herbItemId = Guid.NewGuid();
            var selectedHerbId = Guid.NewGuid();

            var formula = new FormulaEntity
            {
                Id = formulaId,
                Name = "测试验方",
                Herbs = new List<LYBT.Entities.Formulas.FormulaHerbItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(), // 不同的ID
                        HerbName = "其他药材",
                        IsValidated = false
                    }
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithHerbsAsync(formulaId))
                .ReturnsAsync(formula);

            var crossModuleQueryMock = CreateMock<ICrossModuleQueryService>();
            var formulaService = new FormulaService(
                _repositoryMock.Object,
                crossModuleQueryMock.Object,
                Mapper,
                _loggerMock.Object);

            // Act
            var result = await formulaService.ValidateFormulaHerbAsync(formulaId, herbItemId, selectedHerbId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("药材项不存在");
        }

        [Fact]
        public async Task ValidateFormulaHerbAsync_WithNonExistentSelectedHerb_ShouldReturnFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var herbItemId = Guid.NewGuid();
            var selectedHerbId = Guid.NewGuid();

            var formula = new FormulaEntity
            {
                Id = formulaId,
                Name = "测试验方",
                Herbs = new List<LYBT.Entities.Formulas.FormulaHerbItem>
                {
                    new()
                    {
                        Id = herbItemId,
                        HerbName = "药材",
                        IsValidated = false
                    }
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithHerbsAsync(formulaId))
                .ReturnsAsync(formula);

            var crossModuleQueryMock = CreateMock<ICrossModuleQueryService>();
            crossModuleQueryMock.Setup(x => x.GetHerbBasicInfoAsync(selectedHerbId))
                .ReturnsAsync((HerbBasicDto?)null);

            var formulaService = new FormulaService(
                _repositoryMock.Object,
                crossModuleQueryMock.Object,
                Mapper,
                _loggerMock.Object);

            // Act
            var result = await formulaService.ValidateFormulaHerbAsync(formulaId, herbItemId, selectedHerbId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("所选药材不存在");
        }

        [Fact]
        public async Task ValidateFormulaHerbAsync_WithAlreadyValidatedHerb_ShouldReturnFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var herbItemId = Guid.NewGuid();
            var selectedHerbId = Guid.NewGuid();

            var formula = new FormulaEntity
            {
                Id = formulaId,
                Name = "测试验方",
                Herbs = new List<LYBT.Entities.Formulas.FormulaHerbItem>
                {
                    new()
                    {
                        Id = herbItemId,
                        HerbId = Guid.NewGuid(),
                        HerbName = "人参",
                        IsValidated = true, // 已经验证过
                        Quantity = 10
                    }
                }
            };

            _repositoryMock.Setup(x => x.GetByIdWithHerbsAsync(formulaId))
                .ReturnsAsync(formula);

            var crossModuleQueryMock = CreateMock<ICrossModuleQueryService>();
            var formulaService = new FormulaService(
                _repositoryMock.Object,
                crossModuleQueryMock.Object,
                Mapper,
                _loggerMock.Object);

            // Act
            var result = await formulaService.ValidateFormulaHerbAsync(formulaId, herbItemId, selectedHerbId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("该药材已校验，无需重复操作");

            // 验证未调用更新操作（因为已校验，应该直接返回）
            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<FormulaEntity>()), Times.Never);
            _repositoryMock.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        #endregion

        public override void Dispose()
        {
            _context?.Dispose();
            base.Dispose();
        }
    }
}
