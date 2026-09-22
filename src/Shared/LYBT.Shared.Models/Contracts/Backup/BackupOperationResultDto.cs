namespace LYBT.Shared.Models.Contracts.Backup;

/// <summary>
/// 备份操作结果（B-06：创建/恢复/删除/清理统一返回）
/// </summary>
public sealed class BackupOperationResultDto
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>失败原因（成功时为 null）</summary>
    public string? Error { get; set; }

    /// <summary>非致命警告（如恢复前自动备份失败；成功但需知悉时给出）</summary>
    public string? Warning { get; set; }

    /// <summary>创建的备份文件信息（仅创建备份时返回）</summary>
    public BackupFileDto? File { get; set; }

    /// <summary>受影响记录/文件数（清理任务为删除文件数，选择性恢复为回写表数）</summary>
    public int? AffectedCount { get; set; }

    /// <summary>成功消息</summary>
    public string Message { get; set; } = string.Empty;
}
