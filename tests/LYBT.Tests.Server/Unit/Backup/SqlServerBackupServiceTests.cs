using System.Text;
using FluentAssertions;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services.Backup;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Contracts.Backup;
using LYBT.Tests.Server._Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LYBT.Tests.Server.Unit.Backup;

/// <summary>
/// B-06 备份/恢复引擎（<see cref="SqlServerBackupService"/>）真实集成测试——不使用任何 Mock：
/// 真实 LocalDB 数据库（每个测试类实例独占一个 Guid 库名，绝不触碰开发库 LYBTDesktop）
/// + 真实文件系统备份目录 + 真实 T-SQL 备份/恢复/表清单查询。
/// </summary>
/// <remarks>
/// <para>xUnit 每个测试方法构造一个新的测试类实例，故每个测试方法都拥有独立的数据库与备份目录，
/// 且 <see cref="DisposeAsync"/> 删除数据库与目录；程序集已禁用测试并行（见 AssemblyInfo.cs），
/// 因此类内测试不会并发争用同一个库。</para>
/// <para><b>本机环境事实（实测，影响测试参数）</b>：LocalDB 为 Express Edition，
/// <c>BACKUP DATABASE ... WITH COMPRESSION</c> 会直接报错（"BACKUP DATABASE WITH COMPRESSION is not supported
/// on Express Edition"），故所有创建请求显式 <c>Compress = false</c>（引擎在 false 时不生成 COMPRESSION 子句）。</para>
/// <para>断言对象一律是可观测行为：数据库内容、磁盘文件与返回的 DTO；不涉及源码文本或 Mock 回显。</para>
/// </remarks>
public sealed class SqlServerBackupServiceTests : IAsyncLifetime
{
    private const string LocalDbServer = @"(localdb)\MSSQLLocalDB";
    private const string TestPassword = "lybt-backup-test-password";

    /// <summary>加密容器魔数（<c>BackupFileEncryption</c> 头部前 8 字节，ASCII）</summary>
    private const string EncryptionMagic = "LYBTBKP1";

    private readonly string _databaseName = "LYBTZYZS_Backup_" + Guid.NewGuid().ToString("N");
    private readonly string _backupDirectory =
        Path.Combine(Path.GetTempPath(), "lybt_backup_tests_" + Guid.NewGuid().ToString("N"));

    /// <summary>被测引擎（以契约类型持有并在 <see cref="DisposeAsync"/> 释放其内部信号量）</summary>
    private IBackupService _service = null!;

