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
    /// 实体数据列表（JSON 字符串格式）
    /// </summary>
    public List<string> Entities { get; set; } = new();

    /// <summary>
    /// 下载数量
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 成功数量（等于 Count）
    /// </summary>
    public int SuccessCount => Count;

    /// <summary>
    /// 错误数量
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// 错误信息列表
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
