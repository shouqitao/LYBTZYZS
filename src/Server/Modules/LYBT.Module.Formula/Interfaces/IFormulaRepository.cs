using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Formulas.Interfaces;

/// <summary>
/// 验方仓储接口（DDD） - 封验方数据访问逻辑。
/// </summary>
public interface IFormulaRepository
{
    /// <summary>
    /// 根据ID获取验方（含药材组成）。
    /// </summary>
    Task<LYBT.Entities.Formulas.Formula?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// 根据ID获取验方（包括已软删除的）。
    /// </summary>
    Task<LYBT.Entities.Formulas.Formula?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// 分页查询验方（支持关键字 + 分类筛选）。
    /// </summary>
    Task<PagedResult<LYBT.Entities.Formulas.Formula>> GetPagedAsync(int page, int pageSize, string? keyword, string? category, CancellationToken ct);

    /// <summary>
    /// 检查验方名称是否已存在。
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// 新增验方。
    /// </summary>
    Task AddAsync(LYBT.Entities.Formulas.Formula formula, CancellationToken ct);

    /// <summary>
    /// 更新验方。
    /// </summary>
    Task UpdateAsync(LYBT.Entities.Formulas.Formula formula, CancellationToken ct);

    /// <summary>
    /// 按条件查询验方（含药材组成）。
    /// </summary>
    Task<List<LYBT.Entities.Formulas.Formula>> FindWithHerbsAsync(
        System.Linq.Expressions.Expression<Func<LYBT.Entities.Formulas.Formula, bool>> predicate,
        CancellationToken ct = default);

    /// <summary>
    /// 获取所有验方（含药材组成）。
    /// </summary>
    Task<List<LYBT.Entities.Formulas.Formula>> GetAllWithHerbsAsync(CancellationToken ct = default);

    /// <summary>
    /// 按分类获取验方（含药材组成）。
    /// </summary>
    Task<List<LYBT.Entities.Formulas.Formula>> GetByCategoryWithHerbsAsync(string category, CancellationToken ct = default);
}


