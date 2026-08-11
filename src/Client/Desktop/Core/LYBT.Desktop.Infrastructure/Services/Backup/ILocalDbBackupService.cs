namespace LYBT.Desktop.Infrastructure.Services.Backup;

/// <summary>
/// 本地数据库备份服务契约（T7-1: NFR-AVAIL-001 已设计接口名）。
/// 仅服务本地模式 LocalDB（远程模式备份由 SQL Server Agent 承担）。
/// </summary>
public interface ILocalDbBackupService
{
    /// <summary>
    /// 执行备份（T-SQL BACKUP DATABASE），返回备份文件信息
    /// </summary>
    Task<BackupResult> BackupAsync(CancellationToken ct = default);

    /// <summary>
    /// 列出备份文件（按创建时间倒序）
    /// </summary>
    Task<IReadOnlyList<BackupFileInfo>> ListBackupsAsync(CancellationToken ct = default);

    /// <summary>
    /// 备份状态：上次备份时间 / 文件数量 / 总大小
    /// </summary>
    Task<BackupStatus> GetStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// 从指定备份文件恢复（T-SQL RESTORE DATABASE）。
    /// 编排：停止内嵌 LocalWebAPI → 恢复 → 重启内嵌服务（用户已确认自动重启）。
    /// </summary>
    Task<RestoreResult> RestoreAsync(Guid backupId, CancellationToken ct = default);

    /// <summary>
    /// 清理超过保留期（7 天）的备份文件
    /// </summary>
    Task<int> CleanupOldBackupsAsync(CancellationToken ct = default);
}
