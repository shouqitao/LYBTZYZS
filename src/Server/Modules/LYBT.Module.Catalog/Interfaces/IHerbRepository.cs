using LYBT.Entities.Herbs;

namespace LYBT.Module.Catalog.Interfaces;

/// <summary>
/// 药材仓储接口（实体特有查询，公共契约见 ICatalogRepository&lt;Herb&gt;）。
/// </summary>
public interface IHerbRepository : ICatalogRepository<Herb>
{
    /// <summary>
    /// 根据名称精确获取药材（批量导入匹配用）。
    /// </summary>
    Task<Herb?> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>按 ID 批量取名称（P1-11：批量引用检查避免 N+1——单次 IN 查询）</summary>
    Task<Dictionary<Guid, string>> GetNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}
