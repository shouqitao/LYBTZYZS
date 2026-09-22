namespace LYBT.Shared.Models.Contracts.Backup;

/// <summary>
/// 备份文件信息（B-06：备份列表/大小/时间展示）
/// </summary>
/// <remarks>
/// <see cref="Id"/> 为稳定标识：有清单（manifest）时取清单 Id；历史遗留文件由文件名派生
/// （同一文件多次列举得到同一 Id，保证「选中 → 恢复/删除」不会因重新列举而失配）。
/// </remarks>
public sealed class BackupFileDto
{
    /// <summary>稳定标识（清单 Id 或文件名派生值）</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>文件名（含扩展名）</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>备份类型（全量/差异/恢复前保护）</summary>
    public BackupKind Kind { get; set; } = BackupKind.Full;

    /// <summary>创建时间（本地时间）</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>文件大小（字节）</summary>
    public long SizeBytes { get; set; }

    /// <summary>是否启用 SQL Server 备份压缩（<c>WITH COMPRESSION</c>）</summary>
    public bool IsCompressed { get; set; }

    /// <summary>是否启用文件级 AES-256 加密（口令派生密钥）</summary>
    public bool IsEncrypted { get; set; }

    /// <summary>所属数据库名</summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>差异备份的基准备份 Id（仅差异备份有值）</summary>
    public string? BaseFullBackupId { get; set; }

    /// <summary>差异备份的基准备份文件名（仅差异备份有值）</summary>
    public string? BaseFullBackupFileName { get; set; }

    /// <summary>是否为差异备份且基准全量备份缺失（该备份当前不可恢复）</summary>
    public bool IsChainBroken { get; set; }
}
