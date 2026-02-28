namespace LYBT.Shared.Models.Contracts.Sync;

/// <summary>
/// 删除被拒绝的项
/// </summary>
public class SyncDeleteRejectedItem
{
    /// <summary>
    /// 实体ID
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// 拒绝原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 同步删除结果 DTO
/// </summary>
public class SyncDeleteResultDto
{
    /// <summary>
    /// 成功删除的实体ID列表
    /// </summary>
    public List<Guid> Success { get; set; } = new();

    /// <summary>
    /// 被拒绝的删除项（有引用数据）
    /// </summary>
    public List<SyncDeleteRejectedItem> Rejected { get; set; } = new();

    /// <summary>
    /// 成功数量
    /// </summary>
    public int SuccessCount => Success.Count;

    /// <summary>
    /// 拒绝数量
    /// </summary>
    public int RejectedCount => Rejected.Count;

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount => SuccessCount + RejectedCount;
}
