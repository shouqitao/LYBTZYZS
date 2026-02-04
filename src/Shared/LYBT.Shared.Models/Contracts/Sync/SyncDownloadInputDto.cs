namespace LYBT.Shared.Models.Contracts.Sync;

/// <summary>
/// 同步下载请求 DTO
/// </summary>
public class SyncDownloadInputDto
{
    /// <summary>
    /// 实体类型 (Herb/Patient/Formula)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 要下载的实体ID列表
    /// </summary>
    public List<Guid> EntityIds { get; set; } = new();
}
