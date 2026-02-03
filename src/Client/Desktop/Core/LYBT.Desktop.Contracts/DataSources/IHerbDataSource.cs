using LYBT.Entities.Herbs;

namespace LYBT.Desktop.Contracts.DataSources;

/// <summary>
/// 药材数据源接口
/// OpenSpec: implement-local-mode
/// </summary>
public interface IHerbDataSource : IDataSourceBase<Herb>
{
    /// <summary>
    /// 分页获取药材列表（带分类过滤）
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="keyword">搜索关键词</param>
    /// <param name="category">分类过滤</param>
    /// <param name="ct">取消令牌</param>
    Task<(List<Herb> Items, int Total)> GetPagedAsync(
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
    Task<Herb?> RestoreAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 获取所有分类
    /// </summary>
    Task<List<string>> GetCategoriesAsync(CancellationToken ct = default);
}
