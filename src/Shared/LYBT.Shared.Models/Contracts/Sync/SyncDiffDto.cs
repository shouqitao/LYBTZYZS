namespace LYBT.Shared.Models.Contracts.Sync;

/// <summary>
/// 同步差异类型
/// </summary>
public enum SyncDiffType
{
    /// <summary>仅本地有（待上传）</summary>
    LocalOnly,

    /// <summary>仅服务器有（待下载）</summary>
    ServerOnly,

    /// <summary>双方都有但不同（冲突）</summary>
    Modified,

    /// <summary>完全相同（无需同步）</summary>
    Identical
}

/// <summary>
/// 同步差异 DTO - 描述本地与服务器之间的差异
/// </summary>
public class SyncDiffDto
{
    /// <summary>
    /// 实体类型 (Herb/Patient/Formula)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 实体ID
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// 差异类型
    /// </summary>
    public SyncDiffType DiffType { get; set; }

    /// <summary>
    /// 实体名称（用于展示，如药材名）
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// 本地 Checksum
    /// </summary>
    public string? LocalChecksum { get; set; }

    /// <summary>
    /// 服务器 Checksum
    /// </summary>
    public string? ServerChecksum { get; set; }

    /// <summary>
    /// 本地修改时间
    /// </summary>
    public DateTime? LocalChangedAt { get; set; }

    /// <summary>
    /// 服务器修改时间
    /// </summary>
    public DateTime? ServerChangedAt { get; set; }

    /// <summary>
    /// 变更字段列表（用于冲突展示）
    /// </summary>
    public List<string>? ChangedFields { get; set; }
}
