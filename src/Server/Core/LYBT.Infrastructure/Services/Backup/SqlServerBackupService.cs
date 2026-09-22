using System.Data;
using System.Globalization;
using System.Text;
using LYBT.Shared.Configuration;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Contracts.Backup;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Infrastructure.Services.Backup;

/// <summary>
/// SQL Server 备份/恢复引擎（B-06 / US-SHELL-013 / NFR-AVAIL-001）。
/// </summary>
/// <remarks>
/// <para><b>备份</b>：T-SQL <c>BACKUP DATABASE</c>（全量 / <c>WITH DIFFERENTIAL</c> 差异；可选 <c>WITH COMPRESSION</c>），
/// 文件级可选 AES-256 加密（<see cref="BackupFileEncryption"/>）。</para>
/// <para><b>恢复</b>：整库 <c>RESTORE DATABASE ... WITH REPLACE</c>（差异备份自动先还原基准全量，<c>NORECOVERY</c> → 差异 <c>RECOVERY</c>）；
/// 选择性恢复先还原到临时库，再按表/记录回写当前库（含外键约束临时禁用）。</para>
/// <para><b>进度</b>：备份/恢复期间轮询 <c>sys.dm_exec_requests.percent_complete</c>，经 <see cref="BackupJobTracker"/> 暴露给 UI。</para>
/// <para><b>串行化</b>：进程内 <see cref="SemaphoreSlim"/> 保证同一时刻仅一个备份/恢复/清理作业。</para>
/// </remarks>
public sealed class SqlServerBackupService : IBackupService, IDisposable
{
    private const int ProgressPollIntervalMs = 500;
    private const int CommandTimeoutSeconds = 0; // 备份/恢复不限时（大库可能远超默认 30s）
    private const int MaxIdsPerBatch = 500;
    private const string RestoreTempSuffix = "_LYBT_SELRESTORE";

    private readonly IOptions<BackupOptions> _options;
    private readonly IOptions<DatabaseOptions> _databaseOptions;
    private readonly IConfiguration _configuration;
    private readonly BackupJobTracker _tracker;
    private readonly ILogger<SqlServerBackupService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool? _supportsBackupCompression;

