using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Contracts.Repositories;

/// <summary>
/// 药材数据仓储接口
/// List 返回轻量 ListDto，Detail 返回完整 DetailDto。
/// </summary>
public interface IHerbRepository
{
    /// <summary>
    /// 分页查询药材列表 (返回轻量级 ListDto)
    /// </summary>
    Task<PagedResult<HerbListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null);

    /// <summary>
    /// 根据 ID 获取药材详情 (返回完整 DetailDto)
    /// </summary>
    Task<HerbDetailDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// 创建新药材
    /// </summary>
    Task<HerbDetailDto> CreateAsync(HerbInputDto dto);

    /// <summary>
    /// 更新药材信息
    /// </summary>
    Task<HerbDetailDto> UpdateAsync(HerbInputDto dto);

    /// <summary>
    /// 删除药材 (软删除)
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// 搜索药材 (基于关键词，返回 ListDto)
    /// </summary>
    Task<List<HerbListDto>> SearchAsync(string keyword);

    #region 批量导入/导出功能

    /// <summary>
    /// 批量导入药材数据
    /// </summary>
    Task<HerbBatchImportResultDto?> BatchImportAsync(HerbBatchImportInputDto request);

    /// <summary>
    /// 下载药材导入模板 (仅远程模式支持)
    /// </summary>
    Task<byte[]?> ExportTemplateAsync();

    /// <summary>
    /// 导出药材数据到 Excel (仅远程模式支持)
    /// </summary>
    Task<byte[]?> ExportHerbsAsync(string? keyword = null);

    #endregion

    #region 状态切换、恢复和批量操作

    /// <summary>
    /// 切换药材状态 (启用/禁用)
    /// </summary>
    Task<HerbDetailDto?> ToggleStatusAsync(Guid id);

    /// <summary>
    /// 批量删除药材
    /// </summary>
    Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids);

    #endregion
}
