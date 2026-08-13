using FluentAssertions;
using LYBT.Entities.Formulas;
using LYBT.Module.Catalog.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Tests.Server.Unit.Catalog;

/// <summary>
/// Formula.ReplaceHerbs 子集合替换测试（2026-08-13 第 2 层根因——真机 PUT formula 仍 500）：
/// 修复（方案 A 孤儿删除模式）后——子集合替换不触发父行 RowVersion 冲突——正常落库。
/// SQLite concurrency token 宽松——真机严格验证需 SQL Server rowversion（TEST_DB_CONNECTION）。
/// </summary>
public class FormulaReplaceHerbsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CatalogDbContext _context;
    private readonly FormulaRepository _repository;
    private readonly string _sqlLogPath;

    public FormulaReplaceHerbsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _sqlLogPath = Path.Combine(Path.GetTempPath(), "ef-sql-" + Guid.NewGuid().ToString("N") + ".log");
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new CatalogDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new FormulaRepository(_context, NullLogger<FormulaRepository>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        try { if (File.Exists(_sqlLogPath)) File.Delete(_sqlLogPath); } catch { /* 清理尽力 */ }
    }

    private Formula CreateFormulaWithHerbs(params (string Name, int Dosage)[] herbs)
    {
        var formula = Formula.Create(name: "四君子汤", effect: "补气健脾", category: "补益剂",
            createdBy: Guid.NewGuid());
        foreach (var (name, dosage) in herbs)
            formula.ReplaceHerbs(new[] { FormulaHerbItem.Create(formula.Id, name, dosage) });
        _context.Formulas.Add(formula);
        _context.SaveChanges();
        return formula;
    }

    [Fact]
    public async Task ReplaceHerbs_UpdateFormula_AppliesWithoutConcurrencyError()
    {
        // 复现路径（真机 PUT formula）: 加载（跟踪）→ ApplyUpdate（ReplaceHerbs 子集合替换）→ UpdateAsync
        var formula = CreateFormulaWithHerbs(("人参", 3), ("白术", 5));

        var loaded = await _context.Formulas.Include(f => f.Herbs)
            .FirstOrDefaultAsync(f => f.Id == formula.Id);
        loaded.Should().NotBeNull();
        loaded!.UpdateProfile(name: "四君子汤改", effect: "益气健脾", indication: null, usage: "水煎服",
            remark: null, property: null, category: "补益剂", isShared: false, updatedBy: Guid.NewGuid());
        // ReplaceHerbs: 改药材组成（删旧加新——子集合替换）
        loaded.ReplaceHerbs(new[]
        {
            FormulaHerbItem.Create(loaded.Id, "人参", 3),
            FormulaHerbItem.Create(loaded.Id, "茯苓", 5),
            FormulaHerbItem.Create(loaded.Id, "甘草", 2)
        });

        var act = async () => await _repository.UpdateAsync(loaded);

        try { await act.Should().NotThrowAsync("ReplaceHerbs 子集合替换不得触发父行 RowVersion 冲突"); }
        finally
        {
            // 日志文件保留供诊断（测试后 bash 读取）
        }

        // 落库验证
        var fresh = await _context.Formulas.Include(f => f.Herbs)
            .FirstOrDefaultAsync(f => f.Id == formula.Id);
        fresh!.Name.Should().Be("四君子汤改");
        fresh.Herbs.Count.Should().Be(3, "新 herbs 组成应落库");
        fresh.Herbs.Any(h => h.HerbName == "茯苓").Should().BeTrue();
        fresh.Herbs.Any(h => h.HerbName == "白术").Should().BeFalse("旧 herbs 应被删除（孤儿删除）");
    }

    [Fact]
    public async Task ReplaceHerbs_SameComposition_AppliesCleanly()
    {
        // 同一组成更新（无实际子集合变化——herb id 新 Guid——替换语义）
        var formula = CreateFormulaWithHerbs(("人参", 3));

        var loaded = await _context.Formulas.Include(f => f.Herbs)
            .FirstOrDefaultAsync(f => f.Id == formula.Id);
        loaded!.UpdateProfile(name: "四君子汤v2", effect: "补气", indication: null, usage: "水煎服",
            remark: null, property: null, category: "补益剂", isShared: false, updatedBy: Guid.NewGuid());
        loaded.ReplaceHerbs(new[] { FormulaHerbItem.Create(loaded.Id, "人参", 4) });

        var act = async () => await _repository.UpdateAsync(loaded);

        await act.Should().NotThrowAsync();
        var fresh = await _context.Formulas.Include(f => f.Herbs)
            .FirstOrDefaultAsync(f => f.Id == formula.Id);
        fresh!.Herbs.Single().Dosage.Should().Be(4);
    }
}
