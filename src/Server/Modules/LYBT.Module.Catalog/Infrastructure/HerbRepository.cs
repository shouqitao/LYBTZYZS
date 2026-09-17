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
        CancellationToken cancellationToken = default, bool includeChildren = false)
    {
        var query = _context.Herbs
            .AsNoTracking()
            .Where(h => !h.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            // P2-7: 前缀匹配走索引（EF.Functions.Like → SQL LIKE 'kw%'；SQL Server 默认 CI 排序不区分大小写）
            var kw = keyword.Trim();
            query = query.Where(h =>
                EF.Functions.Like(h.Name, $"{kw}%") ||
                (h.PinYinCode != null && EF.Functions.Like(h.PinYinCode, $"{kw}%")));
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
    /// P2-12: 导出专用查询——不分页，直接 ToListAsync
    public override async Task<List<Herb>> GetAllForExportAsync(
        string? keyword, string? category, Guid? operatorId = null, bool isAdmin = false,
        CancellationToken ct = default, bool includeChildren = false)
    {
        var query = _context.Herbs
            .AsNoTracking()
            .Where(h => !h.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(h =>
                EF.Functions.Like(h.Name, $"{kw}%") ||
                (h.PinYinCode != null && EF.Functions.Like(h.PinYinCode, $"{kw}%")));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(h => h.Category != null && h.Category.Contains(category));
        }

        return await query
            .OrderBy(h => h.PinYinCode ?? h.Name)
            .ToListAsync(ct);
    }

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

    /// <inheritdoc/>
    public async Task<List<Herb>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _context.Herbs
            .AsNoTracking()
            .Where(h => !h.IsDeleted && h.Status == LYBT.Shared.Models.Enums.CommonStatus.Enabled)
            .ToListAsync(ct);
    }
}
