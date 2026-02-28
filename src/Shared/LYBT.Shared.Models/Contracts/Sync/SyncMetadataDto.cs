namespace LYBT.Shared.Models.Contracts.Sync;

/// <summary>
/// 同步元数据 DTO - 用于 Checksum 比对
/// </summary>
public class SyncMetadataDto
{
    /// <summary>
    /// 实体ID
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Checksum (SHA256)
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime LastModifiedAt { get; set; }

    /// <summary>
    /// 是否已删除
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 实体名称（显示用）
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// 实体类型
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
}
