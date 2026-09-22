namespace LYBT.Shared.Models.Contracts.Backup;

/// <summary>
/// 备份状态（B-06 / US-SHELL-013：上次备份时间/文件数量/总大小 + 进行中操作与进度）
/// </summary>
public sealed class BackupStatusDto
{
    /// <summary>上次备份时间（无备份时为 null）</summary>
    public DateTime? LastBackupAt { get; set; }

    /// <summary>备份文件数量</summary>
    public int FileCount { get; set; }

    /// <summary>备份文件总大小（字节）</summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>备份目录（可配置）</summary>
    public string BackupDirectory { get; set; } = string.Empty;

    /// <summary>保留天数（超期自动清理）</summary>
    public int RetentionDays { get; set; }

    /// <summary>当前数据库名</summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>是否有备份/恢复/清理操作正在进行</summary>
    public bool IsOperationRunning { get; set; }

    /// <summary>进行中操作类型（备份/恢复/清理），无操作时为 null</summary>
    public string? OperationKind { get; set; }

    /// <summary>进行中操作的阶段说明（用于进度文案）</summary>
    public string? PhaseMessage { get; set; }

    /// <summary>进行中操作进度（0-100；无法量化时为 0）</summary>
    public int ProgressPercent { get; set; }

    /// <summary>进行中操作开始时间</summary>
    public DateTime? OperationStartedAt { get; set; }

    /// <summary>最近一次失败原因（成功后清空）</summary>
    public string? LastError { get; set; }

    /// <summary>自动备份是否启用（计划调度）</summary>
    public bool AutoBackupEnabled { get; set; }

    /// <summary>自动备份间隔（小时）</summary>
    public int AutoBackupIntervalHours { get; set; }
}
