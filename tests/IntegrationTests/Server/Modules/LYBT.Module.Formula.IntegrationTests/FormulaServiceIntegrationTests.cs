using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Formulas;
using LYBT.Infrastructure.Data;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Mapping;
using LYBT.Module.Formulas.Repositories;
using LYBT.Module.Formulas.Services;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Repositories;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OfficeOpenXml;
using Xunit;

namespace LYBT.Module.Formulas.IntegrationTests
{
    /// <summary>
    /// 验方服务集成测试 - 测试完整的工作流
    /// Issue #1357: 验证导入→验证→使用的端到端流程
    /// </summary>
    public class FormulaServiceIntegrationTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IFormulaRepository _formulaRepository;
        private readonly IHerbRepository _herbRepository;
        private readonly FormulaService _formulaService;
        private readonly IMapper _mapper;

        public FormulaServiceIntegrationTests()
        {
            // 配置 AutoMapper
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<FormulaMappingProfile>();
            });
            _mapper = config.CreateMapper();

            // 创建内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"FormulaIntegrationTestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);

            // 创建真实的Repository
            _formulaRepository = new FormulaRepository(_context);
            _herbRepository = new HerbRepository(_context);

            // 创建Mock Logger
            var mockLogger = new Mock<ILogger<FormulaService>>();

            // 创建FormulaService（使用真实依赖）
            _formulaService = new FormulaService(
                _formulaRepository,
                _herbRepository,
                _mapper,
                mockLogger.Object);

            // 初始化测试数据
            SeedTestData();
        }

        #region 测试数据准备

        private void SeedTestData()
        {
            // 添加测试药材
            var herbs = new[]
            {
                new LYBT.Entities.Herbs.Herb { Id = Guid.NewGuid(), Name = "人参", PinYinCode = "RS", Unit = "g", Price = 50m, Status = CommonStatus.Enabled },
                new LYBT.Entities.Herbs.Herb { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "DG", Unit = "g", Price = 30m, Status = CommonStatus.Enabled },
                new LYBT.Entities.Herbs.Herb { Id = Guid.NewGuid(), Name = "黄芪", PinYinCode = "HQ", Unit = "g", Price = 25m, Status = CommonStatus.Enabled },
                new LYBT.Entities.Herbs.Herb { Id = Guid.NewGuid(), Name = "白术", PinYinCode = "BS", Unit = "g", Price = 20m, Status = CommonStatus.Enabled }
            };

            _context.Herbs.AddRange(herbs);
            _context.SaveChanges();
        }

        private Stream CreateTestExcelFile(bool withValidHerbs = true)
        {
            var stream = new MemoryStream();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(stream))
            {
                // Sheet1: 验方信息
                var formulaSheet = package.Workbook.Worksheets.Add("验方信息");
                formulaSheet.Cells[1, 1].Value = "验方编号";
                formulaSheet.Cells[1, 2].Value = "验方名称";
                formulaSheet.Cells[1, 3].Value = "分类";
                formulaSheet.Cells[1, 4].Value = "功效";
                formulaSheet.Cells[1, 5].Value = "用法";
                formulaSheet.Cells[1, 6].Value = "性味";
                formulaSheet.Cells[1, 7].Value = "验方类型";
                formulaSheet.Cells[1, 8].Value = "是否共享";
                formulaSheet.Cells[1, 9].Value = "备注";

                formulaSheet.Cells[2, 1].Value = "F001";
                formulaSheet.Cells[2, 2].Value = "补气养血方";
                formulaSheet.Cells[2, 3].Value = "补益剂";
                formulaSheet.Cells[2, 4].Value = "补气养血，健脾益气";
                formulaSheet.Cells[2, 5].Value = "水煎服，每日2次";
                formulaSheet.Cells[2, 6].Value = "温";
                formulaSheet.Cells[2, 7].Value = "经典";
                formulaSheet.Cells[2, 8].Value = "是";
                formulaSheet.Cells[2, 9].Value = "集成测试验方";

                // Sheet2: 药材明细
                var herbSheet = package.Workbook.Worksheets.Add("药材明细");
                herbSheet.Cells[1, 1].Value = "验方编号";
                herbSheet.Cells[1, 2].Value = "药材名称";
                herbSheet.Cells[1, 3].Value = "用量";
                herbSheet.Cells[1, 4].Value = "单位";

                if (withValidHerbs)
                {
                    // 使用存在的药材名称
                    herbSheet.Cells[2, 1].Value = "F001";
                    herbSheet.Cells[2, 2].Value = "人参";
                    herbSheet.Cells[2, 3].Value = 10;
                    herbSheet.Cells[2, 4].Value = "g";

                    herbSheet.Cells[3, 1].Value = "F001";
                    herbSheet.Cells[3, 2].Value = "当归";
                    herbSheet.Cells[3, 3].Value = 15;
                    herbSheet.Cells[3, 4].Value = "g";

                    herbSheet.Cells[4, 1].Value = "F001";
                    herbSheet.Cells[4, 2].Value = "黄芪";
                    herbSheet.Cells[4, 3].Value = 20;
                    herbSheet.Cells[4, 4].Value = "g";
                }
                else
                {
                    // 使用不存在的药材名称
                    herbSheet.Cells[2, 1].Value = "F001";
                    herbSheet.Cells[2, 2].Value = "不存在的药材1";
                    herbSheet.Cells[2, 3].Value = 10;
                    herbSheet.Cells[2, 4].Value = "g";

                    herbSheet.Cells[3, 1].Value = "F001";
                    herbSheet.Cells[3, 2].Value = "不存在的药材2";
                    herbSheet.Cells[3, 3].Value = 15;
                    herbSheet.Cells[3, 4].Value = "g";
                }

                package.Save();
            }

            stream.Position = 0;
            return stream;
        }

        #endregion

        #region 集成测试 1: 导入Excel成功（药材全部匹配）

        [Fact]
        public async Task Integration_ImportExcel_WithFullMatching_ShouldImportWithValidatedHerbs()
        {
            // Arrange
            using var excelStream = CreateTestExcelFile(withValidHerbs: true);

            // Act - 导入Excel
            var importResult = await _formulaService.ImportFromExcelAsync(excelStream, "test.xlsx");

            // Assert
            importResult.Should().NotBeNull();
            importResult.IsSuccess.Should().BeTrue();
            importResult.Data.Should().NotBeNull();
            importResult.Data!.SuccessCount.Should().Be(1);
            importResult.Data.ImportedData.Should().HaveCount(1);

            var importedFormula = importResult.Data.ImportedData[0];
            importedFormula.Name.Should().Be("补气养血方");
            importedFormula.ValidationStatus.Should().Be(FormulaValidationStatus.Draft); // 导入后为Draft
            importedFormula.Herbs.Should().HaveCount(3);

            // 验证药材是否自动匹配成功
            var matchedHerbs = importedFormula.Herbs.Where(h => h.IsValidated).ToList();
            matchedHerbs.Should().HaveCount(3); // 所有药材都应该自动匹配成功
        }

        #endregion

        #region 集成测试 2: 导入Excel（药材部分不匹配）

        [Fact]
        public async Task Integration_ImportExcel_WithPartialMatching_ShouldImportWithUnvalidatedHerbs()
        {
            // Arrange
            using var excelStream = CreateTestExcelFile(withValidHerbs: false);

            // Act - 导入Excel
            var importResult = await _formulaService.ImportFromExcelAsync(excelStream, "test.xlsx");

            // Assert
            importResult.Should().NotBeNull();
            importResult.IsSuccess.Should().BeTrue();
            importResult.Data!.SuccessCount.Should().Be(1);

            var importedFormula = importResult.Data.ImportedData[0];
            importedFormula.ValidationStatus.Should().Be(FormulaValidationStatus.Draft);
            importedFormula.Herbs.Should().HaveCount(2);

            // 验证药材未自动匹配
            var unvalidatedHerbs = importedFormula.Herbs.Where(h => !h.IsValidated).ToList();
            unvalidatedHerbs.Should().HaveCount(2); // 所有药材都未匹配
            unvalidatedHerbs[0].OriginalHerbName.Should().Be("不存在的药材1");
            unvalidatedHerbs[1].OriginalHerbName.Should().Be("不存在的药材2");
        }

        #endregion

        #region 集成测试 3: 验证状态流转（Draft → Validated）

        [Fact]
        public async Task Integration_ValidationFlow_ShouldTransitionFromDraftToValidated()
        {
            // Arrange - 创建一个Draft状态的验方
            var formula = new LYBT.Entities.Formula.Formula
            {
                Id = Guid.NewGuid(),
                Name = "测试验方",
                Category = "补益剂",
                ValidationStatus = FormulaValidationStatus.Draft,
                Status = CommonStatus.Enabled,
                Herbs = new List<FormulaHerbItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "未验证药材1",
                        IsValidated = false,
                        Quantity = 10,
                        Unit = "g"
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        HerbName = "未验证药材2",
                        IsValidated = false,
                        Quantity = 15,
                        Unit = "g"
                    }
                }
            };

            _context.Formulas.Add(formula);
            await _context.SaveChangesAsync();

            var herb1 = _context.Herbs.First(h => h.Name == "人参");
            var herb2 = _context.Herbs.First(h => h.Name == "当归");

            // Act - 逐个验证药材
            var herbItem1 = formula.Herbs.First();
            var herbItem2 = formula.Herbs.Last();

            // 验证第一个药材
            var validateResult1 = await _formulaService.ValidateFormulaHerbAsync(
                formula.Id,
                herbItem1.Id,
                herb1.Id);

            // Assert - 验证第一个药材成功，但验方状态仍为Draft
            validateResult1.Should().NotBeNull();
            validateResult1.IsSuccess.Should().BeTrue();

            var updatedFormula1 = await _formulaRepository.GetByIdWithHerbsAsync(formula.Id);
            updatedFormula1.Should().NotBeNull();
            updatedFormula1!.ValidationStatus.Should().Be(FormulaValidationStatus.Draft); // 还有未验证的药材
            updatedFormula1.Herbs.Count(h => h.IsValidated).Should().Be(1);

            // 验证第二个药材
            var validateResult2 = await _formulaService.ValidateFormulaHerbAsync(
                formula.Id,
                herbItem2.Id,
                herb2.Id);

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
            var draftFormula1 = new LYBT.Entities.Formula.Formula
            {
                Id = Guid.NewGuid(),
                Name = "待验证验方1",
                ValidationStatus = FormulaValidationStatus.Draft,
                Status = CommonStatus.Enabled,
                Herbs = new List<FormulaHerbItem>()
            };

            var draftFormula2 = new LYBT.Entities.Formula.Formula
            {
                Id = Guid.NewGuid(),
                Name = "待验证验方2",
                ValidationStatus = FormulaValidationStatus.Draft,
                Status = CommonStatus.Enabled,
                Herbs = new List<FormulaHerbItem>()
            };

            var validatedFormula = new LYBT.Entities.Formula.Formula
            {
                Id = Guid.NewGuid(),
                Name = "已验证验方",
                ValidationStatus = FormulaValidationStatus.Validated,
                Status = CommonStatus.Enabled,
                Herbs = new List<FormulaHerbItem>()
            };

            _context.Formulas.AddRange(draftFormula1, draftFormula2, validatedFormula);
            await _context.SaveChangesAsync();

            // Act
            var result = await _formulaService.GetPendingValidationFormulasAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2); // 只有2个Draft状态的验方
            result.Data.Should().OnlyContain(f => f.ValidationStatus == FormulaValidationStatus.Draft);
            result.Data.Should().NotContain(f => f.Name == "已验证验方");
        }

        #endregion

        #region 清理

        public void Dispose()
        {
            _context?.Database.EnsureDeleted();
            _context?.Dispose();
        }

        #endregion
    }
}
