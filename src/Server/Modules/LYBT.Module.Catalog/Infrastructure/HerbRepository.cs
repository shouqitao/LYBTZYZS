using LYBT.Entities.Herbs;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Catalog.Infrastructure;

/// <summary>
/// 药材仓储实现。封装药材数据访问逻辑。
/// </summary>
public class HerbRepository : CatalogRepositoryBase<Herb>, IHerbRepository
{
    public HerbRepository(CatalogDbContext context, ILogger<HerbRepository> logger)
        : base(context, logger)
    {
    }

    /// <inheritdoc/>
    public override async Task<PagedResult<Herb>> GetPagedAsync(
        int page, int pageSize, string? keyword, string? category,
        Guid? operatorId = null, bool isAdmin = false,
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
    public override Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => ExistsAsync(e => e.Name == name, excludeId, cancellationToken);

    /// <inheritdoc/>
    public async Task<Herb?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Herbs
            .FirstOrDefaultAsync(h => h.Name == name && !h.IsDeleted, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<Guid, string>> GetNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return new Dictionary<Guid, string>();
        return await _context.Herbs
            .AsNoTracking()
            .Where(h => idList.Contains(h.Id) && !h.IsDeleted)
            .ToDictionaryAsync(h => h.Id, h => h.Name, ct);
    }
}
