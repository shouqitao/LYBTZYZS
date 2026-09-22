using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LYBT.Shared.Models.Contracts.Backup;

namespace LYBT.Infrastructure.Services.Backup;

/// <summary>
/// 备份文件清单（B-06）。
/// </summary>
/// <remarks>
/// <para>每个备份文件旁挂一份 <c>{文件名}.manifest.json</c> 侧车文件，记录稳定 Id、类型、大小、
/// 压缩/加密标记、数据库名与差异备份的基准全量备份——这是「按 Id 恢复/删除」与「差异链完整性」的依据。</para>
/// <para><b>历史遗留文件</b>（无侧车清单，如 T7-1 时期产物）：Id 由文件名确定性派生（MD5 → Guid），
/// 保证多次列举得到同一 Id；类型按全量处理。修复了原实现「每次列举都 <c>Guid.NewGuid()</c>
/// 导致选中项与恢复目标失配」的缺陷。</para>
/// </remarks>
public static class BackupManifestStore
{
    /// <summary>侧车清单文件后缀</summary>
    public const string ManifestSuffix = ".manifest.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>备份文件扩展名（未加密 / 加密）</summary>
    public static readonly string[] BackupFileSuffixes =
    [
        ".bak",
        ".bak" + BackupFileEncryption.EncryptedExtension
    ];

    /// <summary>判断路径是否为备份文件（排除侧车清单）</summary>
    public static bool IsBackupFile(string path)
    {
        if (path.EndsWith(ManifestSuffix, StringComparison.OrdinalIgnoreCase))
            return false;

        return BackupFileSuffixes.Any(suffix => path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>侧车清单路径</summary>
    public static string ManifestPath(string backupFilePath) => backupFilePath + ManifestSuffix;

    /// <summary>读取侧车清单（缺失或损坏时返回 null）</summary>
    public static BackupManifestEntry? TryRead(string backupFilePath)
    {
        var manifestPath = ManifestPath(backupFilePath);
        if (!File.Exists(manifestPath))
            return null;

        try
        {
            var json = File.ReadAllText(manifestPath);
            return JsonSerializer.Deserialize<BackupManifestEntry>(json, SerializerOptions);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null;
        }
    }

    /// <summary>写入侧车清单</summary>
    public static void Write(string backupFilePath, BackupManifestEntry entry)
    {
        var manifestPath = ManifestPath(backupFilePath);
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(entry, SerializerOptions));
    }

    /// <summary>删除侧车清单（不存在时忽略）</summary>
    public static void Delete(string backupFilePath)
    {
        var manifestPath = ManifestPath(backupFilePath);
        if (File.Exists(manifestPath))
            File.Delete(manifestPath);
    }

    /// <summary>
    /// 由文件名确定性派生 Id（历史遗留文件无清单时使用）——
    /// 同一文件名多次列举必得同一 Id。
    /// </summary>
    public static string DeterministicId(string fileName)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(fileName.ToLowerInvariant()));
        return new Guid(hash).ToString();
    }

    /// <summary>把文件系统中的备份文件解析为清单条目（无清单时按文件名/文件时间合成）</summary>
    public static BackupManifestEntry ResolveEntry(string filePath, string databaseName)
    {
        var info = new FileInfo(filePath);
        var manifest = TryRead(filePath);

        if (manifest != null)
        {
            // 文件被外部替换/移动时以磁盘事实为准（大小/时间）
            manifest.FileName = info.Name;
            manifest.SizeBytes = info.Length;
            if (manifest.CreatedAt == default)
                manifest.CreatedAt = info.CreationTime;
            return manifest;
        }

        return new BackupManifestEntry
        {
            Id = DeterministicId(info.Name),
            FileName = info.Name,
            Kind = BackupKind.Full,
            CreatedAt = info.CreationTime,
            SizeBytes = info.Length,
            IsCompressed = false,
            IsEncrypted = BackupFileEncryption.IsEncryptedFile(filePath),
            DatabaseName = databaseName
        };
    }
}

/// <summary>
/// 备份文件清单条目（侧车 JSON 结构）
/// </summary>
public sealed class BackupManifestEntry
{
    /// <summary>稳定标识</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>文件名（含扩展名）</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>备份类型</summary>
    public BackupKind Kind { get; set; }

    /// <summary>创建时间（本地时间）</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>文件大小（字节）</summary>
    public long SizeBytes { get; set; }

    /// <summary>是否启用 SQL Server 备份压缩</summary>
    public bool IsCompressed { get; set; }

    /// <summary>是否启用文件级加密</summary>
    public bool IsEncrypted { get; set; }

    /// <summary>数据库名</summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>差异备份的基准全量备份 Id</summary>
    public string? BaseFullBackupId { get; set; }

    /// <summary>差异备份的基准全量备份文件名</summary>
    public string? BaseFullBackupFileName { get; set; }

    /// <summary>转换为 API 契约</summary>
    public BackupFileDto ToDto(bool isChainBroken = false) => new()
    {
        Id = Id,
        FileName = FileName,
        Kind = Kind,
        CreatedAt = CreatedAt,
        SizeBytes = SizeBytes,
        IsCompressed = IsCompressed,
        IsEncrypted = IsEncrypted,
        DatabaseName = DatabaseName,
        BaseFullBackupId = BaseFullBackupId,
        BaseFullBackupFileName = BaseFullBackupFileName,
        IsChainBroken = isChainBroken
    };
}
