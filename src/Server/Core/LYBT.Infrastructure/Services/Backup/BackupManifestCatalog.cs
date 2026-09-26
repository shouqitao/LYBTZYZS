using LYBT.Shared.Models.Contracts.Backup;

namespace LYBT.Infrastructure.Services.Backup;

/// <summary>
/// 备份清单目录（A-04 职责提取，2026-09-26）：把「备份文件枚举」与「恢复链解析」从
/// <see cref="SqlServerBackupService"/> 的业务编排中分出。
/// </summary>
/// <remarks>
/// <para>仅承载**清单读取与纯解析**；保留期清理（清理策略）仍留在服务内——该路径无独立测试覆盖，
/// 迁移收益低于风险（见 13c A-04 记录）。</para>
/// <para><b>行为不变</b>：方法体自 <see cref="SqlServerBackupService"/> 原样迁移，目录/库名按次取值
/// （委托），保留运行时可被配置覆盖的语义。</para>
/// </remarks>
internal sealed class BackupManifestCatalog
{
    private readonly Func<string> _backupDirectory;
    private readonly Func<string> _databaseName;

    internal BackupManifestCatalog(Func<string> backupDirectory, Func<string> databaseName)
    {
        _backupDirectory = backupDirectory ?? throw new ArgumentNullException(nameof(backupDirectory));
        _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
    }

    /// <summary>枚举备份目录内的全部备份（按创建时间倒序）。目录不存在时返回空列表。</summary>
    internal List<BackupManifestEntry> List()
    {
        var directory = _backupDirectory();
        if (!Directory.Exists(directory))
            return new List<BackupManifestEntry>();

        var database = _databaseName();
        return Directory
            .EnumerateFiles(directory, "*" + BackupManifestStore.BackupFileSuffixes[0] + "*")
            .Where(BackupManifestStore.IsBackupFile)
            .Select(path => BackupManifestStore.ResolveEntry(path, database))
            .OrderByDescending(e => e.CreatedAt)
            .ToList();
    }

    /// <summary>解析恢复链：整库/差异备份 → [基准全量] 或 [基准全量, 差异]；基准缺失返回 null。</summary>
    internal static List<BackupManifestEntry>? BuildRestoreChain(
        IReadOnlyList<BackupManifestEntry> entries,
        BackupManifestEntry target)
    {
        if (target.Kind != BackupKind.Differential)
            return new List<BackupManifestEntry> { target };

        var baseEntry = !string.IsNullOrEmpty(target.BaseFullBackupFileName)
            ? entries.FirstOrDefault(e => string.Equals(e.FileName, target.BaseFullBackupFileName, StringComparison.OrdinalIgnoreCase))
            : entries.FirstOrDefault(e => e.Id == target.BaseFullBackupId);

        if (baseEntry == null)
            return null;

        return new List<BackupManifestEntry> { baseEntry, target };
    }
}
