namespace LYBT.Shared.Models.Contracts.Backup;

/// <summary>
/// 恢复模式（B-06）
/// </summary>
public enum RestoreMode
{
    /// <summary>整库恢复（<c>RESTORE DATABASE ... WITH REPLACE</c>，覆盖当前数据库）</summary>
    Full = 0,

    /// <summary>选择性恢复（还原到临时库后，按表/记录回写当前库）</summary>
    Selective = 1
}
