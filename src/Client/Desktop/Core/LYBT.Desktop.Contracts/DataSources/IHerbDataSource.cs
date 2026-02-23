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
}
