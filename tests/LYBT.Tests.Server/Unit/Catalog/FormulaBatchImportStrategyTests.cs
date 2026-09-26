using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Module.Catalog.Application.Commands;
using LYBT.Module.Catalog.Infrastructure;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Server.Unit.Catalog;

/// <summary>
/// US-SHELL-021 AC② — 验方批量导入的重复处理策略（Skip/Update/Error）。
/// 重复键 = 验方名称（对齐药材/患者导入约定）；失败行号按 Excel 约定（表头为第 1 行，首条数据行 = 2）。
/// 真实 SQLite 仓储 + 真实 CatalogDbContext。
/// </summary>
public class FormulaBatchImportStrategyTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CatalogDbContext _context;
    private readonly FormulaRepository _formulaRepository;
    private readonly HerbRepository _herbRepository;
    private readonly BatchImportFormulasCommandHandler _handler;

    public FormulaBatchImportStrategyTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        _connection.Open();
        _context = new CatalogDbContext(
            new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
        _formulaRepository = new FormulaRepository(_context, NullLogger<FormulaRepository>.Instance);
        _herbRepository = new HerbRepository(_context, NullLogger<HerbRepository>.Instance);
        _handler = new BatchImportFormulasCommandHandler(
            _formulaRepository, _herbRepository, NullLogger<BatchImportFormulasCommandHandler>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Formulas_BatchImport_SkipStrategy_LeavesExistingFormulaUntouched()
    {
        var existing = await SeedFormulaAsync("四君子汤", effect: "旧功效", herbName: "人参", dosage: 3);

        var result = await _handler.Handle(
            new BatchImportFormulasCommand(
                new List<FormulaImportItemDto>
                {
                    new()
                    {
                        Name = "四君子汤",
                        Effect = "新功效",
                        Usage = "水煎服",
                        Herbs = new List<FormulaHerbImportItemDto> { new() { HerbName = "人参", Dosage = 10, Unit = "克" } },
                    },
                },
                FileName: null,
                Strategy: DuplicateStrategy.Skip,
                CurrentUserId: Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value!;
        report.TotalCount.Should().Be(1);
        report.SkippedCount.Should().Be(1);
        report.SuccessCount.Should().Be(0);
        report.FailureCount.Should().Be(0);
        (report.SuccessCount + report.SkippedCount + report.FailureCount).Should().Be(report.TotalCount);

        await using var fresh = CreateFreshContext();
        var persisted = await fresh.Formulas.Include(f => f.Herbs).SingleAsync();
        persisted.Id.Should().Be(existing.Id);
        persisted.Effect.Should().Be("旧功效", "Skip 策略不得改动已存在验方");
        persisted.Herbs.Should().ContainSingle();
    }

    [Fact]
    public async Task Formulas_BatchImport_UpdateStrategy_OverwritesExistingFormulaAndHerbs()
    {
        var existing = await SeedFormulaAsync("四君子汤", effect: "旧功效", herbName: "人参", dosage: 3);

        var result = await _handler.Handle(
            new BatchImportFormulasCommand(
                new List<FormulaImportItemDto>
                {
                    new()
                    {
                        Name = "四君子汤",
                        Effect = "新功效",
                        Usage = "水煎服",
                        Category = "补益剂",
                        Herbs = new List<FormulaHerbImportItemDto>
                        {
                            new() { HerbName = "人参", Dosage = 10, Unit = "克" },
                            new() { HerbName = "未匹配药材", Dosage = 2, Unit = "克" },
                        },
                    },
                },
                FileName: null,
                Strategy: DuplicateStrategy.Update,
                CurrentUserId: Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value!;
        report.TotalCount.Should().Be(1);
        report.SuccessCount.Should().Be(1);
        report.SkippedCount.Should().Be(0);
        report.FailureCount.Should().Be(0);
        report.SuccessfulIds.Should().ContainSingle().Which.Should().Be(existing.Id);

        await using var fresh = CreateFreshContext();
        var persisted = await fresh.Formulas.Include(f => f.Herbs).SingleAsync();
        persisted.Id.Should().Be(existing.Id, "Update 策略复用既有行，不新建");
        persisted.Effect.Should().Be("新功效");
        persisted.Category.Should().Be("补益剂");
        persisted.Herbs.Should().HaveCount(2, "药材组成整组替换");
        persisted.Herbs.Any(h => h.HerbName == "未匹配药材").Should().BeTrue();
    }

    [Fact]
    public async Task Formulas_BatchImport_ErrorStrategy_ReportsDuplicateRowWithExcelRowNumber()
    {
        var existing = await SeedFormulaAsync("四君子汤", effect: "旧功效", herbName: "人参", dosage: 3);

        var result = await _handler.Handle(
            new BatchImportFormulasCommand(
                new List<FormulaImportItemDto>
                {
                    NewFormula("导入方甲"),
                    NewFormula("四君子汤"),
                },
                FileName: null,
                Strategy: DuplicateStrategy.Error,
                CurrentUserId: Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value!;
        report.TotalCount.Should().Be(2);
        report.SuccessCount.Should().Be(1, "第 1 行非重复 → 新增成功");
        report.FailureCount.Should().Be(1);
        report.SkippedCount.Should().Be(0);

        var failure = report.Failures.Should().ContainSingle().Subject;
        failure.RowIndex.Should().Be(3, "第 2 条数据行 = Excel 第 3 行（表头行号为 1）");
        failure.FormulaName.Should().Be("四君子汤");

        await using var fresh = CreateFreshContext();
        var persistedExisting = await fresh.Formulas.SingleAsync(f => f.Id == existing.Id);
        persistedExisting.Effect.Should().Be("旧功效", "Error 策略不得改动已存在验方");
    }

    private static FormulaImportItemDto NewFormula(string name) => new()
    {
        Name = name,
        Effect = "功效",
        Usage = "水煎服",
        Herbs = new List<FormulaHerbImportItemDto> { new() { HerbName = "人参", Dosage = 3, Unit = "克" } },
    };

    private async Task<Formula> SeedFormulaAsync(string name, string effect, string herbName, int dosage)
    {
        var herb = Herb.Create(herbName, "克", 30m, "RS");
        _context.Herbs.Add(herb);

        var formula = Formula.Create(name: name, effect: effect, usage: "水煎服", category: "补益剂");
        formula.ReplaceHerbs(new[] { FormulaHerbItem.Create(formula.Id, herbName, dosage, "克", herb.Id) });
        _context.Formulas.Add(formula);

        await _context.SaveChangesAsync();
        return formula;
    }

    private CatalogDbContext CreateFreshContext()
        => new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);
}
