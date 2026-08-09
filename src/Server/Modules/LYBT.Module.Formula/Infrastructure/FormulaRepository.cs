using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formulas.Infrastructure;

/// <summary>
/// 验方仓储实现。封装验方数据访问逻辑。
/// </summary>
public class FormulaRepository : BaseRepository<Formula, FormulaDbContext>, IFormulaRepository
{
    /// <summary>
    /// 初始化仓储。
    /// </summary>
    public FormulaRepository(FormulaDbContext context, ILogger<FormulaRepository> logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc/>
    public override async Task<Formula?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Formulas
            .Include(f => f.Herbs)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    /// A-31-C5-3 例外：Formula 特化保留（需 Include Herbs 导航属性，与基类模板不同）
    public override async Task<Formula?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Formulas
            .Include(f => f.Herbs)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<Formula>> GetPagedAsync(
        int page, int pageSize, string? keyword, string? category,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Formulas
            .Include(f => f.Herbs)
            .Where(f => !f.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.ToLower();
            query = query.Where(f =>
                f.Name.ToLower().Contains(kw) ||
                (f.Effect != null && f.Effect.ToLower().Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(f => f.Category != null && f.Category.Contains(category));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Formula>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc/>
    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
        => ExistsAsync(f => f.Name == name, excludeId, ct);

    /// <inheritdoc/>
    public async Task<List<Formula>> FindWithHerbsAsync(
        System.Linq.Expressions.Expression<Func<Formula, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Formulas
            .Include(f => f.Herbs)
            .Where(f => !f.IsDeleted)
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }
}
