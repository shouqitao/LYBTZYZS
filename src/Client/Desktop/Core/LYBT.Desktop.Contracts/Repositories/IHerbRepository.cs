using System.IO;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Contracts.Repositories;

/// <summary>
/// 药材数据仓储接口 (SYNC-D02)
/// List 返回轻量 ListDto，Detail 返回完整 DetailDto。
/// 远程模式和本地模式各有独立实现，由 DI 工厂根据 IConnectionModeProvider 选择。
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
    /// 批量导入药材数据 (仅远程模式支持)
    /// </summary>
    Task<HerbBatchImportResultDto?> BatchImportAsync(Stream fileStream, string fileName);

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
    /// 恢复已删除的药材
    /// </summary>
    Task<HerbDetailDto?> RestoreAsync(Guid id);

    /// <summary>
    /// 批量删除药材
    /// </summary>
    Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids);

    /// <summary>
    /// 批量启用药材
    /// </summary>
    Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids);

    /// <summary>
    /// 批量禁用药材
    /// </summary>
    Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids);

    #endregion

    #region 包装方法 (统一返回元组格式)

    /// <summary>
    /// 创建中药 (带结果包装)
    /// </summary>
    Task<(bool success, HerbDetailDto? data, string? error)> CreateWithResultAsync(HerbInputDto input);

    /// <summary>
    /// 更新中药 (带结果包装)
    /// </summary>
    Task<(bool success, HerbDetailDto? data, string? error)> UpdateWithResultAsync(Guid id, HerbInputDto input);

    /// <summary>
    /// 删除中药 (带结果包装)
    /// </summary>
    Task<(bool success, string? error)> DeleteWithResultAsync(Guid id);

    /// <summary>
    /// 根据 ID 获取中药详情 (带结果包装)
    /// </summary>
    Task<(bool success, HerbDetailDto? data, string? error)> GetByIdWithResultAsync(Guid id);

    #endregion
}
