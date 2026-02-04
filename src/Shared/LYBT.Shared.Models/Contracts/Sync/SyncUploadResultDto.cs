namespace LYBT.Shared.Models.Contracts.Sync;

/// <summary>
/// 单条上传结果
/// </summary>
public class SyncUploadItemResult
{
    /// <summary>
    /// 实体ID
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误信息（失败时）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 是否为冲突
    /// </summary>
    public bool IsConflict { get; set; }
}

/// <summary>
/// 同步上传结果 DTO
/// </summary>
public class SyncUploadResultDto
{
    /// <summary>
    /// 成功数量
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 冲突数量
    /// </summary>
    public int ConflictCount { get; set; }

    /// <summary>
    /// 错误数量
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// 详细结果列表
    /// </summary>
    public List<SyncUploadItemResult> Results { get; set; } = new();
}
