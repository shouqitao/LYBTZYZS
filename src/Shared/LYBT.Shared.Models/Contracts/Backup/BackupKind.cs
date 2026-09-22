namespace LYBT.Shared.Models.Contracts.Backup;

/// <summary>
/// 备份类型（B-06 / US-SHELL-013）
/// </summary>
public enum BackupKind
{
    /// <summary>全量备份（<c>BACKUP DATABASE</c>，差异备份的基准）</summary>
    Full = 0,

    /// <summary>差异备份（<c>WITH DIFFERENTIAL</c>，依赖最近一次全量备份）</summary>
    Differential = 1,

    /// <summary>恢复前自动保护性备份（语义等同全量，仅用于区分展示）</summary>
    PreRestore = 2
}
