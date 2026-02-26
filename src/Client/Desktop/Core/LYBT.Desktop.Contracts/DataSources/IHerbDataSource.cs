using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Contracts.DataSources;

/// <summary>
/// 药材数据源接口
/// </summary>
public interface IHerbDataSource : IDataSourceBase<HerbDetailDto, HerbInputDto>
{
    /// <summary>
    /// 分页获取药材列表（带分类过滤）
    /// </summary>
    Task<(List<HerbDetailDto> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? category,
        CancellationToken ct = default);

    /// <summary>
    /// 切换药材状态（启用/禁用）
    /// </summary>
    Task<bool> ToggleStatusAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 恢复已删除的药材
    /// </summary>
    Task<HerbDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 获取所有分类
    /// </summary>
    Task<List<string>> GetCategoriesAsync(CancellationToken ct = default);

    /// <summary>
    /// 批量删除药材
    /// </summary>
    Task<BatchOperationResultDto> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default);

    // Sprint 4 X2 扩展方法
    // OpenSpec: SYNC-D02 - 过渡态方法

    /// <summary>
    /// T4-X2-13: 批量切换药材状态
    /// </summary>
    Task<BatchOperationResultDto> BatchToggleStatusAsync(List<Guid> ids, bool enable, CancellationToken ct = default);

    /// <summary>
    /// T4-X2-14/15: 批量导入药材
    /// </summary>
    Task<BatchOperationResultDto> BatchImportAsync(List<HerbInputDto> items, CancellationToken ct = default);

    /// <summary>
    /// T4-X2-16: 获取导出数据
    /// </summary>
    Task<List<HerbDetailDto>> GetAllForExportAsync(string? keyword = null, CancellationToken ct = default);

    /// <summary>
    /// T4-X2-17: 检查药材是否被验方/处方引用
    /// </summary>
    Task<bool> HasReferencesAsync(Guid herbId, CancellationToken ct = default);

    /// <summary>
    /// T4-X2-18: 获取导入模板列头
    /// </summary>
    string[] GetImportTemplateColumns();
}
