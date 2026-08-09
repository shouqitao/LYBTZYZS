using LYBT.Entities.Formulas;

namespace LYBT.Module.Catalog.Interfaces;

/// <summary>
/// 验方仓储接口（实体特有查询，公共契约见 ICatalogRepository&lt;Formula&gt;）。
/// </summary>
public interface IFormulaRepository : ICatalogRepository<Formula>
{
    /// <summary>
    /// 按条件查询验方（含药材组成）。
    /// </summary>
    Task<List<Formula>> FindWithHerbsAsync(
        System.Linq.Expressions.Expression<Func<Formula, bool>> predicate,
        CancellationToken ct = default);
}
