using LYBT.Entities.Herbs;
using LYBT.Module.Catalog.Application.Commands;
using LYBT.Module.Catalog.Infrastructure;
using LYBT.Module.Patients.Application.Commands;
using LYBT.Module.Patients.Infrastructure;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Server.Unit.ImportExport;

/// <summary>
/// US-SHELL-021 AC④/AC⑤ — 三类批量导入的「整请求一事务」与统计口径一致性（真实 SQLite 仓储 + 真实 DbContext）。
/// <para>
/// AC④：一次导入请求的全部写入在同一个显式事务内（ADR-0030 同 DbContext 事务）；请求中途取消 →
/// 整批回滚，不留下「前 N 行已落库、第 N+1 行失败」的半批数据。
/// 逐行的 Skip/Update/Error 与校验失败仍是行级结果（见 <see cref="FormulaBatchImportStrategyTests"/>）。
/// </para>
/// <para>AC⑤：TotalCount = 成功 + 跳过 + 失败；SuccessfulIds 与 SuccessCount 数量一致。</para>
/// </summary>
public class BatchImportTransactionTests : IDisposable
{
    private readonly SqliteConnection _catalogConnection;
    private readonly SqliteConnection _patientsConnection;
    private readonly CancelAfterFirstSaveInterceptor _cancelInterceptor = new();
    private readonly CatalogDbContext _catalogContext;
    private readonly PatientsDbContext _patientsContext;
    private readonly HerbRepository _herbRepository;
    private readonly FormulaRepository _formulaRepository;
    private readonly PatientRepository _patientRepository;

    public BatchImportTransactionTests()
    {
        // Foreign Keys=False：本测试只验证事务/统计语义，不构造跨聚合外键前置数据
        _catalogConnection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        _catalogConnection.Open();
        _catalogContext = new CatalogDbContext(
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseSqlite(_catalogConnection)
                .AddInterceptors(_cancelInterceptor)
                .Options);
        _catalogContext.Database.EnsureCreated();
        _herbRepository = new HerbRepository(_catalogContext, NullLogger<HerbRepository>.Instance);
        _formulaRepository = new FormulaRepository(_catalogContext, NullLogger<FormulaRepository>.Instance);

        _patientsConnection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        _patientsConnection.Open();
        _patientsContext = new PatientsDbContext(
            new DbContextOptionsBuilder<PatientsDbContext>()
                .UseSqlite(_patientsConnection)
                .AddInterceptors(_cancelInterceptor)
                .Options);
        _patientsContext.Database.EnsureCreated();
        _patientRepository = new PatientRepository(_patientsContext, NullLogger<PatientRepository>.Instance);
    }

    public void Dispose()
    {
        _catalogContext.Dispose();
        _catalogConnection.Dispose();
        _patientsContext.Dispose();
        _patientsConnection.Dispose();
    }

    // ---------------------------------------------------------------- 患者

