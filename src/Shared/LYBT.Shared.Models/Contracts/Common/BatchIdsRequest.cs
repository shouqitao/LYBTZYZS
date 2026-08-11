namespace LYBT.Shared.Models.Contracts.Common;

/// <summary>
/// 批量 ID 请求（B1 US-MC-018: 批量详情查询）
/// </summary>
public class BatchIdsRequest
{
    /// <summary>ID 列表（上限 100 条）</summary>
    public List<Guid> Ids { get; set; } = new();
}
