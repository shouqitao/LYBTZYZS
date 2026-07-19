using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Formulas.Domain;
using LYBT.Module.Formulas.Interfaces;
using Microsoft.EntityFrameworkCore;
using FormulaEntity = LYBT.Entities.Formulas.Formula;

namespace LYBT.Module.Formulas.Infrastructure;

/// <summary>
/// 验方仓储实现。封装验方数据访问逻辑。
/// </summary>
public class FormulaRepository : IFormulaRepository
{
    private readonly FormulaDbContext _context;

    /// <summary>
    /// 初始化仓储。
    /// </summary>
    public FormulaRepository(FormulaDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<FormulaEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Formulas
            .Include(f => f.Herbs)
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<FormulaEntity?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Formulas
            .Include(f => f.Herbs)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<FormulaEntity>> GetPagedAsync(
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

        return new PagedResult<FormulaEntity>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.Formulas
            .Where(f => f.Name == name && !f.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(f => f.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    /// <inheritdoc/>
    public async Task AddAsync(FormulaEntity formula, CancellationToken cancellationToken = default)
    {
        await _context.Formulas.AddAsync(formula, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(FormulaEntity formula, CancellationToken cancellationToken = default)
    {
        _context.Formulas.Update(formula);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<FormulaEntity>> FindWithHerbsAsync(
        System.Linq.Expressions.Expression<Func<FormulaEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Formulas
            .Include(f => f.Herbs)
            .Where(f => !f.IsDeleted)
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<FormulaEntity>> GetAllWithHerbsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Formulas
            .Include(f => f.Herbs)
            .Where(f => !f.IsDeleted)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<FormulaEntity>> GetByCategoryWithHerbsAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _context.Formulas
            .Include(f => f.Herbs)
            .Where(f => !f.IsDeleted && f.Category != null && f.Category.Contains(category))
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}


