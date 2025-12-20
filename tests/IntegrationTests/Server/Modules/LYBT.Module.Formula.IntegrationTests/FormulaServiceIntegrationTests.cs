using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Formulas;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
using LYBT.Module.Formulas;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Mapping;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Formulas.IntegrationTests
{
    /// <summary>
    /// 验方服务集成测试 - 测试完整的工作流
    /// Issue #1357: 验证导入→验证→使用的端到端流程
    /// 使用真实SQL Server数据库(LYBTDB)和真实药材库
    /// </summary>
    public class FormulaServiceIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IServiceScope _scope;
        private readonly AppDbContext _context;
        private readonly IFormulaService _formulaService;
        private readonly IFormulaRepository _formulaRepository;

        // 存储测试创建的验方ID，用于清理
        private readonly List<Guid> _testFormulaIds = new();

        public FormulaServiceIntegrationTests()
        {
            // 使用真实SQL Server数据库
            var connectionString = "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;Application Name=LYBT.Formula.IntegrationTests";

            var services = new ServiceCollection();

            // 配置DbContext
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                });
                options.EnableSensitiveDataLogging();
            });

            // 配置AutoMapper
            services.AddAutoMapper(typeof(FormulaMappingProfile));

            // 注册Formula模块服务
            services.AddFormulaModule();

            // 注册跨模块查询服务（FormulaService依赖）
            services.AddScoped<ICrossModuleQueryService, CrossModuleQueryService>();

            // 注册Logger
            services.AddLogging(builder => builder.AddDebug());

            _serviceProvider = services.BuildServiceProvider();
            _scope = _serviceProvider.CreateScope();

            _context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _formulaService = _scope.ServiceProvider.GetRequiredService<IFormulaService>();
            _formulaRepository = _scope.ServiceProvider.GetRequiredService<IFormulaRepository>();
        }

        #region 辅助方法

        /// <summary>
        /// 从真实药材库获取药材名称列表
        /// </summary>
        private async Task<List<string>> GetRealHerbNamesAsync(int count = 3)
        {
            var herbs = await _context.Herbs
                .Where(h => h.Status == CommonStatus.Enabled)
                .Take(count)
                .Select(h => h.Name)
                .ToListAsync();

            return herbs;
        }

        /// <summary>
        /// 创建测试用的导入数据
        /// </summary>
        private async Task<List<FormulaImportItemDto>> CreateTestImportDataAsync(bool useRealHerbs = true)
        {
            var herbNames = useRealHerbs
                ? await GetRealHerbNamesAsync(3)
                : new List<string> { "不存在的药材1", "不存在的药材2" };

            var formulaName = $"集成测试验方_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}";

            return new List<FormulaImportItemDto>
            {
                new FormulaImportItemDto
                {
                    Name = formulaName,
                    Effect = "补气养血，健脾益气",
                    Usage = "水煎服，每日2次",
                    Property = "温",
                    IsShared = false,
                    Remark = "集成测试验方",
                    Herbs = herbNames.Select((name, index) => new FormulaHerbImportItemDto
                    {
                        HerbName = name,
                        Dosage = 10 + index * 5,
                        Unit = "g",
                        SortOrder = index
                    }).ToList()
                }
            };
        }

        #endregion

        #region 集成测试 1: 导入验方成功（药材全部匹配）

        [Fact]
        public async Task Integration_ImportFromData_WithRealHerbs_ShouldImportWithValidatedHerbs()
        {
            // Arrange - 使用真实药材库的药材
            var importData = await CreateTestImportDataAsync(useRealHerbs: true);

            // 确保有可用的药材
            if (importData[0].Herbs.Count == 0)
            {
                // 跳过测试如果药材库为空
                return;
            }

            // Act - 导入验方
            var importResult = await _formulaService.ImportFromDataAsync(importData);

            // Assert
            importResult.Should().NotBeNull();
            importResult.IsSuccess.Should().BeTrue();
            importResult.Data.Should().NotBeNull();
            importResult.Data!.SuccessCount.Should().Be(1);
            importResult.Data.SuccessfulFormulas.Should().HaveCount(1);

            var importedFormula = importResult.Data.SuccessfulFormulas[0];
            importedFormula.Name.Should().Be(importData[0].Name);

            // 记录测试创建的验方ID用于清理
            _testFormulaIds.Add(importedFormula.Id);

            // 验证药材是否自动匹配成功
            importResult.Data.MatchedHerbsCount.Should().BeGreaterThan(0);
        }

        #endregion

        #region 集成测试 2: 导入验方（药材不匹配）

        [Fact]
        public async Task Integration_ImportFromData_WithUnknownHerbs_ShouldImportWithUnvalidatedHerbs()
        {
            // Arrange - 使用不存在的药材名称
            var importData = await CreateTestImportDataAsync(useRealHerbs: false);

            // Act - 导入验方
            var importResult = await _formulaService.ImportFromDataAsync(importData);

            // Assert
            importResult.Should().NotBeNull();
            importResult.IsSuccess.Should().BeTrue();
            importResult.Data!.SuccessCount.Should().Be(1);

            // 记录测试创建的验方ID用于清理
            if (importResult.Data.SuccessfulFormulas.Any())
            {
                _testFormulaIds.Add(importResult.Data.SuccessfulFormulas[0].Id);
            }

            // 验证药材未自动匹配
            importResult.Data.UnmatchedHerbsCount.Should().Be(2);
        }

        #endregion

        #region 集成测试 3: 验证状态流转（Draft → Validated）

        [Fact]
        public async Task Integration_ValidationFlow_ShouldTransitionFromDraftToValidated()
        {
            // Arrange - 创建一个Draft状态的验方，使用不存在的药材
            var formula = new Formula
            {
                Id = Guid.NewGuid(),
                Name = $"测试验方_{DateTime.Now:yyyyMMddHHmmss}",
                Category = "补益剂",
                ValidationStatus = FormulaValidationStatus.Draft,
                Status = CommonStatus.Enabled,
                Herbs = new List<FormulaHerbItem>
                {
                    new FormulaHerbItem
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "未验证药材1",
                        OriginalHerbName = "未验证药材1",
                        IsValidated = false,
                        Dosage = 10,
                        Unit = "g"
                    },
                    new FormulaHerbItem
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "未验证药材2",
                        OriginalHerbName = "未验证药材2",
                        IsValidated = false,
                        Dosage = 15,
                        Unit = "g"
                    }
                }
            };

            _context.Formulas.Add(formula);
            await _context.SaveChangesAsync();
            _testFormulaIds.Add(formula.Id);

            // 获取真实药材用于验证
            var realHerbs = await _context.Herbs
                .Where(h => h.Status == CommonStatus.Enabled)
                .Take(2)
                .ToListAsync();

            if (realHerbs.Count < 2)
            {
                // 跳过测试如果药材库中没有足够的药材
                return;
            }

            var herbItem1 = formula.Herbs.First();
            var herbItem2 = formula.Herbs.Last();

            // Act - 验证第一个药材
            var validateResult1 = await _formulaService.ValidateFormulaHerbAsync(
                formula.Id,
                herbItem1.Id,
                realHerbs[0].Id);

            // Assert - 验证第一个药材成功，但验方状态仍为Draft
            validateResult1.Should().NotBeNull();
            validateResult1.IsSuccess.Should().BeTrue();

            var updatedFormula1 = await _formulaRepository.GetByIdWithHerbsAsync(formula.Id);
            updatedFormula1.Should().NotBeNull();
            updatedFormula1!.ValidationStatus.Should().Be(FormulaValidationStatus.Draft); // 还有未验证的药材
            updatedFormula1.Herbs.Count(h => h.IsValidated).Should().Be(1);

            // Act - 验证第二个药材
            var validateResult2 = await _formulaService.ValidateFormulaHerbAsync(
                formula.Id,
                herbItem2.Id,
                realHerbs[1].Id);

            // Assert - 验证第二个药材成功，验方状态自动更新为Validated
            validateResult2.Should().NotBeNull();
            validateResult2.IsSuccess.Should().BeTrue();

            var updatedFormula2 = await _formulaRepository.GetByIdWithHerbsAsync(formula.Id);
            updatedFormula2.Should().NotBeNull();
            updatedFormula2!.ValidationStatus.Should().Be(FormulaValidationStatus.Validated); // 所有药材已验证
            updatedFormula2.Herbs.All(h => h.IsValidated).Should().BeTrue();
        }

        #endregion

        #region 集成测试 4: 获取待验证验方列表

        [Fact]
        public async Task Integration_GetPendingValidationFormulas_ShouldReturnOnlyDraftFormulas()
        {
            // Arrange - 创建多个不同状态的验方
            var timestamp = DateTime.Now.Ticks;

            var draftFormula1 = new Formula
            {
                Id = Guid.NewGuid(),
                Name = $"待验证验方1_{timestamp}",
                ValidationStatus = FormulaValidationStatus.Draft,
                Status = CommonStatus.Enabled,
                Herbs = new List<FormulaHerbItem>()
            };

            var draftFormula2 = new Formula
            {
                Id = Guid.NewGuid(),
                Name = $"待验证验方2_{timestamp}",
                ValidationStatus = FormulaValidationStatus.Draft,
                Status = CommonStatus.Enabled,
                Herbs = new List<FormulaHerbItem>()
            };

            var validatedFormula = new Formula
            {
                Id = Guid.NewGuid(),
                Name = $"已验证验方_{timestamp}",
                ValidationStatus = FormulaValidationStatus.Validated,
                Status = CommonStatus.Enabled,
                Herbs = new List<FormulaHerbItem>()
            };

            _context.Formulas.AddRange(draftFormula1, draftFormula2, validatedFormula);
            await _context.SaveChangesAsync();

            _testFormulaIds.AddRange(new[] { draftFormula1.Id, draftFormula2.Id, validatedFormula.Id });

            // Act
            var result = await _formulaService.GetPendingValidationFormulasAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            // 验证返回的验方都是Draft状态
            result.Data!.Should().OnlyContain(f => f.ValidationStatus == FormulaValidationStatus.Draft);

            // 验证包含我们创建的待验证验方
            result.Data.Should().Contain(f => f.Name == draftFormula1.Name);
            result.Data.Should().Contain(f => f.Name == draftFormula2.Name);

            // 验证不包含已验证的验方
            result.Data.Should().NotContain(f => f.Name == validatedFormula.Name);
        }

        #endregion

        #region 集成测试 5: 验方CRUD完整流程

        [Fact]
        public async Task Integration_FormulaCRUD_ShouldWorkCorrectly()
        {
            // Arrange - 从真实药材库获取药材
            var realHerbs = await GetRealHerbNamesAsync(2);
            if (realHerbs.Count == 0)
            {
                return; // 跳过测试如果药材库为空
            }

            var createDto = new FormulaInputDto
            {
                Name = $"CRUD测试验方_{DateTime.Now:yyyyMMddHHmmss}",
                Effect = "测试功效",
                Usage = "测试用法",
                IsShared = false,
                Herbs = realHerbs.Select((name, index) => new FormulaHerbItemInputDto
                {
                    HerbName = name,
                    Dosage = 10 + index * 5,
                    Unit = "g"
                }).ToList()
            };

            // Act - Create
            var createResult = await _formulaService.CreateAsync(createDto);

            // Assert - Create
            createResult.Should().NotBeNull();
            createResult.IsSuccess.Should().BeTrue();
            createResult.Data.Should().NotBeNull();
            createResult.Data!.Name.Should().Be(createDto.Name);

            var createdId = createResult.Data.Id;
            _testFormulaIds.Add(createdId);

            // Act - Read
            var readResult = await _formulaService.GetByIdAsync(createdId);

            // Assert - Read
            readResult.Should().NotBeNull();
            readResult.IsSuccess.Should().BeTrue();
            readResult.Data.Should().NotBeNull();
            readResult.Data!.Id.Should().Be(createdId);

            // Act - Update
            var updateDto = new FormulaInputDto
            {
                Id = createdId,
                Name = createDto.Name + "_已更新",
                Effect = "更新后的功效",
                Usage = "更新后的用法",
                IsShared = true,
                Herbs = createDto.Herbs
            };
            var updateResult = await _formulaService.UpdateAsync(createdId, updateDto);

            // Assert - Update
            updateResult.Should().NotBeNull();
            updateResult.IsSuccess.Should().BeTrue();
            updateResult.Data!.Name.Should().EndWith("_已更新");
            updateResult.Data.Effect.Should().Be("更新后的功效");

            // Act - Delete
            await _formulaService.DeleteAsync(createdId);

            // Assert - Delete (验方应该被软删除)
            var deletedFormula = await _context.Formulas
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.Id == createdId);
            deletedFormula.Should().NotBeNull();
            deletedFormula!.IsDeleted.Should().BeTrue();
        }

        #endregion

        #region 清理

        public void Dispose()
        {
            CleanupTestData();
            _scope?.Dispose();
            _serviceProvider?.Dispose();
        }

        private void CleanupTestData()
        {
            if (_context == null) return;

            try
            {
                // 清理本次测试创建的验方
                if (_testFormulaIds.Any())
                {
                    var testFormulas = _context.Formulas
                        .IgnoreQueryFilters()
                        .Where(f => _testFormulaIds.Contains(f.Id))
                        .ToList();

                    if (testFormulas.Any())
                    {
                        _context.Formulas.RemoveRange(testFormulas);
                        _context.SaveChanges();
                    }
                }

                // 清理可能遗留的测试数据（根据名称特征）
                var orphanedTestFormulas = _context.Formulas
                    .IgnoreQueryFilters()
                    .Where(f => f.Name.Contains("集成测试验方") ||
                               f.Name.Contains("测试验方_") ||
                               f.Name.Contains("CRUD测试验方"))
                    .ToList();

                if (orphanedTestFormulas.Any())
                {
                    _context.Formulas.RemoveRange(orphanedTestFormulas);
                    _context.SaveChanges();
                }
            }
            catch
            {
                // 忽略清理错误
            }
        }

        #endregion
    }
}
