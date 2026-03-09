namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 本地数据库备份服务接口
/// NFR-AVAIL-001: SQL Server LocalDB 启动自动备份
/// </summary>
public interface ILocalDbBackupService
{
    /// <summary>
    /// 执行数据库备份到指定目录
    /// 每天最多生成一个备份文件，命名格式: lybt_{yyyyMMdd}.bak
    /// </summary>
    Task BackupAsync(CancellationToken ct = default);

    /// <summary>
    /// 清理超过保留天数的旧备份文件
    /// </summary>
    Task CleanupOldBackupsAsync(int retentionDays = 7, CancellationToken ct = default);
}
