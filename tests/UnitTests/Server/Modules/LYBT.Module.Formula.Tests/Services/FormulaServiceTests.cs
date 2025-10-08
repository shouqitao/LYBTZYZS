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
                _repositoryMock.Object,
                Mapper,
                _loggerMock.Object);
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

        #endregion

        public override void Dispose()
        {
            _context?.Dispose();
            base.Dispose();
        }
    }
}