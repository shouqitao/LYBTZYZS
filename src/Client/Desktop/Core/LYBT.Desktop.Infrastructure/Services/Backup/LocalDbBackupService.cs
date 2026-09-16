using System.Data;
using System.IO;
using Microsoft.Data.SqlClient;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services.Backup;

/// <summary>
/// 本地数据库备份服务实现（T7-1: NFR-AVAIL-001）。
/// T-SQL BACKUP DATABASE / RESTORE DATABASE；备份目录 %AppData%/LYBTZYZS/Backup/；
/// 保留 7 天；SemaphoreSlim 串行化手动/自动备份；恢复编排停止/重启内嵌 LocalWebAPI。
/// </summary>
public class LocalDbBackupService : ILocalDbBackupService, IDisposable
{
    // 本地模式固定连接串（与 EmbeddedLocalWebApiService 同款 LocalDB）
    private const string LocalConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=LYBTDesktop;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    private readonly IEmbeddedLocalWebApiService _embeddedWebApi;
    private readonly ILogger<LocalDbBackupService> _logger;
    private readonly SemaphoreSlim _backupLock = new(1, 1);

    public LocalDbBackupService(
        IEmbeddedLocalWebApiService embeddedWebApi,
        ILogger<LocalDbBackupService> logger)
    {
        _embeddedWebApi = embeddedWebApi ?? throw new ArgumentNullException(nameof(embeddedWebApi));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 既有安装的备份位于安装目录内（%LOCALAPPDATA%\LYBTZYZS\Backup），迁移到安装目录之外
        MigrateLegacyBackups();
    }

    /// <inheritdoc />
    public void Dispose() => _backupLock.Dispose();

    /// <summary>
    /// 备份目录：<c>%LOCALAPPDATA%\LYBT\Desktop\Backup</c>（**安装目录之外**——
    /// 经 Velopack 安装时安装根为 <c>%LOCALAPPDATA%\LYBTZYZS</c>，把备份放其中会在更新/卸载时丢失）。
    /// </summary>
    private static string BackupDirectoryPath
        => Path.Combine(
            SystemConstants.UserDataDirectory,
            SystemConstants.FilePaths.BackupDirectory);

