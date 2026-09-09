using LYBT.Entities.Formulas;
using LYBT.Module.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace LYBT.Tests.Server.Integration.Data;

/// <summary>
/// 深挖（formula-deep-fix-2026-08-13）: PUT formula 并发冲突——LYBTDB_Dev（非空库——真实数据）
/// 复现。本地 LYBTDB_Test 空库通过但 Dev 失败——差异实证。
/// 连接串读自 src/Server/Services/LYBT.WebAPI/.env.development（真实 Dev 连接——不打印）。
/// 安全：所有变更在事务内回滚（ReplaceHerbs 换新 item——未提交无痕；旧 item DELETE 回滚恢复）。
/// </summary>
public class FormulaDevReproTests : IDisposable
{
    private readonly string? _connectionString;
    private readonly bool _enabled;
    private readonly string _sqlLogPath;
    private readonly ITestOutputHelper _output;
    private CatalogDbContext? _context;

    public FormulaDevReproTests(ITestOutputHelper output)
    {
        _output = output;
        _sqlLogPath = Path.Combine(Path.GetTempPath(), $"ef-dev-repro-{Guid.NewGuid():N}.log");
        _connectionString = ReadDevConnectionString();
        _enabled = !string.IsNullOrWhiteSpace(_connectionString);
    }

    private static string? ReadDevConnectionString()
    {
        var envPath = Path.Combine(
            AppContext.BaseDirectory[..AppContext.BaseDirectory.IndexOf("tests", StringComparison.OrdinalIgnoreCase)],
            "src", "Server", "Services", "LYBT.WebAPI", ".env.development");
        if (!File.Exists(envPath))
            return null;
        var line = File.ReadLines(envPath)
            .FirstOrDefault(l => l.StartsWith("ConnectionStrings__DefaultConnection="));
        if (line == null)
            return null;
        var value = line[(line.IndexOf('=') + 1)..].Trim();
        if (value.StartsWith('"') && value.EndsWith('"'))
            value = value[1..^1];
        return value;
    }

    [Fact]
    public async Task Repro_PutFormula_OnDev_WithExistingData()
    {
        if (!_enabled)
        {
            _output.WriteLine("SKIP: .env.development 不可读——无法连 Dev");
            return;
        }

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer(_connectionString)
            .LogTo(s => File.AppendAllText(_sqlLogPath, s + Environment.NewLine),
                Microsoft.Extensions.Logging.LogLevel.Information)
            .Options;

        _context = new CatalogDbContext(options);

        // 加载现有 formula（真实数据——含 herbs）
        Formula? existing;
        try
        {
            existing = await _context.Formulas
                .Include(f => f.Herbs)
                .Where(f => f.Herbs.Any())
                .OrderBy(f => f.CreatedAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            // Dev 不可达（网络/预登录握手失败等环境性故障）→ SKIP：
            // 诊断复现测试不应因 Dev 机器离线而阻塞全量测试套件
            _output.WriteLine($"SKIP: Dev 连接失败——{ex.GetType().Name}: {ex.Message}");
            return;
        }

        if (existing == null)
        {
            _output.WriteLine("SKIP: Dev 库无含 herbs 的验方——无法复现真实更新场景");
            return;
        }

        _output.WriteLine($"复现对象: formula={existing.Id} name={existing.Name} herbs={existing.Herbs.Count}");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        // 模拟 PUT body：同 herbs 内容重建（真实客户端回传——同 herbId 同剂量）
        var replacement = existing.Herbs
            .Select(h => FormulaHerbItem.Create(
                existing.Id, h.HerbName, h.Dosage, h.Unit, h.HerbId, h.OriginalHerbName,
                h.Usage, h.Remark, h.ProcessingMethod, h.DecocteMethod))
            .ToList();

        existing.ReplaceHerbs(replacement);

        var repository = new FormulaRepository(_context, NullLogger<FormulaRepository>.Instance);
        try
        {
            await repository.UpdateAsync(existing, CancellationToken.None);
            _output.WriteLine("REPRO-NEGATIVE: Dev 上更新成功（无并发异常）——本地未复现，需服务器日志");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"REPRODUCED: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        // EF SQL 日志取证（脱敏打印——最后 30 行 DML）
        var logLines = File.Exists(_sqlLogPath)
            ? File.ReadAllLines(_sqlLogPath).Where(l => l.Contains("UPDATE") || l.Contains("INSERT") || l.Contains("DELETE")).ToList()
            : new List<string>();
        foreach (var l in logLines.TakeLast(30))
            _output.WriteLine("SQL: " + l);
    }

    public void Dispose()
    {
        _context?.Dispose();
        try { if (File.Exists(_sqlLogPath)) File.Delete(_sqlLogPath); } catch { }
    }
}
