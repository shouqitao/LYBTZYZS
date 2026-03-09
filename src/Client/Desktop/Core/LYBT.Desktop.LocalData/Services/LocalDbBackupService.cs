using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.LocalData.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Services;

/// <summary>
/// SQL Server LocalDB 数据库备份服务
/// NFR-AVAIL-001: 启动时自动备份，保留最近 7 天
/// 使用 BACKUP DATABASE T-SQL 命令实现
/// </summary>
public class LocalDbBackupService : ILocalDbBackupService
{
    private readonly LocalDbContext _context;
    private readonly ILogger<LocalDbBackupService> _logger;

    /// <summary>
    /// 备份文件存放目录: %AppData%/LYBTZYZS/Backup/
    /// </summary>
    private static readonly string BackupDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LYBTZYZS", "Backup");

    public LocalDbBackupService(LocalDbContext context, ILogger<LocalDbBackupService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task BackupAsync(CancellationToken ct = default)
    {
        var backupFileName = $"lybt_{DateTime.Now:yyyyMMdd}.bak";
        var backupPath = Path.Combine(BackupDirectory, backupFileName);

        // 今天已有备份则跳过
        if (File.Exists(backupPath))
        {
            _logger.LogDebug("[Backup] 今日备份已存在: {Path}，跳过", backupPath);
            return;
        }

        try
        {
            // 确保备份目录存在
            Directory.CreateDirectory(BackupDirectory);

            var databaseName = _context.Database.GetDbConnection().Database;
            var sql = $"BACKUP DATABASE [{databaseName}] TO DISK = '{backupPath}' WITH INIT, COMPRESSION";

            _logger.LogInformation("[Backup] 开始备份数据库 {Database} -> {Path}", databaseName, backupPath);

            await _context.Database.ExecuteSqlRawAsync(sql, ct);

            var fileInfo = new FileInfo(backupPath);
            _logger.LogInformation("[Backup] 备份完成，文件大小: {Size:F2} MB", fileInfo.Length / (1024.0 * 1024.0));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Backup] 数据库备份失败，不影响正常启动");
        }
    }

    /// <inheritdoc />
    public async Task CleanupOldBackupsAsync(int retentionDays = 7, CancellationToken ct = default)
    {
        try
        {
            if (!Directory.Exists(BackupDirectory))
            {
                return;
            }

            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            var oldFiles = Directory.GetFiles(BackupDirectory, "lybt_*.bak")
                .Select(f => new FileInfo(f))
                .Where(f => f.LastWriteTime < cutoffDate)
                .ToList();

            foreach (var file in oldFiles)
            {
                file.Delete();
                _logger.LogDebug("[Backup] 已清理过期备份: {FileName}", file.Name);
            }

            if (oldFiles.Count > 0)
            {
                _logger.LogInformation("[Backup] 已清理 {Count} 个过期备份 (保留 {Days} 天)", oldFiles.Count, retentionDays);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Backup] 清理过期备份失败");
        }
    }
}