    /// <summary>
    /// 遗留备份目录（<c>%LOCALAPPDATA%\LYBTZYZS\Backup</c>）——一次性迁移既有备份。
    /// </summary>
    private static string LegacyBackupDirectoryPath
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LYBTZYZS",
            SystemConstants.FilePaths.BackupDirectory);

    /// <summary>把遗留目录中的备份移到新目录（幂等；新目录已存在时不动）。</summary>
    private void MigrateLegacyBackups()
    {
        try
        {
            if (Directory.Exists(BackupDirectoryPath) || !Directory.Exists(LegacyBackupDirectoryPath))
                return;

            Directory.CreateDirectory(SystemConstants.UserDataDirectory);
            Directory.Move(LegacyBackupDirectoryPath, BackupDirectoryPath);
            _logger.LogInformation("[Backup] 备份目录已迁移出安装目录: {From} → {To}",
                LegacyBackupDirectoryPath, BackupDirectoryPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Backup] 备份目录迁移失败（保留原位置继续可用）");
        }
    }

    /// <summary>数据库名（从连接串解析，当前固定 LYBTDesktop）</summary>
    private static string DatabaseName => "LYBTDesktop";

    /// <inheritdoc />
    public async Task<BackupResult> BackupAsync(CancellationToken ct = default)
    {
        await _backupLock.WaitAsync(ct);
        try
        {
            var dir = BackupDirectoryPath;
            Directory.CreateDirectory(dir);

            var fileName = $"LYBTDB_{DateTime.Now:yyyyMMddHHmmss}{SystemConstants.FileExtensions.Backup}";
            var fullPath = Path.Combine(dir, fileName);

            var sql = $"BACKUP DATABASE [{DatabaseName}] TO DISK = N'{fullPath}' WITH INIT, FORMAT";
            await ExecuteNonQueryAsync(sql, ct);

            var info = new BackupFileInfo(
                Guid.NewGuid(),
                fileName,
                fullPath,
                DateTime.Now,
                new FileInfo(fullPath).Length);

            _logger.LogInformation("[BACKUP] 备份完成: {File} ({Size} bytes)", fileName, info.SizeBytes);
            return new BackupResult(true, null, info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP] 备份失败");
            return new BackupResult(false, ex.Message, null);
        }
        finally
        {
            _backupLock.Release();
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<BackupFileInfo>> ListBackupsAsync(CancellationToken ct = default)
    {
        var dir = BackupDirectoryPath;
        if (!Directory.Exists(dir))
            return Task.FromResult<IReadOnlyList<BackupFileInfo>>(Array.Empty<BackupFileInfo>());

        var files = Directory.GetFiles(dir, $"*{SystemConstants.FileExtensions.Backup}")
            .Select(f =>
            {
                var fi = new FileInfo(f);
                return new BackupFileInfo(
                    Guid.NewGuid(),
                    fi.Name,
                    fi.FullName,
                    fi.CreationTime,
                    fi.Length);
            })
            .OrderByDescending(f => f.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<BackupFileInfo>>(files);
    }

    /// <inheritdoc />
    public async Task<BackupStatus> GetStatusAsync(CancellationToken ct = default)
    {
        var files = await ListBackupsAsync(ct);
        return new BackupStatus(
            files.Count > 0 ? files[0].CreatedAt : null,
            files.Count,
            files.Sum(f => f.SizeBytes));
    }

    /// <inheritdoc />
    public async Task<RestoreResult> RestoreAsync(Guid backupId, CancellationToken ct = default)
    {
        var files = await ListBackupsAsync(ct);
        var target = files.FirstOrDefault(f => f.Id == backupId);
        if (target == null)
            return new RestoreResult(false, "未找到指定的备份文件");

        try
        {
            // 1. 停止内嵌 LocalWebAPI（释放 LocalDB 独占连接，RESTORE 才能成功）
            _logger.LogInformation("[BACKUP] 恢复前置：停止内嵌 LocalWebAPI");
            await _embeddedWebApi.StopAsync(ct);

            // 2. RESTORE DATABASE
            var sql = $"""
                ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                RESTORE DATABASE [{DatabaseName}] FROM DISK = N'{target.FullPath}' WITH REPLACE;
                ALTER DATABASE [{DatabaseName}] SET MULTI_USER;
                """;
            await ExecuteNonQueryAsync(sql, ct);

            // 3. 重启内嵌 LocalWebAPI（用户已确认自动重启）
            _logger.LogInformation("[BACKUP] 恢复完成，重启内嵌 LocalWebAPI");
            await _embeddedWebApi.StartAsync(ct);

            _logger.LogInformation("[BACKUP] 恢复成功: {File}", target.FileName);
            return new RestoreResult(true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BACKUP] 恢复失败");
            // 尽力恢复内嵌服务（即使恢复失败也尝试重启，避免应用失去本地模式）
            try { await _embeddedWebApi.StartAsync(CancellationToken.None); }
            catch (Exception startEx) { _logger.LogError(startEx, "[BACKUP] 恢复失败后重启内嵌服务也失败"); }
            return new RestoreResult(false, ex.Message);
        }
    }

    /// <inheritdoc />
    public Task<int> CleanupOldBackupsAsync(CancellationToken ct = default)
    {
        var dir = BackupDirectoryPath;
        if (!Directory.Exists(dir))
            return Task.FromResult(0);

        var cutoff = DateTime.Now.AddDays(-SystemConstants.BackupRetentionDays);
        var removed = 0;

        foreach (var file in Directory.GetFiles(dir, $"*{SystemConstants.FileExtensions.Backup}"))
        {
            ct.ThrowIfCancellationRequested();
            var fi = new FileInfo(file);
            if (fi.CreationTime < cutoff)
            {
                try
                {
                    fi.Delete();
                    removed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[BACKUP] 清理旧备份失败: {File}", file);
                }
            }
        }

        if (removed > 0)
            _logger.LogInformation("[BACKUP] 清理旧备份 {Count} 个", removed);
        return Task.FromResult(removed);
    }

    private async Task ExecuteNonQueryAsync(string sql, CancellationToken ct)
    {
        await using var connection = new SqlConnection(LocalConnectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text,
            CommandTimeout = 120
        };
        await command.ExecuteNonQueryAsync(ct);
    }
}
