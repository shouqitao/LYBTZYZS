using LYBT.Shared.Models.Contracts.Backup;

namespace LYBT.Infrastructure.Services.Backup;

/// <summary>
/// 数据库备份/恢复服务契约（B-06 / US-SHELL-013 / NFR-AVAIL-001）。
/// </summary>
/// <remarks>
/// <para>双宿主共用：远程 WebAPI 针对服务端 SQL Server，LocalWebAPI（桌面内嵌）针对本机 LocalDB；
/// 连接串经 <c>IOptions&lt;DatabaseOptions&gt;</c> + <c>ConnectionStringResolver</c> 解析，数据库名取自连接串。</para>
/// <para>实现须为进程内串行（同一时刻仅一个备份/恢复/清理作业），进度经 <see cref="BackupJobTracker"/> 暴露。</para>
/// </remarks>
public interface IBackupService
{
    /// <summary>创建备份（全量/差异；可选压缩与文件级加密）</summary>
    Task<BackupOperationResultDto> CreateAsync(BackupCreateRequestDto request, CancellationToken ct = default);

    /// <summary>列出备份文件（按创建时间倒序）</summary>
    Task<IReadOnlyList<BackupFileDto>> ListAsync(CancellationToken ct = default);

    /// <summary>备份状态：上次备份时间/文件数量/总大小/进行中作业与进度</summary>
    Task<BackupStatusDto> GetStatusAsync(CancellationToken ct = default);

    /// <summary>恢复备份（整库覆盖或选择性回写表/记录；可选恢复前自动保护性备份）</summary>
    Task<BackupOperationResultDto> RestoreAsync(RestoreRequestDto request, CancellationToken ct = default);

    /// <summary>删除指定备份（被差异备份引用的全量备份拒绝删除）</summary>
    Task<BackupOperationResultDto> DeleteAsync(string backupId, CancellationToken ct = default);

    /// <summary>清理超过保留期的备份文件</summary>
    Task<BackupOperationResultDto> CleanupAsync(CancellationToken ct = default);

    /// <summary>列出可用于选择性恢复的表（含记录数）</summary>
    Task<IReadOnlyList<BackupTableDto>> ListTablesAsync(CancellationToken ct = default);

    /// <summary>
    /// 自动备份（NFR-AVAIL-001）：仅当距上次备份超过 <c>Backup:AutoBackup:IntervalHours</c> 时执行，
    /// 执行后顺带清理过期备份。返回 <c>AffectedCount=1</c> 表示已执行，<c>0</c> 表示未到间隔（未执行）。
    /// </summary>
    Task<BackupOperationResultDto> AutoBackupAsync(CancellationToken ct = default);
}