    public SqlServerBackupService(
        IOptions<BackupOptions> options,
        IOptions<DatabaseOptions> databaseOptions,
        IConfiguration configuration,
        BackupJobTracker tracker,
        ILogger<SqlServerBackupService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _databaseOptions = databaseOptions ?? throw new ArgumentNullException(nameof(databaseOptions));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private string ConnectionString =>
        ConnectionStringResolver.GetEffectiveConnectionString(_databaseOptions.Value, _configuration);

    /// <summary>
    /// master 库连接串——整库恢复（ALTER SINGLE_USER + RESTORE DATABASE）必须在 master 上下文执行，
    /// 否则目标库被当前会话占用导致 RESTORE 被拒绝。
    /// </summary>
    private string MasterConnectionString
    {
        get
        {
            var builder = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" };
            return builder.ConnectionString;
        }
    }

    private string DatabaseName
    {
        get
        {
            var name = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("连接字符串未指定数据库名（Initial Catalog），无法执行备份/恢复");

            return name;
        }
    }

    private string BackupDirectory
    {
        get
        {
            var directory = _options.Value.Directory;
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("未配置备份目录（Backup:Directory）");

            return directory;
        }
    }

    /// <inheritdoc />
    public async Task<BackupOperationResultDto> CreateAsync(BackupCreateRequestDto request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _gate.WaitAsync(ct);
        try
        {
            return await CreateCoreAsync(request.Kind, request.Compress, request.Encrypt, request.Password, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<BackupOperationResultDto> AutoBackupAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var options = _options.Value;
            var entries = ReadEntries();
            var last = entries.Count > 0 ? entries.Max(e => e.CreatedAt) : (DateTime?)null;

            if (last.HasValue && DateTime.Now - last.Value < TimeSpan.FromHours(options.AutoBackup.IntervalHours))
            {
                return new BackupOperationResultDto
                {
                    Success = true,
                    AffectedCount = 0,
                    Message = $"距上次备份未满 {options.AutoBackup.IntervalHours} 小时，跳过自动备份"
                };
            }

            var kind = options.AutoBackup.Kind == BackupKind.Differential ? BackupKind.Differential : BackupKind.Full;
            var result = await CreateCoreAsync(
                kind,
                options.CompressByDefault,
                options.AutoBackup.Encrypt,
                options.EncryptionPassword,
                ct);

            if (!result.Success)
                return result;

            var removed = await CleanupCoreAsync(ct);
            result.AffectedCount = 1;
            result.Message = removed > 0
                ? $"{result.Message}；已清理过期备份 {removed} 个"
                : result.Message;
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<BackupFileDto>> ListAsync(CancellationToken ct = default)
    {
        var entries = ReadEntries();
        var fileNames = entries.Select(e => e.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<BackupFileDto> result = entries
            .Select(e => e.ToDto(
                e.Kind == BackupKind.Differential &&
                !string.IsNullOrEmpty(e.BaseFullBackupFileName) &&
                !fileNames.Contains(e.BaseFullBackupFileName!)))
            .ToList();

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<BackupStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var options = _options.Value;
        var entries = ReadEntries();
        var job = _tracker.Current;

        var status = new BackupStatusDto
        {
            LastBackupAt = entries.Count > 0 ? entries.Max(e => e.CreatedAt) : null,
            FileCount = entries.Count,
            TotalSizeBytes = entries.Sum(e => e.SizeBytes),
            BackupDirectory = BackupDirectory,
            RetentionDays = options.RetentionDays,
            DatabaseName = DatabaseName,
            IsOperationRunning = job?.IsRunning ?? false,
            OperationKind = job?.IsRunning == true ? job.Kind : null,
            PhaseMessage = job?.Phase,
            ProgressPercent = job?.Percent ?? 0,
            OperationStartedAt = job?.IsRunning == true ? job.StartedAt : null,
            LastError = job?.LastError,
            AutoBackupEnabled = options.AutoBackup.Enabled,
            AutoBackupIntervalHours = options.AutoBackup.IntervalHours
        };

        return Task.FromResult(status);
    }

    /// <inheritdoc />
    public async Task<BackupOperationResultDto> RestoreAsync(RestoreRequestDto request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _gate.WaitAsync(ct);
        try
        {
            return await RestoreCoreAsync(request, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<BackupOperationResultDto> DeleteAsync(string backupId, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var entries = ReadEntries();
            var target = entries.FirstOrDefault(e => e.Id == backupId);
            if (target == null)
                return Failure($"未找到指定的备份（Id={backupId}）");

            var dependents = entries
                .Where(e => e.Kind == BackupKind.Differential &&
                            string.Equals(e.BaseFullBackupFileName, target.FileName, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.FileName)
                .ToList();

            if (dependents.Count > 0)
                return Failure($"该全量备份是差异备份 {string.Join("、", dependents)} 的基准，请先删除差异备份");

            var path = Path.Combine(BackupDirectory, target.FileName);
            if (File.Exists(path))
                File.Delete(path);
            BackupManifestStore.Delete(path);

            _logger.LogInformation("[BACKUP] 已删除备份 {File}", target.FileName);
            return new BackupOperationResultDto
            {
                Success = true,
                AffectedCount = 1,
                Message = $"已删除备份 {target.FileName}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP] 删除备份失败 Id={Id}", backupId);
            return Failure(ex.Message);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<BackupOperationResultDto> CleanupAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            _tracker.Start("清理", "清理过期备份");
            var removed = await CleanupCoreAsync(ct);
            _tracker.Complete($"清理完成（{removed} 个）");

            return new BackupOperationResultDto
            {
                Success = true,
                AffectedCount = removed,
                Message = removed > 0 ? $"已清理过期备份 {removed} 个" : "没有需要清理的过期备份"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP] 清理过期备份失败");
            _tracker.Fail(ex.Message);
            return Failure(ex.Message);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BackupTableDto>> ListTablesAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT t.name AS TableName,
                   COALESCE(SUM(CASE WHEN p.index_id IN (0, 1) THEN p.rows ELSE 0 END), 0) AS [RowCount],
                   CASE WHEN EXISTS (SELECT 1 FROM sys.columns c WHERE c.object_id = t.object_id AND c.name = N'Id') THEN 1 ELSE 0 END AS HasId
            FROM sys.tables t
            LEFT JOIN sys.partitions p ON p.object_id = t.object_id
            WHERE t.is_ms_shipped = 0
            GROUP BY t.name, t.object_id
            ORDER BY t.name
            """;

        var tables = new List<BackupTableDto>();
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            tables.Add(new BackupTableDto
            {
                TableName = reader.GetString(0),
                RowCount = Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture),
                SupportsRecordSelection = Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture) == 1
            });
        }

        return tables;
    }

    // ------------------------------------------------------------------
    // 备份
    // ------------------------------------------------------------------

    private async Task<BackupOperationResultDto> CreateCoreAsync(
        BackupKind kind,
        bool compress,
        bool encrypt,
        string? password,
        CancellationToken ct)
    {
        var effectiveKind = kind == BackupKind.Differential ? BackupKind.Differential : kind;
        var sqlKind = effectiveKind == BackupKind.Differential ? BackupKind.Differential : BackupKind.Full;
        string? rawBackupPath = null;
        string? createdFile = null;
        string? createdManifestTarget = null;

        _tracker.Start("备份", "准备备份");
        try
        {
            var directory = BackupDirectory;
            Directory.CreateDirectory(directory);

            var entries = ReadEntries();
            BackupManifestEntry? baseFull = null;
            if (sqlKind == BackupKind.Differential)
            {
                baseFull = entries.FirstOrDefault(e => e.Kind != BackupKind.Differential);
                if (baseFull == null)
                    return FailureWithTracker("尚无全量备份，无法创建差异备份；请先执行一次全量备份");
            }

            var resolvedPassword = ResolvePassword(encrypt, password);
            if (encrypt && string.IsNullOrEmpty(resolvedPassword))
                return FailureWithTracker("未提供备份口令，且未配置 Backup:EncryptionPassword");

            var stamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var suffix = effectiveKind switch
            {
                BackupKind.Differential => "_diff",
                BackupKind.PreRestore => "_prerestore",
                _ => string.Empty
            };
            var fileName = $"LYBTDB_{stamp}{suffix}.bak";
            var fullPath = Path.Combine(directory, fileName);
            rawBackupPath = fullPath;

            // 备份压缩能力探测：Express/LocalDB 实例不支持 WITH COMPRESSION（请求压缩时降级为未压缩并提示）
            var useCompression = compress && await SupportsBackupCompressionAsync(ct);
            string? compressionWarning = null;
            if (compress && !useCompression)
            {
                compressionWarning = "当前数据库实例不支持备份压缩（Express/LocalDB），已按未压缩执行";
                _logger.LogWarning("[BACKUP] {Warning}（数据库 {Database}）", compressionWarning, DatabaseName);
            }

            _tracker.Report("备份数据库", 0);
            var sql = BuildBackupSql(sqlKind, fullPath, useCompression);
            await ExecuteWithProgressAsync(sql, "备份数据库", ct);

            if (encrypt)
            {
                _tracker.Report("加密备份文件", 95);
                var encryptedPath = fullPath + BackupFileEncryption.EncryptedExtension;
                await Task.Run(() => BackupFileEncryption.Encrypt(fullPath, encryptedPath, resolvedPassword!), ct);
                File.Delete(fullPath);
                fullPath = encryptedPath;
                fileName += BackupFileEncryption.EncryptedExtension;
            }

            var info = new FileInfo(fullPath);
            var entry = new BackupManifestEntry
            {
                Id = Guid.NewGuid().ToString(),
                FileName = fileName,
                Kind = effectiveKind,
                CreatedAt = DateTime.Now,
                SizeBytes = info.Length,
                IsCompressed = useCompression,
                IsEncrypted = encrypt,
                DatabaseName = DatabaseName,
                BaseFullBackupId = baseFull?.Id,
                BaseFullBackupFileName = baseFull?.FileName
            };
            BackupManifestStore.Write(fullPath, entry);
            createdFile = fullPath;
            createdManifestTarget = fullPath;

            _tracker.Complete($"备份完成（{FormatSize(entry.SizeBytes)}）");
            _logger.LogInformation("[BACKUP] 备份完成 {File}（{Kind}，{Size} 字节，压缩={Compressed}，加密={Encrypted}）",
                fileName, effectiveKind, entry.SizeBytes, useCompression, encrypt);

            return new BackupOperationResultDto
            {
                Success = true,
                Warning = compressionWarning,
                File = entry.ToDto(),
                AffectedCount = 1,
                Message = compressionWarning == null
                    ? $"备份成功：{fileName}"
                    : $"备份成功：{fileName}（{compressionWarning}）"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP] 备份失败");
            _tracker.Fail(ex.Message);
            // 失败产物（明文 .bak 与加密半成品）不保留
            TryDeleteFile(rawBackupPath);
            TryDeleteFile(createdFile);
            if (createdManifestTarget != null)
                TryDeleteManifest(createdManifestTarget);
            return Failure(ex.Message);
        }
    }

    private string BuildBackupSql(BackupKind kind, string fullPath, bool compress)
    {
        var clauses = new List<string>();
        if (kind == BackupKind.Differential)
            clauses.Add("DIFFERENTIAL");

        clauses.Add("INIT");
        clauses.Add("FORMAT");
        clauses.Add($"NAME = N'LYBT {kind} Backup'");
        if (compress)
            clauses.Add("COMPRESSION");

        var path = EscapeSqlLiteral(fullPath);
        return $"BACKUP DATABASE [{EscapeIdentifier(DatabaseName)}] TO DISK = N'{path}' WITH {string.Join(", ", clauses)}";
    }

    // ------------------------------------------------------------------
    // 恢复
    // ------------------------------------------------------------------

    private async Task<BackupOperationResultDto> RestoreCoreAsync(RestoreRequestDto request, CancellationToken ct)
    {
        var tempFiles = new List<string>();
        _tracker.Start("恢复", "准备恢复");
        try
        {
            var options = _options.Value;
            var entries = ReadEntries();
            var target = entries.FirstOrDefault(e => e.Id == request.BackupId);
            if (target == null)
                return FailureWithTracker($"未找到指定的备份（Id={request.BackupId}）");

            var chain = BuildRestoreChain(entries, target);
            if (chain == null)
                return FailureWithTracker($"备份 {target.FileName} 的基准全量备份缺失，无法恢复");

            var password = ResolvePassword(chain.Any(e => e.IsEncrypted), request.Password);
            if (chain.Any(e => e.IsEncrypted) && string.IsNullOrEmpty(password))
                return FailureWithTracker("该备份已加密，请提供备份口令");

            // 1. 解密（如需）到临时文件
            var diskPaths = new List<string>();
            foreach (var entry in chain)
            {
                var path = Path.Combine(BackupDirectory, entry.FileName);
                if (!File.Exists(path))
                    return FailureWithTracker($"备份文件不存在：{entry.FileName}");

                if (!entry.IsEncrypted)
                {
                    diskPaths.Add(path);
                    continue;
                }

                _tracker.Report("解密备份文件", 3);
                var decrypted = Path.Combine(Path.GetTempPath(), $"lybt_restore_{Guid.NewGuid():N}.bak");
                await Task.Run(() => BackupFileEncryption.Decrypt(path, decrypted, password!), ct);
                tempFiles.Add(decrypted);
                diskPaths.Add(decrypted);
            }

            // 2. 恢复前自动保护当前数据（尽力而为，不阻断恢复）
            string? warning = null;
            if (request.CreatePreRestoreBackup)
            {
                _tracker.Report("恢复前自动备份当前数据", 5);
                var pre = await CreatePreRestoreBackupAsync(options, ct);
                if (!pre.Success)
                {
                    warning = $"恢复前自动备份失败（{pre.Error}），本次恢复未包含保护性备份";
                    _logger.LogWarning("[BACKUP] 恢复前自动备份失败: {Error}", pre.Error);
                }
            }

            // 3. 执行恢复
            if (request.Mode == RestoreMode.Selective)
            {
                var selectiveResult = await RestoreSelectiveCoreAsync(diskPaths, request.Tables, tempFiles, ct);
                if (!selectiveResult.Success)
                    return selectiveResult;
                selectiveResult.Warning = warning;
                return selectiveResult;
            }

            await RestoreFullCoreAsync(diskPaths, ct);

            _tracker.Complete("恢复完成，请重启应用");
            _logger.LogInformation("[BACKUP] 恢复完成：{File}", target.FileName);
            return new BackupOperationResultDto
            {
                Success = true,
                Warning = warning,
                Message = $"恢复完成：{target.FileName}，请重启应用"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP] 恢复失败");
            _tracker.Fail(ex.Message);
            return Failure(ex.Message);
        }
        finally
        {
            foreach (var file in tempFiles)
                TryDeleteFile(file);
        }
    }

    private async Task<BackupOperationResultDto> CreatePreRestoreBackupAsync(BackupOptions options, CancellationToken ct)
    {
        var password = string.IsNullOrWhiteSpace(options.EncryptionPassword) ? null : options.EncryptionPassword;
        var encrypt = options.EncryptByDefault && password != null;

        // 保护性备份不改变作业跟踪器（当前作业仍为「恢复」）——临时保存并还原
        var snapshot = _tracker.Current;
        var result = await CreateCoreAsync(BackupKind.PreRestore, options.CompressByDefault, encrypt, password, ct);

        if (snapshot is { IsRunning: true })
        {
            _tracker.Start(snapshot.Kind, snapshot.Phase);
            _tracker.Report(snapshot.Phase, snapshot.Percent);
        }

        return result;
    }

    private async Task RestoreFullCoreAsync(IReadOnlyList<string> diskPaths, CancellationToken ct)
    {
        var database = DatabaseName;
        var statements = new List<string>
        {
            $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE"
        };

        if (diskPaths.Count == 1)
        {
            statements.Add($"RESTORE DATABASE [{database}] FROM DISK = N'{EscapeSqlLiteral(diskPaths[0])}' WITH REPLACE, RECOVERY");
        }
        else
        {
            // 差异链：基准全量 NORECOVERY → 差异 RECOVERY
            for (var i = 0; i < diskPaths.Count; i++)
            {
                var recovery = i == diskPaths.Count - 1 ? "RECOVERY" : "NORECOVERY";
                var replace = i == 0 ? "REPLACE, " : string.Empty;
                statements.Add($"RESTORE DATABASE [{database}] FROM DISK = N'{EscapeSqlLiteral(diskPaths[i])}' WITH {replace}{recovery}");
            }
        }

        statements.Add($"ALTER DATABASE [{database}] SET MULTI_USER");

        _tracker.Report("恢复数据库", 10);
        try
        {
            // 必须在 master 上下文执行：RESTORE 的目标库不能正被当前会话占用
            // （否则报 "RESTORE cannot process database ... because it is in use by this session"）
            await ExecuteWithProgressAsync(string.Join(";\n", statements), "恢复数据库", ct, useMasterDatabase: true);
        }
        catch
        {
            // 恢复中断时数据库可能停留在 SINGLE_USER/RESTORING——尽力恢复多用户可用性
            await TrySetMultiUserAsync(database, ct);
            throw;
        }
        finally
        {
            SqlConnection.ClearAllPools();
        }
    }

    private async Task<BackupOperationResultDto> RestoreSelectiveCoreAsync(
        IReadOnlyList<string> diskPaths,
        IReadOnlyList<SelectiveRestoreTableDto> tables,
        List<string> tempFiles,
        CancellationToken ct)
    {
        if (tables.Count == 0)
            return FailureWithTracker("选择性恢复未指定任何表");

        var database = DatabaseName;
        var tempDatabase = database + RestoreTempSuffix;

        // 1. 逻辑文件名（取基准全量，差异恢复沿用同一组文件）
        _tracker.Report("读取备份文件清单", 15);
        var logicalFiles = await ReadLogicalFileNamesAsync(diskPaths[0], ct);
        if (logicalFiles.Count == 0)
            return FailureWithTracker("无法读取备份文件的逻辑文件清单");

        var dataDirectory = await ReadServerPropertyAsync("InstanceDefaultDataPath", ct) ?? Path.GetTempPath();
        var logDirectory = await ReadServerPropertyAsync("InstanceDefaultLogPath", ct) ?? dataDirectory;

        var moves = new List<string>();
        var dataIndex = 0;
        var logIndex = 0;
        foreach (var (logicalName, fileType) in logicalFiles)
        {
            var isLog = fileType.Contains("LOG", StringComparison.OrdinalIgnoreCase);
            var extension = isLog ? ".ldf" : ".mdf";
            var directory = isLog ? logDirectory : dataDirectory;
            var fileName = isLog
                ? $"{tempDatabase}_{logIndex++}_log{extension}"
                : $"{tempDatabase}_{dataIndex++}{extension}";
            moves.Add($"MOVE N'{EscapeSqlLiteral(logicalName)}' TO N'{EscapeSqlLiteral(Path.Combine(directory, fileName))}'");
        }

        // 2. 还原到临时库
        var restoreStatements = new List<string>
        {
            $"IF DB_ID(N'{EscapeSqlLiteral(tempDatabase)}') IS NOT NULL BEGIN ALTER DATABASE [{tempDatabase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{tempDatabase}]; END"
        };
        for (var i = 0; i < diskPaths.Count; i++)
        {
            var recovery = i == diskPaths.Count - 1 ? "RECOVERY" : "NORECOVERY";
            restoreStatements.Add(
                $"RESTORE DATABASE [{tempDatabase}] FROM DISK = N'{EscapeSqlLiteral(diskPaths[i])}' WITH {string.Join(", ", moves)}, REPLACE, {recovery}");
        }

        _tracker.Report("还原到临时库", 25);
        await ExecuteWithProgressAsync(string.Join(";\n", restoreStatements), "还原到临时库", ct);

        try
        {
            // 3. 表/记录回写
            var copied = 0;
            var total = tables.Count;
            await SetAllConstraintsAsync(disable: true, ct);
            try
            {
                foreach (var table in tables)
                {
                    ct.ThrowIfCancellationRequested();
                    var tableName = (table.TableName ?? string.Empty).Trim();
                    if (tableName.Length == 0)
                        continue;

                    var exists = await TableExistsAsync(tableName, tempDatabase, ct);
                    if (!exists)
                        return FailureWithTracker($"备份中不存在表 {tableName}，已中止选择性恢复");

                    _tracker.Report($"回写表 {tableName}", 30 + (int)(65.0 * copied / total));
                    await CopyTableAsync(tableName, tempDatabase, table.Ids, ct);
                    copied++;
                }
            }
            finally
            {
                await SetAllConstraintsAsync(disable: false, ct);
            }

            _tracker.Complete($"选择性恢复完成（{copied} 张表）");
            _logger.LogInformation("[BACKUP] 选择性恢复完成：{Count} 张表（临时库 {Temp}）", copied, tempDatabase);

            return new BackupOperationResultDto
            {
                Success = true,
                AffectedCount = copied,
                Warning = "外键约束已重新启用但未校验（如需校验请执行 DBCC CHECKCONSTRAINTS），且恢复完成后建议重启应用",
                Message = $"选择性恢复完成：{copied} 张表，请重启应用"
            };
        }
        finally
        {
            await DropDatabaseIfExistsAsync(tempDatabase, ct);
            SqlConnection.ClearAllPools();
        }
    }

    private async Task CopyTableAsync(string tableName, string tempDatabase, List<Guid>? ids, CancellationToken ct)
    {
        var qualified = $"[dbo].[{EscapeIdentifier(tableName)}]";
        var tempQualified = $"[{tempDatabase}].[dbo].[{EscapeIdentifier(tableName)}]";

        var columns = await ReadCopyableColumnsAsync(tableName, ct);
        if (columns.Count == 0)
            return;

        var identityColumn = columns.FirstOrDefault(c => c.IsIdentity).Name;
        var columnList = string.Join(", ", columns.Select(c => $"[{EscapeIdentifier(c.Name)}]"));

        // 记录级过滤按批拆分（SQL Server 单条语句参数上限 2100）；无过滤时单批全表
        var idChunks = ids is { Count: > 0 }
            ? ids.Chunk(MaxIdsPerBatch).Select(chunk => (IReadOnlyList<Guid>?)chunk).ToList()
            : new List<IReadOnlyList<Guid>?> { null };

        var hasIdColumn = columns.Any(c => string.Equals(c.Name, "Id", StringComparison.OrdinalIgnoreCase));
        if (ids is { Count: > 0 } && !hasIdColumn)
            throw new InvalidOperationException($"表 {tableName} 无 Id 列，仅支持整表恢复");

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);
        try
        {
            foreach (var chunk in idChunks)
            {
                ct.ThrowIfCancellationRequested();

                var parameters = new List<SqlParameter>();
                var filter = string.Empty;
                if (chunk is { Count: > 0 })
                {
                    var idList = string.Join(", ", chunk.Select((_, index) => $"@id{index}"));
                    filter = $" WHERE [Id] IN ({idList})";
                    parameters.AddRange(chunk.Select((id, index) => new SqlParameter($"@id{index}", id)));
                }

                var builder = new StringBuilder();
                if (identityColumn != null)
                    builder.AppendLine($"SET IDENTITY_INSERT {qualified} ON;");
                builder.AppendLine($"DELETE FROM {qualified}{filter};");
                builder.AppendLine($"INSERT INTO {qualified} ({columnList}) SELECT {columnList} FROM {tempQualified}{filter};");
                if (identityColumn != null)
                    builder.AppendLine($"SET IDENTITY_INSERT {qualified} OFF;");

                await using var command = new SqlCommand(builder.ToString(), connection, transaction)
                {
                    CommandTimeout = CommandTimeoutSeconds
                };
                command.Parameters.AddRange(parameters.ToArray());
                await command.ExecuteNonQueryAsync(ct);
            }

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task SetAllConstraintsAsync(bool disable, CancellationToken ct)
    {
        var action = disable ? "NOCHECK CONSTRAINT ALL" : "WITH NOCHECK CHECK CONSTRAINT ALL";
        var sql = $"""
            DECLARE @sql nvarchar(max) = N'';
            SELECT @sql += N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N' {action};'
            FROM sys.tables t
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE t.is_ms_shipped = 0;
            EXEC sp_executesql @sql;
            """;

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = CommandTimeoutSeconds };
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<List<CopyColumn>> ReadCopyableColumnsAsync(string tableName, CancellationToken ct)
    {
        const string sql = """
            SELECT c.name, c.is_identity, c.is_computed, ty.name AS TypeName
            FROM sys.columns c
            JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            WHERE c.object_id = OBJECT_ID(@qualified)
            ORDER BY c.column_id
            """;

        var columns = new List<CopyColumn>();
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
        command.Parameters.Add(new SqlParameter("@qualified", $"[dbo].[{tableName}]"));
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var isComputed = reader.GetBoolean(2);
            var typeName = reader.GetString(3);
            if (isComputed ||
                string.Equals(typeName, "timestamp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(typeName, "rowversion", StringComparison.OrdinalIgnoreCase))
                continue;

            columns.Add(new CopyColumn(reader.GetString(0), reader.GetBoolean(1)));
        }

        return columns;
    }

    private async Task<bool> TableExistsAsync(string tableName, string database, CancellationToken ct)
    {
        var sql = $"SELECT CASE WHEN OBJECT_ID(N'[{EscapeIdentifier(database)}].[dbo].[{EscapeIdentifier(tableName)}]') IS NULL THEN 0 ELSE 1 END";
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
        var value = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(value, CultureInfo.InvariantCulture) == 1;
    }

    private async Task<List<(string LogicalName, string FileType)>> ReadLogicalFileNamesAsync(string backupPath, CancellationToken ct)
    {
        var sql = $"RESTORE FILELISTONLY FROM DISK = N'{EscapeSqlLiteral(backupPath)}'";
        var files = new List<(string, string)>();
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = CommandTimeoutSeconds };
        await using var reader = await command.ExecuteReaderAsync(ct);
        var logicalNameIndex = reader.GetOrdinal("LogicalName");
        var typeIndex = reader.GetOrdinal("Type");
        while (await reader.ReadAsync(ct))
        {
            files.Add((reader.GetString(logicalNameIndex), reader.GetString(typeIndex)));
        }

        return files;
    }

    /// <summary>
    /// 备份压缩能力探测（进程内缓存）：SQL Server Express / LocalDB 不支持
    /// <c>BACKUP DATABASE ... WITH COMPRESSION</c>，请求压缩时须降级为未压缩而非直接失败。
    /// </summary>
    private async Task<bool> SupportsBackupCompressionAsync(CancellationToken ct)
    {
        if (_supportsBackupCompression.HasValue)
            return _supportsBackupCompression.Value;

        var edition = await ReadServerPropertyAsync("Edition", ct) ?? string.Empty;
        var supported = !edition.Contains("Express", StringComparison.OrdinalIgnoreCase);
        _supportsBackupCompression = supported;
        return supported;
    }

    private async Task<string?> ReadServerPropertyAsync(string property, CancellationToken ct)
    {
        var sql = $"SELECT CAST(SERVERPROPERTY('{EscapeIdentifier(property)}') AS nvarchar(4000))";
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
        var value = await command.ExecuteScalarAsync(ct);
        return value == null || value == DBNull.Value ? null : (string)value;
    }

    private async Task DropDatabaseIfExistsAsync(string database, CancellationToken ct)
    {
        try
        {
            var sql = $"""
                IF DB_ID(N'{EscapeSqlLiteral(database)}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{database}];
                END
                """;
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync(ct);
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = CommandTimeoutSeconds };
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[BACKUP] 清理临时库失败：{Database}", database);
        }
    }

    private async Task TrySetMultiUserAsync(string database, CancellationToken ct)
    {
        try
        {
            // 恢复中断时目标库可能处于 RESTORING/SINGLE_USER——须从 master 连接重置
            await using var connection = new SqlConnection(MasterConnectionString);
            await connection.OpenAsync(ct);
            await using var command = new SqlCommand($"ALTER DATABASE [{database}] SET MULTI_USER", connection)
            {
                CommandTimeout = 60
            };
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[BACKUP] 恢复中断后重置多用户模式失败：{Database}", database);
        }
    }

    // ------------------------------------------------------------------
    // 清理
    // ------------------------------------------------------------------

    private async Task<int> CleanupCoreAsync(CancellationToken ct)
    {
        var directory = BackupDirectory;
        if (!Directory.Exists(directory))
            return 0;

        var options = _options.Value;
        var entries = ReadEntries();
        if (entries.Count == 0)
            return 0;

        var cutoff = DateTime.Now.AddDays(-options.RetentionDays);

        // 保护：最新一份全量备份 + 被差异备份引用的基准全量备份
        var protectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var newestFull = entries.FirstOrDefault(e => e.Kind != BackupKind.Differential);
        if (newestFull != null)
            protectedIds.Add(newestFull.Id);

        foreach (var entry in entries.Where(e => e.Kind == BackupKind.Differential))
        {
            if (!string.IsNullOrEmpty(entry.BaseFullBackupFileName))
            {
                var baseEntry = entries.FirstOrDefault(e =>
                    string.Equals(e.FileName, entry.BaseFullBackupFileName, StringComparison.OrdinalIgnoreCase));
                if (baseEntry != null)
                    protectedIds.Add(baseEntry.Id);
            }
        }

        var removed = 0;
        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();
            if (entry.CreatedAt >= cutoff || protectedIds.Contains(entry.Id))
                continue;

            try
            {
                var path = Path.Combine(directory, entry.FileName);
                if (File.Exists(path))
                    File.Delete(path);
                BackupManifestStore.Delete(path);
                removed++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[BACKUP] 清理过期备份失败：{File}", entry.FileName);
            }
        }

        if (removed > 0)
            _logger.LogInformation("[BACKUP] 已清理过期备份 {Count} 个", removed);

        return await Task.FromResult(removed);
    }

    // ------------------------------------------------------------------
    // 基础设施
    // ------------------------------------------------------------------

    private List<BackupManifestEntry> ReadEntries()
    {
        var directory = BackupDirectory;
        if (!Directory.Exists(directory))
            return new List<BackupManifestEntry>();

        var database = DatabaseName;
        return Directory
            .EnumerateFiles(directory, "*" + BackupManifestStore.BackupFileSuffixes[0] + "*")
            .Where(BackupManifestStore.IsBackupFile)
            .Select(path => BackupManifestStore.ResolveEntry(path, database))
            .OrderByDescending(e => e.CreatedAt)
            .ToList();
    }

    private BackupManifestEntry? FindEntry(string backupId)
        => ReadEntries().FirstOrDefault(e => e.Id == backupId);

    /// <summary>解析恢复链：整库/差异备份 → [基准全量] 或 [基准全量, 差异]</summary>
    private static List<BackupManifestEntry>? BuildRestoreChain(
        IReadOnlyList<BackupManifestEntry> entries,
        BackupManifestEntry target)
    {
        if (target.Kind != BackupKind.Differential)
            return new List<BackupManifestEntry> { target };

        var baseEntry = !string.IsNullOrEmpty(target.BaseFullBackupFileName)
            ? entries.FirstOrDefault(e => string.Equals(e.FileName, target.BaseFullBackupFileName, StringComparison.OrdinalIgnoreCase))
            : entries.FirstOrDefault(e => e.Id == target.BaseFullBackupId);

        if (baseEntry == null)
            return null;

        return new List<BackupManifestEntry> { baseEntry, target };
    }

    private string? ResolvePassword(bool encrypt, string? password)
    {
        if (!encrypt)
            return null;

        return string.IsNullOrWhiteSpace(password) ? _options.Value.EncryptionPassword : password;
    }

    private async Task ExecuteWithProgressAsync(string sql, string phase, CancellationToken ct, bool useMasterDatabase = false)
    {
        await using var connection = new SqlConnection(useMasterDatabase ? MasterConnectionString : ConnectionString);
        await connection.OpenAsync(ct);

        var sessionId = connection.ServerProcessId;
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = CommandTimeoutSeconds };
        var execution = command.ExecuteNonQueryAsync(ct);

        while (!execution.IsCompleted)
        {
            await Task.WhenAny(execution, Task.Delay(ProgressPollIntervalMs, ct));
            if (execution.IsCompleted)
                break;

            var percent = await TryQueryPercentAsync(sessionId, ct);
            if (percent.HasValue)
                _tracker.Report(phase, percent.Value);
        }

        await execution;
    }

    private async Task<int?> TryQueryPercentAsync(int sessionId, CancellationToken ct)
    {
        try
        {
            const string sql = """
                SELECT percent_complete
                FROM sys.dm_exec_requests
                WHERE session_id = @spid
                  AND command IN ('BACKUP DATABASE', 'RESTORE DATABASE', 'RESTORE VERIFYONLY')
                """;

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync(ct);
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
            command.Parameters.Add(new SqlParameter("@spid", sessionId));
            var value = await command.ExecuteScalarAsync(ct);
            if (value == null || value == DBNull.Value)
                return null;

            return (int)Math.Round(Convert.ToDouble(value, CultureInfo.InvariantCulture));
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException)
        {
            // DMV 不可用（权限/版本）时降级为无百分比进度，不影响备份本身
            return null;
        }
    }

    private BackupOperationResultDto FailureWithTracker(string error)
    {
        _tracker.Fail(error);
        return Failure(error);
    }

    private static BackupOperationResultDto Failure(string error) => new()
    {
        Success = false,
        Error = error,
        Message = error
    };

    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''");

    private static string EscapeIdentifier(string value) => value.Replace("]", "]]");

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
        >= 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B"
    };

    private static void TryDeleteFile(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // 忽略清理失败
        }
    }

    private static void TryDeleteManifest(string backupFilePath)
    {
        try
        {
            BackupManifestStore.Delete(backupFilePath);
        }
        catch (IOException)
        {
            // 忽略清理失败
        }
    }

    private readonly record struct CopyColumn(string Name, bool IsIdentity);
}
