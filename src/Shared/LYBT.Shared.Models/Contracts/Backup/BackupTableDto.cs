namespace LYBT.Shared.Models.Contracts.Backup;

/// <summary>
/// 可恢复表信息（B-06 选择性恢复 UI 的表清单）
/// </summary>
public sealed class BackupTableDto
{
    /// <summary>表名（不含架构）</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>当前库中该表记录数</summary>
    public int RowCount { get; set; }

    /// <summary>是否具备 <c>Id</c> 列（支持记录级选择性恢复）</summary>
    public bool SupportsRecordSelection { get; set; }
}
