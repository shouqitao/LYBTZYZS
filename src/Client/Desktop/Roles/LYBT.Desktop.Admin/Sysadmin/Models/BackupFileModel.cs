using LYBT.Shared.Models.Contracts.Backup;

namespace LYBT.Desktop.Admin.Sysadmin.Models;

/// <summary>
/// 备份文件展示模型（B-06）——包装 <see cref="BackupFileDto"/> 供 DataGrid 绑定，
/// 承担显示格式化（大小/类型/保护方式），避免 VM 直接持有 DTO（DP_M1）。
/// </summary>
public sealed class BackupFileModel
{
    /// <summary>稳定标识（恢复/删除时回传服务端）</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>文件名</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>备份类型</summary>
    public BackupKind Kind { get; init; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>文件大小（字节）</summary>
    public long SizeBytes { get; init; }

    /// <summary>是否加密</summary>
    public bool IsEncrypted { get; init; }

    /// <summary>是否压缩</summary>
    public bool IsCompressed { get; init; }

    /// <summary>差异备份的基准备份文件名</summary>
    public string? BaseFullBackupFileName { get; init; }

    /// <summary>差异备份的基准全量缺失（不可恢复）</summary>
    public bool IsChainBroken { get; init; }

    /// <summary>备份时间文本</summary>
    public string CreatedAtText => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>大小文本</summary>
    public string SizeText => FormatSize(SizeBytes);

    /// <summary>类型文本</summary>
    public string KindText => Kind switch
    {
        BackupKind.Differential => "差异",
        BackupKind.PreRestore => "恢复前保护",
        _ => "全量"
    };

    /// <summary>保护方式文本（压缩/加密/无）</summary>
    public string ProtectionText
    {
        get
        {
            var parts = new List<string>();
            if (IsCompressed)
                parts.Add("压缩");
            if (IsEncrypted)
                parts.Add("加密");
            return parts.Count == 0 ? "无" : string.Join("+", parts);
        }
    }

    /// <summary>基准备份文本（差异备份显示其全量基准）</summary>
    public string BaseText => string.IsNullOrEmpty(BaseFullBackupFileName) ? "—" : BaseFullBackupFileName;

    /// <summary>由契约 DTO 构造展示模型</summary>
    public static BackupFileModel FromDto(BackupFileDto dto) => new()
    {
        Id = dto.Id,
        FileName = dto.FileName,
        Kind = dto.Kind,
        CreatedAt = dto.CreatedAt,
        SizeBytes = dto.SizeBytes,
        IsEncrypted = dto.IsEncrypted,
        IsCompressed = dto.IsCompressed,
        BaseFullBackupFileName = dto.BaseFullBackupFileName,
        IsChainBroken = dto.IsChainBroken
    };

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
        >= 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B"
    };
}
