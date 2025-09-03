using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Formula;
using LYBT.Module.Formula.Interfaces;
using LYBT.Module.Formula.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Formula.Tests
{
    /// <summary>
    /// 验方服务单元测试
    /// </summary>
    public class FormulaServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<FormulaService>> _mockLogger;
        private readonly FormulaService _service;

        private readonly Guid _testOperatorId = Guid.NewGuid();
        private readonly string _testOperatorName = "测试医生";

        public FormulaServiceTests()
        {
            // 使用内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _dbContext = new AppDbContext(options);
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<FormulaService>>();
            _service = new FormulaService(_dbContext, _mockMapper.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        #region GetListAsync Tests

        [Fact]
        public async Task GetListAsync_WithEnabledFormulas_ReturnsOnlyEnabledFormulas()
        {
            // Arrange
            var formulas = new List<FormulaModel>
            {
                new FormulaModel { Id = Guid.NewGuid(), Name = "启用验方", Status = CommonStatus.Enabled },
                new FormulaModel { Id = Guid.NewGuid(), Name = "禁用验方", Status = CommonStatus.Disabled }
            };
            
            _dbContext.Formulas.AddRange(formulas);
            await _dbContext.SaveChangesAsync();

            var expectedDtos = new List<FormulaDto>
            {
                new FormulaDto { Id = formulas[0].Id, Name = "启用验方" }
            };

            _mockMapper.Setup(x => x.Map<List<FormulaDto>>(It.IsAny<List<FormulaModel>>()))
                      .Returns(expectedDtos);

            // Act
            var result = await _service.GetListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("启用验方");
        }

        [Fact]
        public async Task GetListAsync_WithEmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            _mockMapper.Setup(x => x.Map<List<FormulaDto>>(It.IsAny<List<FormulaModel>>()))
                      .Returns(new List<FormulaDto>());

            // Act
            var result = await _service.GetListAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_WithSearchKeyword_FiltersResults()
        {
            // Arrange
            var formulas = new List<FormulaModel>
            {
                new FormulaModel { Id = Guid.NewGuid(), Name = "感冒验方", Status = CommonStatus.Enabled },
                new FormulaModel { Id = Guid.NewGuid(), Name = "胃病验方", Status = CommonStatus.Enabled }
            };
            
            _dbContext.Formulas.AddRange(formulas);
            await _dbContext.SaveChangesAsync();

            var query = new FormulaQueryDto { CurrentPage = 1, PageSize = 10, SearchKeyword = "感冒" };
            var expectedDtos = new List<FormulaDto>
            {
                new FormulaDto { Id = formulas[0].Id, Name = "感冒验方" }
            };

            _mockMapper.Setup(x => x.Map<List<FormulaDto>>(It.IsAny<List<FormulaModel>>()))
                      .Returns(expectedDtos);

            // Act
            var result = await _service.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items.First().Name.Should().Contain("感冒");
        }

        [Fact]
        public async Task GetPagedAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var formulas = Enumerable.Range(1, 5)
                .Select(i => new FormulaModel 
                { 
                    Id = Guid.NewGuid(), 
                    Name = $"验方{i}", 
                    Status = CommonStatus.Enabled 
                })
                .ToList();
            
            _dbContext.Formulas.AddRange(formulas);
            await _dbContext.SaveChangesAsync();

            var query = new FormulaQueryDto { CurrentPage = 2, PageSize = 2 };
            var expectedDtos = new List<FormulaDto>
            {
                new FormulaDto { Id = formulas[2].Id, Name = "验方3" },
                new FormulaDto { Id = formulas[3].Id, Name = "验方4" }
            };

            _mockMapper.Setup(x => x.Map<List<FormulaDto>>(It.IsAny<List<FormulaModel>>()))
                      .Returns(expectedDtos);

            // Act
            var result = await _service.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(5);
            result.CurrentPage.Should().Be(2);
            result.PageSize.Should().Be(2);
            result.Items.Should().HaveCount(2);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsFormulaDetail()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var formula = new FormulaModel 
            { 
                Id = formulaId, 
                Name = "测试验方", 
                Status = CommonStatus.Enabled 
            };
            
            _dbContext.Formulas.Add(formula);
            await _dbContext.SaveChangesAsync();

            var expectedDto = new FormulaDetailDto { Id = formulaId, Name = "测试验方" };
            _mockMapper.Setup(x => x.Map<FormulaDetailDto>(formula)).Returns(expectedDto);

            // Act
            var result = await _service.GetByIdAsync(formulaId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(formulaId);
            result.Name.Should().Be("测试验方");
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _service.GetByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WithDisabledFormula_ReturnsNull()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var formula = new FormulaModel 
            { 
                Id = formulaId, 
                Name = "禁用验方", 
                Status = CommonStatus.Disabled 
            };
            
            _dbContext.Formulas.Add(formula);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdAsync(formulaId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidData_ReturnsCreatedFormula()
        {
            // Arrange
            var createDto = new FormulaCreateDto { Name = "新验方" };
            var expectedDto = new FormulaDetailDto 
            { 
                Id = Guid.NewGuid(), 
                Name = "新验方" 
            };

            _mockMapper.Setup(x => x.Map<FormulaDetailDto>(It.IsAny<FormulaModel>()))
                      .Returns(expectedDto);

            // Act
            var result = await _service.CreateAsync(createDto, _testOperatorId, _testOperatorName);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("新验方");
            
            // 验证数据库中是否保存了验方
            var savedFormula = await _dbContext.Formulas.FirstOrDefaultAsync(f => f.Name == "新验方");
            savedFormula.Should().NotBeNull();
            savedFormula!.Status.Should().Be(CommonStatus.Enabled);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidData_ReturnsUpdatedFormula()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var formula = new FormulaModel 
            { 
                Id = formulaId, 
                Name = "原名称", 
                Status = CommonStatus.Enabled 
            };
            
            _dbContext.Formulas.Add(formula);
            await _dbContext.SaveChangesAsync();

            var updateDto = new FormulaUpdateDto { Name = "更新名称" };
            var expectedDto = new FormulaDetailDto 
            { 
                Id = formulaId, 
                Name = "更新名称" 
            };

            _mockMapper.Setup(x => x.Map<FormulaDetailDto>(It.IsAny<FormulaModel>()))
                      .Returns(expectedDto);

            // Act
            var result = await _service.UpdateAsync(formulaId, updateDto, _testOperatorId, _testOperatorName);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("更新名称");
            
            // 验证数据库中的更新
            var updatedFormula = await _dbContext.Formulas.FindAsync(formulaId);
            updatedFormula!.Name.Should().Be("更新名称");
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updateDto = new FormulaUpdateDto { Name = "更新名称" };

            // Act
            var result = await _service.UpdateAsync(nonExistentId, updateDto, _testOperatorId, _testOperatorName);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithValidId_SoftDeletesFormula()
        {
            // Arrange
            var formulaId = Guid.NewGuid();
            var formula = new FormulaModel 
            { 
                Id = formulaId, 
                Name = "待删除验方", 
                Status = CommonStatus.Enabled 
            };
            
            _dbContext.Formulas.Add(formula);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(formulaId, _testOperatorId, _testOperatorName);

            // Assert
            result.Should().BeTrue();
            
            // 验证软删除
            var deletedFormula = await _dbContext.Formulas.FindAsync(formulaId);
            deletedFormula!.Status.Should().Be(CommonStatus.Disabled);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentId_ReturnsFalse()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _service.DeleteAsync(nonExistentId, _testOperatorId, _testOperatorName);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region SearchFormulasAsync Tests

        [Fact]
        public async Task SearchFormulasAsync_WithKeyword_ReturnsMatchingFormulas()
        {
            // Arrange
            var formulas = new List<FormulaModel>
            {
                new FormulaModel { Id = Guid.NewGuid(), Name = "银翘散", Status = CommonStatus.Enabled },
                new FormulaModel { Id = Guid.NewGuid(), Name = "麻黄汤", Status = CommonStatus.Enabled },
                new FormulaModel { Id = Guid.NewGuid(), Name = "银杏叶", Status = CommonStatus.Enabled }
            };
            
            _dbContext.Formulas.AddRange(formulas);
            await _dbContext.SaveChangesAsync();

            var expectedDtos = new List<FormulaDto>
            {
                new FormulaDto { Id = formulas[0].Id, Name = "银翘散" },
                new FormulaDto { Id = formulas[2].Id, Name = "银杏叶" }
            };

            _mockMapper.Setup(x => x.Map<List<FormulaDto>>(It.IsAny<List<FormulaModel>>()))
                      .Returns(expectedDtos);

            // Act
            var result = await _service.SearchFormulasAsync("银", 10);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(f => f.Name.Contains("银")).Should().BeTrue();
        }

        [Fact]
        public async Task SearchFormulasAsync_WithMaxResults_LimitsResults()
        {
            // Arrange
            var formulas = Enumerable.Range(1, 10)
                .Select(i => new FormulaModel 
                { 
                    Id = Guid.NewGuid(), 
                    Name = $"验方{i}", 
                    Status = CommonStatus.Enabled 
                })
                .ToList();
            
            _dbContext.Formulas.AddRange(formulas);
            await _dbContext.SaveChangesAsync();

            var expectedDtos = formulas.Take(5)
                .Select(f => new FormulaDto { Id = f.Id, Name = f.Name })
                .ToList();

            _mockMapper.Setup(x => x.Map<List<FormulaDto>>(It.IsAny<List<FormulaModel>>()))
                      .Returns(expectedDtos);

            // Act
            var result = await _service.SearchFormulasAsync("验方", 5);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(5);
        }

        #endregion

        #region CopyFormulaAsync Tests

        [Fact]
        public async Task CopyFormulaAsync_WithValidId_CreatesNewFormula()
        {
            // Arrange
            var originalId = Guid.NewGuid();
            var original = new FormulaModel 
            { 
                Id = originalId, 
                Name = "原验方", 
                Status = CommonStatus.Enabled 
            };
            
            _dbContext.Formulas.Add(original);
            await _dbContext.SaveChangesAsync();

            var newName = "复制的验方";
            var expectedDto = new FormulaDetailDto 
            { 
                Id = Guid.NewGuid(), 
                Name = newName 
            };

            _mockMapper.Setup(x => x.Map<FormulaDetailDto>(It.IsAny<FormulaModel>()))
                      .Returns(expectedDto);

            // Act
            var result = await _service.CopyFormulaAsync(originalId, newName, _testOperatorId, _testOperatorName);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be(newName);
            
            // 验证数据库中创建了新验方
            var copiedFormula = await _dbContext.Formulas
                .FirstOrDefaultAsync(f => f.Name == newName);
            copiedFormula.Should().NotBeNull();
        }

        [Fact]
        public async Task CopyFormulaAsync_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _service.CopyFormulaAsync(nonExistentId, "新名称", _testOperatorId, _testOperatorName);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CreateFromPrescriptionAsync Tests

        [Fact]
        public async Task CreateFromPrescriptionAsync_WithValidData_CreatesFormula()
        {
            // Arrange
            var dto = new CreateFormulaFromPrescriptionDto 
            { 
                Name = "从处方创建的验方",
                PrescriptionId = Guid.NewGuid()
            };
            var expectedDto = new FormulaDetailDto 
            { 
                Id = Guid.NewGuid(), 
                Name = dto.Name 
            };

            _mockMapper.Setup(x => x.Map<FormulaDetailDto>(It.IsAny<FormulaModel>()))
                      .Returns(expectedDto);

            // Act
            var result = await _service.CreateFromPrescriptionAsync(dto, _testOperatorId, _testOperatorName);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be(dto.Name);
            
            // 验证数据库中创建了验方
            var createdFormula = await _dbContext.Formulas
                .FirstOrDefaultAsync(f => f.Name == dto.Name);
            createdFormula.Should().NotBeNull();
        }

        #endregion

        #region GetStatisticsAsync Tests

        [Fact]
        public async Task GetStatisticsAsync_WithDateRange_ReturnsCorrectStatistics()
        {
            // Arrange
            var baseDate = DateTime.Now.Date;
            var formulas = new List<FormulaModel>
            {
                new FormulaModel { Id = Guid.NewGuid(), Name = "验方1", Status = CommonStatus.Enabled, CreateTime = baseDate.AddDays(-5) },
                new FormulaModel { Id = Guid.NewGuid(), Name = "验方2", Status = CommonStatus.Enabled, CreateTime = baseDate.AddDays(-3) },
                new FormulaModel { Id = Guid.NewGuid(), Name = "验方3", Status = CommonStatus.Enabled, CreateTime = baseDate.AddDays(-10) }
            };
            
            _dbContext.Formulas.AddRange(formulas);
            await _dbContext.SaveChangesAsync();

            var startDate = baseDate.AddDays(-7);
            var endDate = baseDate;

            // Act
            var result = await _service.GetStatisticsAsync(startDate, endDate);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(2); // 只有2个在日期范围内
            result.PrivateCount.Should().Be(2);
            result.SharedCount.Should().Be(0);
        }

        #endregion

        #region Advanced Feature Tests

        [Fact]
        public async Task ShareFormulaAsync_WithValidId_ReturnsTrue()
        {
            // Act
            var result = await _service.ShareFormulaAsync(Guid.NewGuid(), _testOperatorId, _testOperatorName);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task UnshareFormulaAsync_WithValidId_ReturnsTrue()
        {
            // Act
            var result = await _service.UnshareFormulaAsync(Guid.NewGuid(), _testOperatorId, _testOperatorName);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetRecommendationsAsync_WithSymptoms_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetRecommendationsAsync("发热", "感冒");

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetFrequentlyUsedFormulasAsync_WithDoctorId_ReturnsFormulas()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var formulas = new List<FormulaModel>
            {
                new FormulaModel { Id = Guid.NewGuid(), Name = "常用验方1", Status = CommonStatus.Enabled },
                new FormulaModel { Id = Guid.NewGuid(), Name = "常用验方2", Status = CommonStatus.Enabled }
            };
            
            _dbContext.Formulas.AddRange(formulas);
            await _dbContext.SaveChangesAsync();

            var expectedDtos = formulas.Select(f => new FormulaDto { Id = f.Id, Name = f.Name }).ToList();
            _mockMapper.Setup(x => x.Map<List<FormulaDto>>(It.IsAny<List<FormulaModel>>()))
                      .Returns(expectedDtos);

            // Act
            var result = await _service.GetFrequentlyUsedFormulasAsync(doctorId, 10);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task ValidateFormulaAsync_WithValidId_ReturnsValidationResult()
        {
            // Act
            var result = await _service.ValidateFormulaAsync(Guid.NewGuid());

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
            result.Warnings.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUsageRecordsAsync_WithValidId_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetUsageRecordsAsync(Guid.NewGuid());

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion
    }
}