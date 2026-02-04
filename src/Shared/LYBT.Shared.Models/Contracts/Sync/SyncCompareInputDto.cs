namespace LYBT.Shared.Models.Contracts.Sync;

/// <summary>
/// 本地实体元数据（用于比对请求）
/// </summary>
public class LocalEntityMetadata
{
    /// <summary>
    /// 实体ID
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// 本地 Checksum
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// 本地修改时间
    /// </summary>
    public DateTime? LastModifiedAt { get; set; }
}

/// <summary>
/// 同步比对请求 DTO
/// </summary>
public class SyncCompareInputDto
{
    /// <summary>
    /// 实体类型 (Herb/Patient/Formula)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 本地实体元数据列表
    /// </summary>
    public List<LocalEntityMetadata> LocalEntities { get; set; } = new();
}
