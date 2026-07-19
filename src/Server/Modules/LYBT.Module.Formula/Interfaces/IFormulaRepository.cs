using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Formulas.Domain;
using FormulaEntity = LYBT.Entities.Formulas.Formula;

namespace LYBT.Module.Formulas.Interfaces;

/// <summary>
/// 验方仓储接口（DDD） - 封验方数据访问逻辑。
/// </summary>
public interface IFormulaRepository
{
    /// <summary>
    /// 根据ID获取验方（含药材组成）。
    /// </summary>
    Task<FormulaEntity?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// 根据ID获取验方（包括已软删除的）。
    /// </summary>
    Task<FormulaEntity?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// 分页查询验方（支持关键字 + 分类筛选）。
    /// </summary>
    Task<PagedResult<FormulaEntity>> GetPagedAsync(int page, int pageSize, string? keyword, string? category, CancellationToken ct);

    /// <summary>
    /// 检查验方名称是否已存在。
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// 新增验方。
    /// </summary>
    Task AddAsync(FormulaEntity formula, CancellationToken ct);

    /// <summary>
    /// 更新验方。
    /// </summary>
    Task UpdateAsync(FormulaEntity formula, CancellationToken ct);

    /// <summary>
    /// 按条件查询验方（含药材组成）。
    /// </summary>
    Task<List<FormulaEntity>> FindWithHerbsAsync(
        System.Linq.Expressions.Expression<Func<FormulaEntity, bool>> predicate,
        CancellationToken ct = default);

    /// <summary>
    /// 获取所有验方（含药材组成）。
    /// </summary>
    Task<List<FormulaEntity>> GetAllWithHerbsAsync(CancellationToken ct = default);

    /// <summary>
    /// 按分类获取验方（含药材组成）。
    /// </summary>
    Task<List<FormulaEntity>> GetByCategoryWithHerbsAsync(string category, CancellationToken ct = default);
}


