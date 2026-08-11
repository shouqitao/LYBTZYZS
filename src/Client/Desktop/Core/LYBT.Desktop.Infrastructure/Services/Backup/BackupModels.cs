namespace LYBT.Desktop.Infrastructure.Services.Backup;

/// <summary>
/// 备份文件信息（T7-1: NFR-AVAIL-001 本地备份）
/// </summary>
public record BackupFileInfo(
    Guid Id,
    string FileName,
    string FullPath,
    DateTime CreatedAt,
    long SizeBytes);

/// <summary>
/// 备份状态（US-SHELL-013: 上次备份时间/文件数量/总大小）
/// </summary>
public record BackupStatus(
    DateTime? LastBackupAt,
    int FileCount,
    long TotalSizeBytes);

/// <summary>
/// 备份结果
/// </summary>
public record BackupResult(
    bool Success,
    string? Error,
    BackupFileInfo? File);

/// <summary>
/// 恢复结果
/// </summary>
public record RestoreResult(
    bool Success,
    string? Error);
