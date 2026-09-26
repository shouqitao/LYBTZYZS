using LYBT.Entities.Formulas;

namespace LYBT.Module.Catalog.Interfaces;

/// <summary>
/// 验方仓储接口（实体特有查询，公共契约见 ICatalogRepository&lt;Formula&gt;）。
/// </summary>
public interface IFormulaRepository : ICatalogRepository<Formula>
{
    /// <summary>
    /// 根据名称精确获取验方（批量导入重复策略 Update 用；已软删同名视为不存在）。
    /// </summary>
    Task<Formula?> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// 按条件查询验方（含药材组成）。
    /// </summary>
    Task<List<Formula>> FindWithHerbsAsync(
        System.Linq.Expressions.Expression<Func<Formula, bool>> predicate,
        CancellationToken ct = default);
}
