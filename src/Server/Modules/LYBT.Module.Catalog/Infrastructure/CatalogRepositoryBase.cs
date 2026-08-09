using LYBT.Entities.Common;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Catalog.Infrastructure;

/// <summary>
/// 目录仓储泛型基类（A-31-C3b 合并 HerbRepository/FormulaRepository 同构方法）。
/// 分页/名称查重因实体差异（关键字字段/排序）由各具体仓储实现（走基类保护方法 ExistsAsync）。
/// </summary>
public abstract class CatalogRepositoryBase<TEntity> : BaseRepository<TEntity, CatalogDbContext>, ICatalogRepository<TEntity>
    where TEntity : BaseEntity
{
    protected CatalogRepositoryBase(CatalogDbContext context, ILogger logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc/>
    public abstract Task<PagedResult<TEntity>> GetPagedAsync(
        int page, int pageSize, string? keyword, string? category, CancellationToken ct);

    /// <inheritdoc/>
    public abstract Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
}
