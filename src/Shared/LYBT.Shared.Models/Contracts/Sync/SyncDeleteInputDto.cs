namespace LYBT.Shared.Models.Contracts.Sync;

/// <summary>
/// 同步删除请求 DTO
/// </summary>
public class SyncDeleteInputDto
{
    /// <summary>
    /// 实体类型 (Herb/Patient/Formula)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 要删除的实体ID列表
    /// </summary>
    public List<Guid> EntityIds { get; set; } = new();
}