    private string ConnectionString =>
        $"Server={LocalDbServer};Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        _service = CreateService();
    }

    public async Task DisposeAsync()
    {
        if (_service is IDisposable disposable)
            disposable.Dispose();

        try
        {
            SqlConnection.ClearAllPools();
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
        }
        catch (Exception)
        {
            // 清理失败不得掩盖测试断言结果
        }

        try
        {
            if (Directory.Exists(_backupDirectory))
                Directory.Delete(_backupDirectory, recursive: true);
        }
        catch (Exception)
        {
            // 同上：清理失败不得掩盖测试断言结果
        }
    }

    // ------------------------------------------------------------------
    // 1. 全量备份：产物、清单与稳定 Id
    // ------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_FullBackup_WritesBackupAndManifest_AndListAsyncReturnsStableId()
    {
        var result = await CreateBackupAsync(BackupKind.Full);

        result.Success.Should().BeTrue(result.Error);
        result.File.Should().NotBeNull();

        var file = result.File!;
        file.Kind.Should().Be(BackupKind.Full);
        file.IsEncrypted.Should().BeFalse();
        file.IsCompressed.Should().BeFalse();
        file.DatabaseName.Should().Be(_databaseName);
        file.FileName.Should().EndWith(".bak");

        var filePath = Path.Combine(_backupDirectory, file.FileName);
        File.Exists(filePath).Should().BeTrue("备份成功必须落盘");
        File.Exists(filePath + ".manifest.json").Should().BeTrue("每个备份文件必须有 .bak.manifest.json 侧车清单");
        new FileInfo(filePath).Length.Should().BeGreaterThan(0);
        file.SizeBytes.Should().Be(new FileInfo(filePath).Length);
        file.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(5));

        // 稳定 Id 回归守卫：旧实现每次列举都 Guid.NewGuid()，导致「选中项 → 恢复/删除」失配
        var firstListing = await _service.ListAsync();
        var secondListing = await _service.ListAsync();

        firstListing.Should().ContainSingle();
        firstListing[0].FileName.Should().Be(file.FileName);
        firstListing[0].Id.Should().Be(file.Id);
        secondListing[0].Id.Should().Be(firstListing[0].Id, "同一备份文件多次列举必须得到同一 Id");
    }

    // ------------------------------------------------------------------
    // 2. 加密备份：容器格式、明文删除、清单标记
    // ------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_EncryptedBackup_WritesEncryptedContainer_AndRemovesPlaintext()
    {
        var result = await CreateBackupAsync(BackupKind.Full, encrypt: true, password: TestPassword);

        result.Success.Should().BeTrue(result.Error);
        var file = result.File!;
        file.FileName.Should().EndWith(".bak.enc");
        file.IsEncrypted.Should().BeTrue();

        var encryptedPath = Path.Combine(_backupDirectory, file.FileName);
        File.Exists(encryptedPath).Should().BeTrue();

        var magic = new byte[Encoding.ASCII.GetByteCount(EncryptionMagic)];
        using (var stream = File.OpenRead(encryptedPath))
        {
            stream.ReadExactly(magic.AsSpan());
        }

        Encoding.ASCII.GetString(magic).Should().Be(EncryptionMagic, "加密容器必须以 LYBTBKP1 魔数开头");
        new FileInfo(encryptedPath).Length.Should().BeGreaterThan(Encoding.ASCII.GetByteCount(EncryptionMagic));

        var plaintextPath = encryptedPath[..^BackupFileEncryption.EncryptedExtension.Length];
        File.Exists(plaintextPath).Should().BeFalse("加密完成后明文 .bak 必须被删除");

        var listing = await _service.ListAsync();
        listing.Should().ContainSingle();
        listing[0].Id.Should().Be(file.Id);
        listing[0].IsEncrypted.Should().BeTrue();
        listing[0].FileName.Should().Be(file.FileName);
    }

    // ------------------------------------------------------------------
    // 3. 差异备份：基准引用 + 无全量基准时拒绝
    // ------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_Differential_ReferencesBaseFullBackup_AndFailsWithoutBase()
    {
        // 空目录（尚无任何全量备份）时创建差异备份必须失败并给出清晰原因
        var withoutBase = await CreateBackupAsync(BackupKind.Differential);

        withoutBase.Success.Should().BeFalse();
        withoutBase.Error.Should().NotBeNullOrWhiteSpace();
        withoutBase.Error.Should().Contain("全量", "错误信息需说明缺少全量基准");
        withoutBase.File.Should().BeNull();
        (await _service.ListAsync()).Should().BeEmpty("失败的差异备份不得留下任何产物");

        var full = await CreateBackupAsync(BackupKind.Full);
        full.Success.Should().BeTrue(full.Error);

        var differential = await CreateBackupAsync(BackupKind.Differential);

        differential.Success.Should().BeTrue(differential.Error);
        var diff = differential.File!;
        diff.Kind.Should().Be(BackupKind.Differential);
        diff.FileName.Should().EndWith("_diff.bak");
        diff.BaseFullBackupId.Should().Be(full.File!.Id);
        diff.BaseFullBackupFileName.Should().Be(full.File.FileName);
        File.Exists(Path.Combine(_backupDirectory, diff.FileName)).Should().BeTrue();

        var listing = await _service.ListAsync();
        listing.Should().HaveCount(2);
        listing.Should().OnlyContain(f => !f.IsChainBroken, "基准全量仍在，差异链完整");
        listing.Single(f => f.Kind == BackupKind.Differential).BaseFullBackupId.Should().Be(full.File.Id);
    }

    // ------------------------------------------------------------------
    // 4. 整库恢复：备份后的一切改动被回滚
    // ------------------------------------------------------------------

    /// <remarks>
    /// 整库恢复会以 <c>RESTORE DATABASE ... WITH REPLACE, RECOVERY</c> 覆盖当前库；
    /// SQL Server 要求执行 RESTORE 的会话不处于被还原数据库的上下文中
    /// （否则报 "RESTORE cannot process database 'x' because it is in use by this session"）。
    /// </remarks>
    [Fact]
    public async Task RestoreAsync_FullMode_RevertsMutations_AndRestoresOriginalRows()
    {
        var (_, tableName) = await TableOfAsync<Herb>();
        var keptHerb = await SeedHerbAsync("当归");

        var backup = await CreateBackupAsync(BackupKind.Full);
        backup.Success.Should().BeTrue(backup.Error);

        // 备份后制造两类差异：删除原有行（原始 SQL）、新增行
        await ExecuteSqlAsync($"DELETE FROM [dbo].[{tableName}] WHERE [Id] = '{keptHerb.Id}'");
        var herbAddedAfterBackup = await SeedHerbAsync("川芎");

        (await ReadHerbNameAsync(keptHerb.Id)).Should().BeNull();
        SqlConnection.ClearAllPools();

        var restore = await RestoreFullAsync(backup.File!.Id);

        restore.Success.Should().BeTrue(restore.Error);
        (await ReadHerbNameAsync(keptHerb.Id)).Should().Be("当归", "整库恢复应还原备份时存在的行");
        (await HerbExistsAsync(herbAddedAfterBackup.Id)).Should().BeFalse("备份之后新增的行应被整库恢复覆盖");
    }

    // ------------------------------------------------------------------
    // 5. 加密备份的恢复口令校验
    // ------------------------------------------------------------------

    /// <remarks>
    /// 口令正确性通过选择性恢复验证（恢复链第一步都是口令解密，代码路径相同）；
    /// 整库恢复的会话上下文问题见 <see cref="RestoreAsync_FullMode_RevertsMutations_AndRestoresOriginalRows"/>。
    /// </remarks>
    [Fact]
    public async Task RestoreAsync_EncryptedBackup_WrongPasswordFails_AndCorrectPasswordSucceeds()
    {
        var (_, tableName) = await TableOfAsync<Herb>();
        var herb = await SeedHerbAsync("黄芪");

        var backup = await CreateBackupAsync(BackupKind.Full, encrypt: true, password: TestPassword);
        backup.Success.Should().BeTrue(backup.Error);
        var backupId = backup.File!.Id;

        await ExecuteSqlAsync($"DELETE FROM [dbo].[{tableName}] WHERE [Id] = '{herb.Id}'");
        (await HerbExistsAsync(herb.Id)).Should().BeFalse();

        var wrongPassword = await _service.RestoreAsync(new RestoreRequestDto
        {
            BackupId = backupId,
            Mode = RestoreMode.Selective,
            CreatePreRestoreBackup = false,
            Password = "not-the-password",
            Tables = [new SelectiveRestoreTableDto { TableName = tableName, Ids = [herb.Id] }]
        });

        wrongPassword.Success.Should().BeFalse();
        wrongPassword.Error.Should().NotBeNullOrWhiteSpace();
        (await HerbExistsAsync(herb.Id)).Should().BeFalse("口令错误时不得改动当前库");

        var correctPassword = await _service.RestoreAsync(new RestoreRequestDto
        {
            BackupId = backupId,
            Mode = RestoreMode.Selective,
            CreatePreRestoreBackup = false,
            Password = TestPassword,
            Tables = [new SelectiveRestoreTableDto { TableName = tableName, Ids = [herb.Id] }]
        });

        correctPassword.Success.Should().BeTrue(correctPassword.Error);
        (await HerbExistsAsync(herb.Id)).Should().BeTrue("口令正确时备份中的行应被恢复");
    }

    // ------------------------------------------------------------------
    // 6. 删除：文件 + 清单，差异备份的基准全量受保护
    // ------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_RemovesFileAndManifest_AndRefusesDeletingDifferentialBase()
    {
        var full = await CreateBackupAsync(BackupKind.Full);
        var differential = await CreateBackupAsync(BackupKind.Differential);
        full.Success.Should().BeTrue(full.Error);
        differential.Success.Should().BeTrue(differential.Error);

        var fullPath = Path.Combine(_backupDirectory, full.File!.FileName);
        var diffPath = Path.Combine(_backupDirectory, differential.File!.FileName);

        var refused = await _service.DeleteAsync(full.File.Id);

        refused.Success.Should().BeFalse();
        refused.Error.Should().Contain(differential.File.FileName, "拒绝原因需指明依赖它的差异备份");
        File.Exists(fullPath).Should().BeTrue("被差异备份引用的基准全量不得被删除");
        File.Exists(fullPath + ".manifest.json").Should().BeTrue();

        var deleteDifferential = await _service.DeleteAsync(differential.File.Id);

        deleteDifferential.Success.Should().BeTrue(deleteDifferential.Error);
        File.Exists(diffPath).Should().BeFalse();
        File.Exists(diffPath + ".manifest.json").Should().BeFalse();

        var deleteFull = await _service.DeleteAsync(full.File.Id);

        deleteFull.Success.Should().BeTrue(deleteFull.Error);
        File.Exists(fullPath).Should().BeFalse();
        File.Exists(fullPath + ".manifest.json").Should().BeFalse();
        (await _service.ListAsync()).Should().BeEmpty();

        var missing = await _service.DeleteAsync(full.File.Id);
        missing.Success.Should().BeFalse();
    }

    // ------------------------------------------------------------------
    // 7. 表清单（选择性恢复的候选表）
    // ------------------------------------------------------------------

    [Fact]
    public async Task ListTablesAsync_ReportsCreatedSchemaTables_WithRecordSelectionSupport()
    {
        var (_, tableName) = await TableOfAsync<Herb>();
        await SeedHerbAsync("白芍");

        var tables = await _service.ListTablesAsync();

        tables.Should().NotBeEmpty();
        tables.Should().Contain(t => t.TableName == tableName);

        var herbTable = tables.Single(t => t.TableName == tableName);
        herbTable.RowCount.Should().Be(1, "Herbs 中恰好插入 1 行");
        herbTable.SupportsRecordSelection.Should().BeTrue("Herbs 具有 Id 列，应支持记录级选择性恢复");

        // 无 Id 列的表（Identity 用户角色关联表，复合主键）不得被标记为支持记录级恢复
        var joinTable = await TableWithoutIdColumnAsync();
        tables.Should().Contain(t => t.TableName == joinTable);
        tables.Single(t => t.TableName == joinTable).SupportsRecordSelection.Should().BeFalse();
    }

    // ------------------------------------------------------------------
    // 8. 备份状态
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetStatusAsync_ReflectsBackupState()
    {
        var beforeAnyBackup = await _service.GetStatusAsync();

        beforeAnyBackup.FileCount.Should().Be(0);
        beforeAnyBackup.TotalSizeBytes.Should().Be(0);
        beforeAnyBackup.LastBackupAt.Should().BeNull();
        beforeAnyBackup.DatabaseName.Should().Be(_databaseName);
        beforeAnyBackup.BackupDirectory.Should().Be(_backupDirectory);
        beforeAnyBackup.RetentionDays.Should().Be(7);
        beforeAnyBackup.IsOperationRunning.Should().BeFalse();

        var backup = await CreateBackupAsync(BackupKind.Full);
        backup.Success.Should().BeTrue(backup.Error);

        var afterBackup = await _service.GetStatusAsync();

        afterBackup.FileCount.Should().Be(1);
        afterBackup.TotalSizeBytes.Should().Be(backup.File!.SizeBytes);
        afterBackup.LastBackupAt.Should().NotBeNull();
        afterBackup.LastBackupAt!.Value.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(5));
        afterBackup.DatabaseName.Should().Be(_databaseName);
        afterBackup.BackupDirectory.Should().Be(_backupDirectory);
        afterBackup.RetentionDays.Should().Be(7);
        afterBackup.IsOperationRunning.Should().BeFalse();
    }

    // ------------------------------------------------------------------
    // 9. 选择性恢复：仅回写指定记录
    // ------------------------------------------------------------------

    [Fact]
    public async Task RestoreAsync_SelectiveMode_RestoresDeletedRowWithoutWipingNewerRows()
    {
        var (_, tableName) = await TableOfAsync<Herb>();
        var restoredHerb = await SeedHerbAsync("甘草");
        var untouchedHerb = await SeedHerbAsync("茯苓");

        var backup = await CreateBackupAsync(BackupKind.Full);
        backup.Success.Should().BeTrue(backup.Error);

        await ExecuteSqlAsync($"DELETE FROM [dbo].[{tableName}] WHERE [Id] = '{restoredHerb.Id}'");
        var herbAddedAfterBackup = await SeedHerbAsync("陈皮");
        (await HerbExistsAsync(restoredHerb.Id)).Should().BeFalse();

        var restore = await _service.RestoreAsync(new RestoreRequestDto
        {
            BackupId = backup.File!.Id,
            Mode = RestoreMode.Selective,
            CreatePreRestoreBackup = false,
            Tables = [new SelectiveRestoreTableDto { TableName = tableName, Ids = [restoredHerb.Id] }]
        });

        restore.Success.Should().BeTrue(restore.Error);
        restore.AffectedCount.Should().Be(1, "仅回写 1 张表");
        (await ReadHerbNameAsync(restoredHerb.Id)).Should().Be("甘草", "被删除的记录应按 Id 回写");
        (await HerbExistsAsync(untouchedHerb.Id)).Should().BeTrue("未被指定的记录不应受影响");
        (await HerbExistsAsync(herbAddedAfterBackup.Id)).Should().BeTrue("选择性恢复不得覆盖备份之后新增的记录");
    }

    // ------------------------------------------------------------------
    // 10. 自动备份：无备份时执行，间隔内跳过
    // ------------------------------------------------------------------

    [Fact]
    public async Task AutoBackupAsync_CreatesBackupWhenNoneExists_ThenSkipsWithinInterval()
    {
        var first = await _service.AutoBackupAsync();

        first.Success.Should().BeTrue(first.Error);
        first.AffectedCount.Should().Be(1, "尚无任何备份时应真正执行一次备份");
        first.File.Should().NotBeNull();
        var filePath = Path.Combine(_backupDirectory, first.File!.FileName);
        File.Exists(filePath).Should().BeTrue();
        var writtenAt = File.GetLastWriteTimeUtc(filePath);

        await Task.Delay(TimeSpan.FromSeconds(1));
        var second = await _service.AutoBackupAsync();

        second.Success.Should().BeTrue(second.Error);
        second.AffectedCount.Should().Be(0, "距上次备份未满间隔时不执行备份");

        var listing = await _service.ListAsync();
        listing.Should().ContainSingle("未到间隔的自动备份不得产生新文件");
        listing[0].Id.Should().Be(first.File.Id);
        File.GetLastWriteTimeUtc(filePath).Should().Be(writtenAt, "跳过时不得触碰已有备份文件");
    }

    // ------------------------------------------------------------------
    // 测试基础设施
    // ------------------------------------------------------------------

    /// <summary>创建指向本测试独占数据库的上下文（即用即弃，避免长生命周期 DbContext 持有连接）</summary>
    private AppDbContext CreateContext()
        => new(TestDbFactory.CreateOptions<AppDbContext>(ConnectionString));

    private SqlServerBackupService CreateService()
    {
        var options = new BackupOptions
        {
            Directory = _backupDirectory,
            RetentionDays = 7,
            // LocalDB（Express Edition）不支持 BACKUP ... WITH COMPRESSION（见类注释）
            CompressByDefault = false,
            AutoBackup = new AutoBackupOptions
            {
                Enabled = false,
                IntervalHours = 24,
                Kind = BackupKind.Full
            }
        };

        return new SqlServerBackupService(
            Options.Create(options),
            Options.Create(new DatabaseOptions { ConnectionString = ConnectionString }),
            new ConfigurationBuilder().Build(),
            new BackupJobTracker(),
            NullLogger<SqlServerBackupService>.Instance);
    }

    private Task<BackupOperationResultDto> CreateBackupAsync(BackupKind kind, bool encrypt = false, string? password = null)
        => _service.CreateAsync(new BackupCreateRequestDto
        {
            Kind = kind,
            Compress = false,
            Encrypt = encrypt,
            Password = password
        });

    private Task<BackupOperationResultDto> RestoreFullAsync(string backupId)
        => _service.RestoreAsync(new RestoreRequestDto
        {
            BackupId = backupId,
            Mode = RestoreMode.Full,
            CreatePreRestoreBackup = false
        });

    /// <summary>解析 EF 模型中的实际表名/架构（不臆测表名）</summary>
    private async Task<(string Schema, string Name)> TableOfAsync<TEntity>() where TEntity : class
    {
        await using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} 未映射到数据库表");

        return (entityType.GetSchema() ?? "dbo",
            entityType.GetTableName() ?? throw new InvalidOperationException($"{typeof(TEntity).Name} 缺少表名映射"));
    }

    /// <summary>解析 EF 模型中不具 <c>Id</c> 列的表（Identity 用户角色关联表，复合主键）</summary>
    private async Task<string> TableWithoutIdColumnAsync()
    {
        await using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(IdentityUserRole<Guid>))
            ?? throw new InvalidOperationException("Identity 用户角色关联实体未映射到数据库表");

        return entityType.GetTableName()
            ?? throw new InvalidOperationException("Identity 用户角色关联实体缺少表名映射");
    }

    private async Task<Herb> SeedHerbAsync(string name)
    {
        await using var context = CreateContext();
        var herb = Herb.Create(name, "g", 12.5m, category: "集成测试");
        context.Herbs.Add(herb);
        await context.SaveChangesAsync();
        return herb;
    }

    private async Task<bool> HerbExistsAsync(Guid id)
    {
        await using var context = CreateContext();
        return await context.Herbs.AsNoTracking().AnyAsync(h => h.Id == id);
    }

    private async Task<string?> ReadHerbNameAsync(Guid id)
    {
        await using var context = CreateContext();
        return await context.Herbs.AsNoTracking()
            .Where(h => h.Id == id)
            .Select(h => h.Name)
            .FirstOrDefaultAsync();
    }

    private async Task ExecuteSqlAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync();
    }
}
