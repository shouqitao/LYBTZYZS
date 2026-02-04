namespace LYBT.Shared.Models.Contracts.Sync;

/// <summary>
/// 同步比对结果 DTO
/// </summary>
public class SyncCompareResultDto
{
    /// <summary>
    /// 差异列表
    /// </summary>
    public List<SyncDiffDto> Diffs { get; set; } = new();

    /// <summary>
    /// 服务器端实体总数
    /// </summary>
    public int ServerTotalCount { get; set; }

    /// <summary>
    /// 比对时间
    /// </summary>
    public DateTime ComparedAt { get; set; } = DateTime.UtcNow;
}
