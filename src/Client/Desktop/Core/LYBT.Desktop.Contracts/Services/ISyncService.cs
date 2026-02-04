using LYBT.Shared.Models.Contracts.Sync;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 数据同步服务接口
/// 负责协调本地数据与服务器之间的同步操作
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// 获取支持的实体类型列表
    /// </summary>
    Task<IReadOnlyList<string>> GetSupportedEntityTypesAsync(CancellationToken ct = default);

    /// <summary>
    /// 检查差异（比对本地与服务器）
    /// </summary>
    /// <param name="entityType">实体类型 (Herb/Patient/Formula)</param>
    /// <returns>差异列表</returns>
    Task<SyncCheckResult> CheckDifferencesAsync(string entityType, CancellationToken ct = default);

    /// <summary>
    /// 上传本地数据到服务器
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityIds">要上传的实体ID列表</param>
    /// <returns>上传结果</returns>
    Task<SyncUploadResultDto> UploadAsync(string entityType, List<Guid> entityIds, CancellationToken ct = default);

    /// <summary>
    /// 从服务器下载数据到本地
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityIds">要下载的实体ID列表</param>
    /// <returns>下载结果</returns>
    Task<SyncDownloadResultDto> DownloadAsync(string entityType, List<Guid> entityIds, CancellationToken ct = default);

    /// <summary>
    /// 同步删除操作（服务器端带引用检查）
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityIds">要删除的实体ID列表</param>
    /// <returns>删除结果</returns>
    Task<SyncDeleteResultDto> DeleteAsync(string entityType, List<Guid> entityIds, CancellationToken ct = default);

    /// <summary>
    /// 执行完整同步（处理所有差异）
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="resolution">冲突解决策略</param>
    /// <returns>同步结果</returns>
    Task<SyncExecutionResult> ExecuteSyncAsync(
        string entityType,
        SyncResolution resolution,
        CancellationToken ct = default);
}

/// <summary>
/// 同步检查结果
/// </summary>
public class SyncCheckResult
{
    /// <summary>
    /// 实体类型
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 仅本地有的实体（待上传）
    /// </summary>
    public List<SyncDiffDto> LocalOnly { get; set; } = [];

    /// <summary>
    /// 仅服务器有的实体（待下载）
    /// </summary>
    public List<SyncDiffDto> ServerOnly { get; set; } = [];

    /// <summary>
    /// 双方都有但不同的实体（冲突）
    /// </summary>
    public List<SyncDiffDto> Conflicts { get; set; } = [];

    /// <summary>
    /// 是否有差异需要同步
    /// </summary>
    public bool HasDifferences => LocalOnly.Count > 0 || ServerOnly.Count > 0 || Conflicts.Count > 0;

    /// <summary>
    /// 总差异数
    /// </summary>
    public int TotalDifferences => LocalOnly.Count + ServerOnly.Count + Conflicts.Count;
}

/// <summary>
/// 冲突解决策略
/// </summary>
public class SyncResolution
{
    /// <summary>
    /// 要上传的实体ID列表
    /// </summary>
    public List<Guid> ToUpload { get; set; } = [];

    /// <summary>
    /// 要下载的实体ID列表
    /// </summary>
    public List<Guid> ToDownload { get; set; } = [];

    /// <summary>
    /// 冲突解决方式（EntityId -> UseLocal）
    /// true: 使用本地版本（上传）
    /// false: 使用服务器版本（下载）
    /// </summary>
    public Dictionary<Guid, bool> ConflictResolutions { get; set; } = [];

    /// <summary>
    /// 跳过的实体ID列表
    /// </summary>
    public List<Guid> Skipped { get; set; } = [];
}

/// <summary>
/// 同步执行结果
/// </summary>
public class SyncExecutionResult
{
    /// <summary>
    /// 实体类型
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 成功上传数量
    /// </summary>
    public int UploadedCount { get; set; }

    /// <summary>
    /// 成功下载数量
    /// </summary>
    public int DownloadedCount { get; set; }

    /// <summary>
    /// 跳过数量
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// 失败数量
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 错误信息列表
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// 是否全部成功
    /// </summary>
    public bool IsSuccess => FailedCount == 0 && Errors.Count == 0;
}
