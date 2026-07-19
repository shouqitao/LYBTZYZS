using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Herbs.Domain;
using LYBT.Module.Herbs.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Herbs.Infrastructure;

/// <summary>
/// 药材仓储实现。封装药材数据访问逻辑。
/// </summary>
public class HerbRepository : IHerbRepository
{
    private readonly HerbsDbContext _context;

    public HerbRepository(HerbsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<Herb?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Herbs
            .FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Herb?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Herbs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<Herb>> GetPagedAsync(
        int page, int pageSize, string? keyword, string? category,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Herbs
            .Where(h => !h.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.ToLower();
            query = query.Where(h =>
                h.Name.ToLower().Contains(kw) ||
                (h.PinYinCode != null && h.PinYinCode.ToLower().Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(h => h.Category != null && h.Category.Contains(category));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(h => h.PinYinCode ?? h.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Herb>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Herbs
            .Where(h => h.Name == name && !h.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(h => h.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(Herb herb, CancellationToken cancellationToken = default)
    {
        await _context.Herbs.AddAsync(herb, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Herb herb, CancellationToken cancellationToken = default)
    {
        _context.Herbs.Update(herb);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Herb?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Herbs
            .FirstOrDefaultAsync(h => h.Name == name && !h.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Herb?> GetByNameOrPinyinAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return null;

        var term = searchTerm.ToLower();

        // 优先精确匹配名称
        var byName = await _context.Herbs
            .FirstOrDefaultAsync(h => h.Name.ToLower() == term && !h.IsDeleted, cancellationToken);

        if (byName != null)
            return byName;

        // 然后匹配拼音码
        return await _context.Herbs
            .FirstOrDefaultAsync(h => h.PinYinCode != null && h.PinYinCode.ToLower() == term && !h.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Herb>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Herbs
            .Where(h => !h.IsDeleted)
            .OrderBy(h => h.PinYinCode ?? h.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Herb>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _context.Herbs
            .Where(h => !h.IsDeleted && h.Category != null && h.Category.Contains(category))
            .OrderBy(h => h.PinYinCode ?? h.Name)
            .ToListAsync(cancellationToken);
    }
}


