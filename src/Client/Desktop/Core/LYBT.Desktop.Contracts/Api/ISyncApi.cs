using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Sync;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// 数据同步 API 客户端接口
/// 对应服务器端 SyncController
/// </summary>
public interface ISyncApi
{
    /// <summary>
    /// 获取支持的实体类型列表
    /// </summary>
    [Refit.Get("/api/v1/sync/entity-types")]
    Task<ApiResponse<IReadOnlyList<string>>> GetEntityTypesAsync();

    /// <summary>
    /// 获取指定实体类型的元数据（用于 Checksum 比对）
    /// </summary>
    /// <param name="entityType">实体类型 (Herb/Patient/Formula)</param>
    [Refit.Get("/api/v1/sync/metadata")]
    Task<ApiResponse<List<SyncMetadataDto>>> GetMetadataAsync(
        [Refit.Query] string entityType);

    /// <summary>
    /// 比对本地与服务器的差异
    /// </summary>
    [Refit.Post("/api/v1/sync/compare")]
    Task<ApiResponse<SyncCompareResultDto>> CompareAsync(
        [Refit.Body] SyncCompareInputDto input);

    /// <summary>
    /// 上传本地数据到服务器
    /// </summary>
    [Refit.Post("/api/v1/sync/upload")]
    Task<ApiResponse<SyncUploadResultDto>> UploadAsync(
        [Refit.Body] SyncUploadInputDto input);

    /// <summary>
    /// 从服务器下载数据
    /// </summary>
    [Refit.Post("/api/v1/sync/download")]
    Task<ApiResponse<SyncDownloadResultDto>> DownloadAsync(
        [Refit.Body] SyncDownloadInputDto input);

    /// <summary>
    /// 同步删除操作（带引用检查）
    /// </summary>
    [Refit.Post("/api/v1/sync/delete")]
    Task<ApiResponse<SyncDeleteResultDto>> DeleteAsync(
        [Refit.Body] SyncDeleteInputDto input);
}
