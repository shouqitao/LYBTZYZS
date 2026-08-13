using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Catalog.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using LYBT.Tests.Server.Integration;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Tests.Server.Integration.Data;

/// <summary>
/// L3 真实 SQL Server 验证（任务书 formula-realsql-fix-2026-08-13）:
/// Formula 更新（ReplaceHerbs 子集合替换）在真实 SQL Server 上不得触发并发冲突——
/// SQLite 通过不算数（EF 对 rowversion/子集合的行为在 SQL Server 上有差异）。
/// 需 TEST_DB_CONNECTION 环境变量（连接 192.168.190.243 LYBTDB_Test——测试库空库 EF 迁移自建）。
/// </summary>
public class FormulaRealSqlUpdateTests : IDisposable
{
    private readonly string? _connectionString;
    private readonly bool _enabled;
    private readonly DbContextOptions<CatalogDbContext> _options;
    private readonly CatalogDbContext _context;

    public FormulaRealSqlUpdateTests()
    {
        _connectionString = TestDatabase.ConnectionString; // TEST_DB_CONNECTION 环境变量
        _enabled = !string.IsNullOrWhiteSpace(_connectionString);
        if (!_enabled)
        {
            _options = null!;
            _context = null!;
            return;
        }

        _options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        _context = new CatalogDbContext(_options);
    }

    [Fact]
    public async Task UpdateFormula_ReplaceHerbs_OnRealSqlServer_PersistsWithoutConcurrencyError()
    {
        if (!_enabled)
            return; // 未配置 TEST_DB_CONNECTION——真实 SQL 验证跳过（任务书: 需用户设置环境变量）

        await _context.Database.EnsureCreatedAsync(); // 空库 EF 迁移自建（LYBTDB_Test）

        // 建两味药材（引用校验依赖——HerbId 必须存在）
        var herbA = Herb.Create(name: "黄芪", unit: "g", price: 0m, pinYinCode: "HQ", category: "补气");
        var herbB = Herb.Create(name: "白术", unit: "g", price: 0m, pinYinCode: "BZ", category: "健脾");
        _context.Herbs.AddRange(herbA, herbB);
        await _context.SaveChangesAsync();

        // 建验方（含组成 A）——走真实 FormulaRepository 路径
        var repository = new FormulaRepository(_context, NullLogger<FormulaRepository>.Instance);
        var formula = Formula.Create(
            "真实SQL测试方", "补气健脾", "气虚", "水煎服", "测试", null, "内科",
            formulaType: FormulaType.Classic, isShared: false,
            userId: Guid.NewGuid(), createdBy: Guid.NewGuid());
        formula.ReplaceHerbs(new List<FormulaHerbItem>
        {
            FormulaHerbItem.Create(formula.Id, "黄芪", dosage: 9, unit: "g", herbId: herbA.Id, originalHerbName: "黄芪"),
        });
        _context.Formulas.Add(formula);
        await _context.SaveChangesAsync();

        // 更新：换组成（ReplaceHerbs 全替换 + 新 Guid）——真实 SQL 上不得 0 rows 并发异常
        var loaded = await _context.Formulas.Include(f => f.Herbs).FirstAsync(f => f.Id == formula.Id);
        loaded.ReplaceHerbs(new List<FormulaHerbItem>
        {
            FormulaHerbItem.Create(loaded.Id, "白术", dosage: 12, unit: "g", herbId: herbB.Id, originalHerbName: "白术"),
            FormulaHerbItem.Create(loaded.Id, "黄芪", dosage: 9, unit: "g", herbId: herbA.Id, originalHerbName: "黄芪"),
        });

        // 模拟 handler 路径（BaseRepository.UpdateAsync + FormulaRepository override）
        await repository.UpdateAsync(loaded, CancellationToken.None);

        var persisted = await _context.Formulas.Include(f => f.Herbs).FirstAsync(f => f.Id == formula.Id);
        Assert.Equal(2, persisted.Herbs.Count);
        Assert.DoesNotContain(persisted.Herbs, h => h.HerbName == "黄芪" && h.Id == Guid.Empty);
        Assert.Contains(persisted.Herbs, h => h.HerbName == "白术");

        // 清理（测试库幂等——删本测试数据）
        _context.FormulaHerbItems.RemoveRange(persisted.Herbs);
        _context.Formulas.Remove(persisted);
        _context.Herbs.RemoveRange(herbA, herbB);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateFormula_DetachedReplaceHerbs_OnRealSqlServer_InsertsNewItems()
    {
        if (!_enabled)
            return; // 未配置 TEST_DB_CONNECTION——跳过

        await _context.Database.EnsureCreatedAsync();

        var herbA = Herb.Create(name: "甘草", unit: "g", price: 0m, pinYinCode: "GC", category: "调和");
        _context.Herbs.Add(herbA);
        await _context.SaveChangesAsync();

        var formula = Formula.Create(
            "真实SQL测试方2", "调和", "诸症", "水煎服", "测试", null, "内科",
            formulaType: FormulaType.Experience, isShared: false,
            userId: Guid.NewGuid(), createdBy: Guid.NewGuid());
        formula.ReplaceHerbs(new List<FormulaHerbItem>
        {
            FormulaHerbItem.Create(formula.Id, "甘草", dosage: 3, unit: "g", herbId: herbA.Id, originalHerbName: "甘草"),
        });
        _context.Formulas.Add(formula);
        await _context.SaveChangesAsync();

        // Detached 场景: 新 context 加载 + 换组成（新 Guid 新 item）——必须 INSERT 非 UPDATE
        await using var fresh = new CatalogDbContext(_options);
        var repo = new FormulaRepository(fresh, NullLogger<FormulaRepository>.Instance);
        var detached = await fresh.Formulas.Include(f => f.Herbs).FirstAsync(f => f.Id == formula.Id);
        detached.ReplaceHerbs(new List<FormulaHerbItem>
        {
            FormulaHerbItem.Create(detached.Id, "甘草", dosage: 5, unit: "g", herbId: herbA.Id, originalHerbName: "甘草"),
        });
        await repo.UpdateAsync(detached, CancellationToken.None);

        var persisted = await fresh.Formulas.Include(f => f.Herbs).FirstAsync(f => f.Id == formula.Id);
        Assert.Single(persisted.Herbs);
        Assert.Equal(5, persisted.Herbs.First().Dosage);

        fresh.FormulaHerbItems.RemoveRange(persisted.Herbs);
        fresh.Formulas.Remove(persisted);
        fresh.Herbs.Remove(herbA);
        await fresh.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
