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
}
