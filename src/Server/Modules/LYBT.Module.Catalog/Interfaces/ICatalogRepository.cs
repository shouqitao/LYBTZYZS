using LYBT.Entities.Common;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Catalog.Interfaces;

/// <summary>
/// 目录仓储泛型接口（A-31-C3b 合并 IHerbRepository/IFormulaRepository 公共契约）。
/// 封装药材/验方共享的仓储访问逻辑（同构方法），实体特有方法由各具体接口扩展。
/// </summary>
/// <typeparam name="TEntity">目录实体（Herb / Formula）</typeparam>
public interface ICatalogRepository<TEntity> : IRepository<TEntity>
    where TEntity : BaseEntity
{
    /// <summary>
    /// 根据ID获取实体（包括已软删除的，恢复操作使用）。
    /// </summary>
    Task<TEntity?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// 分页查询实体（支持关键字 + 分类筛选）。
    /// </summary>
    Task<PagedResult<TEntity>> GetPagedAsync(int page, int pageSize, string? keyword, string? category, CancellationToken ct);

    /// <summary>
    /// 检查实体名称是否已存在。
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
}
