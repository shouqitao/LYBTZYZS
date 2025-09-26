using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Entities.Formula;
using LYBT.Infrastructure.Data;
using LYBT.Module.Formula.Interfaces;
using LYBT.Module.Formula.Services;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Common;
using LYBT.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

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
                _context,
                _repositoryMock.Object,
                _loggerMock.Object,
                Mapper);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            // 注册方剂服务相关的依赖
            services.AddSingleton(_formulaService);
            services.AddSingleton(_repositoryMock.Object);
        }

        #region 创建方剂测试

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var createDto = new FormulaCreateDto
            {
                Name = "小柴胡汤",
                Code = "XCHT001",
                PinyinAbbreviation = "XCHT",
                Category = "和解剂",
                Source = "《伤寒论》",
                Composition = "柴胡24g，黄芩9g，人参9g，半夏9g，甘草6g，生姜9g，大枣4枚",
                Preparation = "上七味，以水一斗二升，煮取六升，去滓，再煎取三升",
                Function = "和解少阳",
                Indication = "少阳证：往来寒热，胸胁苦满，默默不欲饮食，心烦喜呕",
                Usage = "温服一升，日三服",
                Contraindication = "阴虚火旺者慎用",
                Notes = "临床常用方剂",
                IsClassic = true,
                IsActive = true
            };

            var formula = new FormulaEntity
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Code = createDto.Code,
                Category = createDto.Category,
                Source = createDto.Source,
                IsClassic = createDto.IsClassic,
                IsActive = createDto.IsActive,
                CreatedAt = DateTime.Now
            };

            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<FormulaEntity>()))
                .ReturnsAsync(formula);

            // Act
            var result = await _formulaService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Name.Should().Be(createDto.Name);
            result.Data.Code.Should().Be(createDto.Code);
            result.Data.Category.Should().Be(createDto.Category);

            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<FormulaEntity>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateCode_ShouldReturnFailure()
        {
            // Arrange
            var createDto = new FormulaCreateDto
            {
                Name = "小柴胡汤",
                Code = "XCHT001",
                Category = "和解剂"
            };

            _repositoryMock.Setup(x => x.ExistsByCodeAsync(createDto.Code))
                .ReturnsAsync(true);

            // Act
            var result = await _formulaService.CreateAsync(createDto);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("方剂代码已存在");

            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<FormulaEntity>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WithHerbIngredients_ShouldSaveIngredients()
        {
            // Arrange
            var createDto = new FormulaCreateDto
            {
                Name = "麻黄汤",
                Code = "MHT001",
                Category = "解表剂",
                HerbIngredients = new List<FormulaHerbDto>
                {
                    new FormulaHerbDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "麻黄",
                        Dosage = 9,
                        Unit = "g",
                        ProcessingMethod = "去节",
                        Role = "君药"
                    },
                    new FormulaHerbDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "桂枝",
                        Dosage = 6,
                        Unit = "g",
                        Role = "臣药"
                    },
                    new FormulaHerbDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "杏仁",
                        Dosage = 9,
                        Unit = "g",
                        ProcessingMethod = "去皮尖",
                        Role = "佐药"
                    },
                    new FormulaHerbDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "甘草",
                        Dosage = 3,
                        Unit = "g",
                        ProcessingMethod = "炙",
                        Role = "使药"
                    }
                }
            };

            _repositoryMock.Setup(x => x.AddAsync(It.IsAny<FormulaEntity>()))
                .ReturnsAsync(new FormulaEntity { Id = Guid.NewGuid(), Name = createDto.Name });

            // Act
            var result = await _formulaService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            _repositoryMock.Verify(x => x.AddHerbIngredientsAsync(
                It.IsAny<Guid>(),
                It.Is<List<FormulaHerbEntity>>(herbs => herbs.Count == 4)
            ), Times.Once);
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
                Code = "XCHT001",
                Category = "和解剂",
                Source = "《伤寒论》",
                IsClassic = true,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(formulaId))
                .ReturnsAsync(formula);

            // Act
            var result = await _formulaService.GetByIdAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(formulaId);
            result.Data.Name.Should().Be(formula.Name);
        }

        [Fact]
        public async Task GetByCodeAsync_WithValidCode_ShouldReturnFormula()
        {
            // Arrange
            var code = "XCHT001";
            var formula = new FormulaEntity
            {
                Id = Guid.NewGuid(),
                Name = "小柴胡汤",
                Code = code,
                Category = "和解剂",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _repositoryMock.Setup(x => x.GetByCodeAsync(code))
                .ReturnsAsync(formula);

            // Act
            var result = await _formulaService.GetByCodeAsync(code);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Code.Should().Be(code);
        }

        [Fact]
        public async Task GetByCategoryAsync_ShouldReturnFormulasByCategory()
        {
            // Arrange
            var category = "解表剂";
            var formulas = new List<FormulaEntity>
            {
                new FormulaEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "麻黄汤",
                    Code = "MHT001",
                    Category = category,
                    IsActive = true
                },
                new FormulaEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "桂枝汤",
                    Code = "GZT001",
                    Category = category,
                    IsActive = true
                }
            };

            _repositoryMock.Setup(x => x.GetByCategoryAsync(category))
                .ReturnsAsync(formulas);

            // Act
            var result = await _formulaService.GetByCategoryAsync(category);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.All(f => f.Category == category).Should().BeTrue();
        }

        [Fact]
        public async Task GetClassicFormulasAsync_ShouldReturnOnlyClassicFormulas()
        {
            // Arrange
            var formulas = new List<FormulaEntity>
            {
                new FormulaEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "小柴胡汤",
                    Code = "XCHT001",
                    Category = "和解剂",
                    Source = "《伤寒论》",
                    IsClassic = true,
                    IsActive = true
                },
                new FormulaEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "四逆汤",
                    Code = "SNT001",
                    Category = "温里剂",
                    Source = "《伤寒论》",
                    IsClassic = true,
                    IsActive = true
                }
            };

            _repositoryMock.Setup(x => x.GetClassicFormulasAsync())
                .ReturnsAsync(formulas);

            // Act
            var result = await _formulaService.GetClassicFormulasAsync();

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.All(f => f.IsClassic).Should().BeTrue();
        }

        [Fact]
        public async Task SearchAsync_WithSearchTerm_ShouldReturnMatchingFormulas()
        {
            // Arrange
            var searchTerm = "柴胡";
            var formulas = new List<FormulaEntity>
            {
                new FormulaEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "小柴胡汤",
                    Code = "XCHT001",
                    Category = "和解剂",
                    IsActive = true
                },
                new FormulaEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "大柴胡汤",
                    Code = "DCHT001",
                    Category = "和解剂",
                    IsActive = true
                }
            };

            _repositoryMock.Setup(x => x.SearchAsync(searchTerm))
                .ReturnsAsync(formulas);

            // Act
            var result = await _formulaService.SearchAsync(searchTerm);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.All(f => f.Name.Contains(searchTerm)).Should().BeTrue();
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
                Function = "和解少阳，疏肝解郁",
                Indication = "少阳证，肝郁气滞证",
                Notes = "临床应用广泛"
            };

            var existingFormula = new FormulaEntity
            {
                Id = formulaId,
                Name = "小柴胡汤",
                Code = "XCHT001",
                Category = "和解剂",
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-30)
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(formulaId))
                .ReturnsAsync(existingFormula);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<FormulaEntity>()))
                .ReturnsAsync(existingFormula);

            // Act
            var result = await _formulaService.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(formulaId);

            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<FormulaEntity>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithHerbIngredients_ShouldUpdateIngredients()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var updateDto = new FormulaUpdateDto
            {
                Id = formulaId,
                HerbIngredients = new List<FormulaHerbDto>
                {
                    new FormulaHerbDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "柴胡",
                        Dosage = 12,
                        Unit = "g",
                        Role = "君药"
                    },
                    new FormulaHerbDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "黄芩",
                        Dosage = 9,
                        Unit = "g",
                        Role = "臣药"
                    }
                }
            };

            var existingFormula = new FormulaEntity
            {
                Id = formulaId,
                Name = "小柴胡汤",
                Code = "XCHT001",
                Category = "和解剂",
                IsActive = true
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(formulaId))
                .ReturnsAsync(existingFormula);

            _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<FormulaEntity>()))
                .ReturnsAsync(existingFormula);

            // Act
            var result = await _formulaService.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            // 验证先删除旧配方，再添加新配方
            _repositoryMock.Verify(x => x.RemoveHerbIngredientsAsync(formulaId), Times.Once);
            _repositoryMock.Verify(x => x.AddHerbIngredientsAsync(
                formulaId,
                It.Is<List<FormulaHerbEntity>>(herbs => herbs.Count == 2)
            ), Times.Once);
        }

        #endregion

        #region 删除方剂测试

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var formula = new FormulaEntity
            {
                Id = formulaId,
                Name = "测试方剂",
                Code = "TEST001",
                IsActive = true,
                IsDeleted = false
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(formulaId))
                .ReturnsAsync(formula);

            _repositoryMock.Setup(x => x.DeleteAsync(formulaId))
                .ReturnsAsync(true);

            // Act
            var result = await _formulaService.DeleteAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("成功");

            _repositoryMock.Verify(x => x.DeleteAsync(formulaId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithClassicFormula_ShouldReturnFailure()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var formula = new FormulaEntity
            {
                Id = formulaId,
                Name = "小柴胡汤",
                Code = "XCHT001",
                IsClassic = true,
                IsActive = true,
                IsDeleted = false
            };

            _repositoryMock.Setup(x => x.GetByIdAsync(formulaId))
                .ReturnsAsync(formula);

            // Act
            var result = await _formulaService.DeleteAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("经典方剂不能删除");

            _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region 方剂配伍管理测试

        [Fact]
        public async Task GetHerbIngredientsAsync_WithValidFormulaId_ShouldReturnIngredients()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var herbIngredients = new List<FormulaHerbEntity>
            {
                new FormulaHerbEntity
                {
                    Id = Guid.NewGuid(),
                    FormulaId = formulaId,
                    HerbId = Guid.NewGuid(),
                    HerbName = "柴胡",
                    Dosage = 24,
                    Unit = "g",
                    Role = "君药",
                    SortOrder = 1
                },
                new FormulaHerbEntity
                {
                    Id = Guid.NewGuid(),
                    FormulaId = formulaId,
                    HerbId = Guid.NewGuid(),
                    HerbName = "黄芩",
                    Dosage = 9,
                    Unit = "g",
                    Role = "臣药",
                    SortOrder = 2
                }
            };

            _repositoryMock.Setup(x => x.GetHerbIngredientsAsync(formulaId))
                .ReturnsAsync(herbIngredients);

            // Act
            var result = await _formulaService.GetHerbIngredientsAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data[0].HerbName.Should().Be("柴胡");
            result.Data[1].HerbName.Should().Be("黄芩");
        }

        [Fact]
        public async Task CalculateTotalDosageAsync_ShouldReturnCorrectTotal()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var herbIngredients = new List<FormulaHerbEntity>
            {
                new FormulaHerbEntity
                {
                    FormulaId = formulaId,
                    HerbName = "柴胡",
                    Dosage = 24,
                    Unit = "g"
                },
                new FormulaHerbEntity
                {
                    FormulaId = formulaId,
                    HerbName = "黄芩",
                    Dosage = 9,
                    Unit = "g"
                },
                new FormulaHerbEntity
                {
                    FormulaId = formulaId,
                    HerbName = "人参",
                    Dosage = 9,
                    Unit = "g"
                }
            };

            _repositoryMock.Setup(x => x.GetHerbIngredientsAsync(formulaId))
                .ReturnsAsync(herbIngredients);

            // Act
            var result = await _formulaService.CalculateTotalDosageAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.TotalDosage.Should().Be(42); // 24 + 9 + 9
            result.Data.Unit.Should().Be("g");
            result.Data.HerbCount.Should().Be(3);
        }

        #endregion

        #region 边界条件测试

        [Fact]
        public async Task CreateAsync_WithEmptyRequiredFields_ShouldReturnValidationError()
        {
            // Arrange
            var createDto = new FormulaCreateDto
            {
                Name = "", // 空的名称
                Code = "", // 空的代码
                Category = "" // 空的分类
            };

            // Act
            var result = await _formulaService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("验证失败");
        }

        [Fact]
        public async Task GetPagedAsync_WithLargePageSize_ShouldLimitResults()
        {
            // Arrange
            var pageRequest = new PagedRequest
            {
                PageNumber = 1,
                PageSize = 1000 // 极大的页面大小
            };

            _repositoryMock.Setup(x => x.GetPagedAsync(
                    1,
                    50, // 应该限制为最大值50
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .ReturnsAsync((new List<FormulaEntity>(), 0));

            // Act
            var result = await _formulaService.GetPagedAsync(pageRequest);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.PageSize.Should().BeLessThanOrEqualTo(50);
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidHerbDosage_ShouldReturnValidationError()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var updateDto = new FormulaUpdateDto
            {
                Id = formulaId,
                HerbIngredients = new List<FormulaHerbDto>
                {
                    new FormulaHerbDto
                    {
                        HerbId = Guid.NewGuid(),
                        HerbName = "柴胡",
                        Dosage = -5, // 无效的负数剂量
                        Unit = "g"
                    }
                }
            };

            // Act
            var result = await _formulaService.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("剂量必须大于0");
        }

        #endregion

        public override void Dispose()
        {
            _context?.Dispose();
            base.Dispose();
        }
    }
}