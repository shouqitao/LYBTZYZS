namespace LYBT.Shared.Models.Contracts.Backup;

/// <summary>
/// 选择性恢复的表级请求（B-06：指定表/记录）
/// </summary>
public sealed class SelectiveRestoreTableDto
{
    /// <summary>表名（不含架构，固定 dbo；服务端按备份内实际表名校验）</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 记录级过滤：仅恢复这些主键记录（为空 = 恢复该表全部记录）。
    /// 仅支持具备 <c>Id</c> 列的表；表无 <c>Id</c> 列且指定了记录时该表将被拒绝。
    /// </summary>
    public List<Guid>? Ids { get; set; }
}
