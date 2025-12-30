namespace LYBT.Desktop.Herbs.Interfaces;

using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

/// <summary>
/// 中药Service接口
/// 提供中药CRUD操作的统一处理
/// </summary>
public interface IHerbService
{
    /// <summary>
    /// 创建中药
    /// </summary>
    /// <param name="input">中药输入DTO</param>
    /// <returns>操作结果元组(成功标志, 详情DTO, 错误信息)</returns>
    Task<(bool success, HerbDetailDto? data, string? error)> CreateAsync(HerbInputDto input);

    /// <summary>
    /// 更新中药
    /// </summary>
    /// <param name="id">中药ID</param>
    /// <param name="input">中药输入DTO</param>
    /// <returns>操作结果元组(成功标志, 详情DTO, 错误信息)</returns>
    Task<(bool success, HerbDetailDto? data, string? error)> UpdateAsync(Guid id, HerbInputDto input);

    /// <summary>
    /// 删除中药
    /// </summary>
    /// <param name="id">中药ID</param>
    /// <returns>操作结果元组(成功标志, 错误信息)</returns>
    Task<(bool success, string? error)> DeleteAsync(Guid id);

    /// <summary>
    /// 根据ID获取中药详情
    /// </summary>
    /// <param name="id">中药ID</param>
    /// <returns>操作结果元组(成功标志, 详情DTO, 错误信息)</returns>
    Task<(bool success, HerbDetailDto? data, string? error)> GetByIdAsync(Guid id);

    /// <summary>
    /// 分页获取中药列表（只读查询）
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="keyword">搜索关键词</param>
    /// <returns>分页结果</returns>
    Task<PagedResult<HerbListDto>> GetPagedAsync(int page, int pageSize, string? keyword = null);
}
