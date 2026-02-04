using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Sync;

namespace LYBT.Module.Sync.Interfaces;

/// <summary>
/// 同步服务接口 - 处理基础数据的双向同步
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// 获取指定实体类型的所有元数据（用于客户端比对）
    /// </summary>
    /// <param name="entityType">实体类型 (Herb/Patient/Formula)</param>
    /// <returns>元数据列表</returns>
    Task<ServiceResult<List<SyncMetadataDto>>> GetMetadataAsync(string entityType);

    /// <summary>
    /// 比对本地与服务器的差异
    /// </summary>
    /// <param name="input">比对请求</param>
    /// <returns>差异结果</returns>
    Task<ServiceResult<SyncCompareResultDto>> CompareAsync(SyncCompareInputDto input);

    /// <summary>
    /// 上传本地数据到服务器
    /// </summary>
    /// <param name="input">上传请求</param>
    /// <returns>上传结果</returns>
    Task<ServiceResult<SyncUploadResultDto>> UploadAsync(SyncUploadInputDto input);

    /// <summary>
    /// 从服务器下载数据
    /// </summary>
    /// <param name="input">下载请求</param>
    /// <returns>下载结果</returns>
    Task<ServiceResult<SyncDownloadResultDto>> DownloadAsync(SyncDownloadInputDto input);

    /// <summary>
    /// 同步删除操作（带引用检查）
    /// </summary>
    /// <param name="input">删除请求</param>
    /// <returns>删除结果</returns>
    Task<ServiceResult<SyncDeleteResultDto>> DeleteAsync(SyncDeleteInputDto input);

    /// <summary>
    /// 获取支持的实体类型列表
    /// </summary>
    /// <returns>实体类型列表</returns>
    IReadOnlyList<string> GetSupportedEntityTypes();
}