    [Fact]
    public async Task Patients_BatchImport_ReportsConsistentCounts_AndPersistsRows()
    {
        var handler = new BatchImportPatientsCommandHandler(_patientRepository);
        var request = new BatchImportPatientsCommand(
            new List<PatientInputDto>
            {
                new() { Name = "张三", Gender = Gender.Male, PhoneNumber = "13800000001" },
                new() { Name = "李四", Gender = Gender.Female, PhoneNumber = "13800000002" },
                // 批内重名 → 命中 Skip（同事务内前一行可见）
                new() { Name = "张三", Gender = Gender.Male, PhoneNumber = "13800000003" },
            },
            DuplicateStrategy.Skip,
            Guid.NewGuid());

        var result = await handler.Handle(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value!;
        report.TotalCount.Should().Be(3);
        report.SuccessCount.Should().Be(2);
        report.SkippedCount.Should().Be(1);
        report.FailureCount.Should().Be(0);
        report.SuccessfulIds.Should().HaveCount(2);
        (report.SuccessCount + report.SkippedCount + report.FailureCount).Should().Be(report.TotalCount);

        await using var fresh = CreateFreshPatientsContext();
        (await fresh.Patients.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Patients_BatchImport_CancelledMidRequest_RollsBackWholeRequest()
    {
        using var cts = new CancellationTokenSource();
        _cancelInterceptor.Enabled = true;
        _cancelInterceptor.Source = cts;

        var handler = new BatchImportPatientsCommandHandler(_patientRepository);
        var request = new BatchImportPatientsCommand(
            new List<PatientInputDto>
            {
                new() { Name = "王五", Gender = Gender.Male, PhoneNumber = "13900000001" },
                new() { Name = "赵六", Gender = Gender.Male, PhoneNumber = "13900000002" },
            },
            DuplicateStrategy.Skip,
            Guid.NewGuid());

        var act = async () => await handler.Handle(request, cts.Token);

        // 第 1 行落库后取消 → 第 2 行循环顶部抛取消 → 请求级 catch 整批回滚后重抛
        await act.Should().ThrowAsync<OperationCanceledException>();

        await using var fresh = CreateFreshPatientsContext();
        (await fresh.Patients.CountAsync())
            .Should().Be(0, "请求中途取消必须整体回滚——不允许半批患者落库");
    }

    // ---------------------------------------------------------------- 药材

    [Fact]
    public async Task Herbs_BatchImport_ReportsConsistentCounts_AndPersistsRows()
    {
        _catalogContext.Herbs.Add(Herb.Create("当归", "克", 10m, "DG"));
        await _catalogContext.SaveChangesAsync();

        var handler = new BatchImportHerbsCommandHandler(_herbRepository);
        var request = new BatchImportHerbsCommand(
            new List<HerbInputDto>
            {
                new() { Name = "白芍", Unit = "克", Price = 12m },
                new() { Name = "当归", Unit = "克", Price = 15m }, // 已存在 → Skip
            },
            DuplicateStrategy.Skip,
            Guid.NewGuid());

        var result = await handler.Handle(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value!;
        report.TotalCount.Should().Be(2);
        report.SuccessCount.Should().Be(1);
        report.SkippedCount.Should().Be(1);
        report.FailureCount.Should().Be(0);
        report.SuccessfulIds.Should().HaveCount(1);
        (report.SuccessCount + report.SkippedCount + report.FailureCount).Should().Be(report.TotalCount);

        await using var fresh = CreateFreshCatalogContext();
        (await fresh.Herbs.CountAsync(h => h.Name == "白芍")).Should().Be(1);
    }

    [Fact]
    public async Task Herbs_BatchImport_CancelledMidRequest_RollsBackWholeRequest()
    {
        using var cts = new CancellationTokenSource();
        _cancelInterceptor.Enabled = true;
        _cancelInterceptor.Source = cts;

        var handler = new BatchImportHerbsCommandHandler(_herbRepository);
        var request = new BatchImportHerbsCommand(
            new List<HerbInputDto>
            {
                new() { Name = "黄芪", Unit = "克", Price = 8m },
                new() { Name = "党参", Unit = "克", Price = 9m },
            },
            DuplicateStrategy.Skip,
            Guid.NewGuid());

        var act = async () => await handler.Handle(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();

        await using var fresh = CreateFreshCatalogContext();
        (await fresh.Herbs.CountAsync(h => h.Name == "黄芪" || h.Name == "党参"))
            .Should().Be(0, "请求中途取消必须整体回滚——不允许半批药材落库");
    }

    // ---------------------------------------------------------------- 验方

    [Fact]
    public async Task Formulas_BatchImport_ReportsConsistentCounts_AndPersistsRows()
    {
        _catalogContext.Herbs.Add(Herb.Create("人参", "克", 30m, "RS"));
        await _catalogContext.SaveChangesAsync();

        var handler = new BatchImportFormulasCommandHandler(
            _formulaRepository, _herbRepository, NullLogger<BatchImportFormulasCommandHandler>.Instance);
        var request = new BatchImportFormulasCommand(
            new List<FormulaImportItemDto>
            {
                new()
                {
                    Name = "四君子汤",
                    Effect = "益气健脾",
                    Usage = "水煎服",
                    Herbs = new List<FormulaHerbImportItemDto> { new() { HerbName = "人参", Dosage = 10, Unit = "克" } },
                },
                new()
                {
                    Name = "未知方",
                    Effect = "未知",
                    Usage = "水煎服",
                    Herbs = new List<FormulaHerbImportItemDto> { new() { HerbName = "不存在的药材", Dosage = 5, Unit = "克" } },
                },
            },
            FileName: "formulas.json",
            Strategy: DuplicateStrategy.Skip,
            CurrentUserId: Guid.NewGuid());

        var result = await handler.Handle(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value!;
        report.TotalCount.Should().Be(2);
        report.SuccessCount.Should().Be(2);
        report.SkippedCount.Should().Be(0);
        report.FailureCount.Should().Be(0);
        report.SuccessfulIds.Should().HaveCount(2);
        (report.SuccessCount + report.SkippedCount + report.FailureCount).Should().Be(report.TotalCount);
        report.MatchedHerbsCount.Should().Be(1);
        report.UnmatchedHerbsCount.Should().Be(1);

        await using var fresh = CreateFreshCatalogContext();
        (await fresh.Formulas.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Formulas_BatchImport_CancelledMidRequest_RollsBackWholeRequest()
    {
        using var cts = new CancellationTokenSource();
        _cancelInterceptor.Enabled = true;
        _cancelInterceptor.Source = cts;

        var handler = new BatchImportFormulasCommandHandler(
            _formulaRepository, _herbRepository, NullLogger<BatchImportFormulasCommandHandler>.Instance);
        var request = new BatchImportFormulasCommand(
            new List<FormulaImportItemDto>
            {
                NewFormula("导入方甲"),
                NewFormula("导入方乙"),
            },
            FileName: null,
            Strategy: DuplicateStrategy.Skip,
            CurrentUserId: Guid.NewGuid());

        var act = async () => await handler.Handle(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();

        await using var fresh = CreateFreshCatalogContext();
        (await fresh.Formulas.CountAsync())
            .Should().Be(0, "请求中途取消必须整体回滚——不允许半批验方落库");
    }

    private static FormulaImportItemDto NewFormula(string name) => new()
    {
        Name = name,
        Effect = "功效",
        Usage = "水煎服",
        Herbs = new List<FormulaHerbImportItemDto> { new() { HerbName = "甘草", Dosage = 3, Unit = "克" } },
    };

    private CatalogDbContext CreateFreshCatalogContext()
        => new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_catalogConnection).Options);

    private PatientsDbContext CreateFreshPatientsContext()
        => new(new DbContextOptionsBuilder<PatientsDbContext>().UseSqlite(_patientsConnection).Options);

    /// <summary>
    /// 第 1 次 SaveChanges 完成后取消令牌 —— 模拟「请求处理中途被取消」，
    /// 命中循环顶部的 ThrowIfCancellationRequested → 请求级 catch 整批回滚。
    /// </summary>
    private sealed class CancelAfterFirstSaveInterceptor : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }

        public CancellationTokenSource? Source { get; set; }

        private int _saveCount;

        public override ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled && Interlocked.Increment(ref _saveCount) == 1)
                Source!.Cancel();

            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }
    }
}
