using System.Text.Json;

namespace LYBT.Shared.Models.Contracts.Sync;

/// <summary>
/// 同步下载结果 DTO
/// </summary>
public class SyncDownloadResultDto
{
    /// <summary>
    /// 实体类型
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 实体数据列表（JSON 格式）
    /// </summary>
    public List<JsonElement> Entities { get; set; } = new();

    /// <summary>
    /// 下载数量
    /// </summary>
    public int Count { get; set; }
}
