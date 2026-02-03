using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.LocalData.Context;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.DataSources;

/// <summary>
/// 本地药材数据源实现 - SQLite EF Core
/// OpenSpec: implement-local-mode
/// </summary>
public class LocalHerbDataSource : IHerbDataSource
{
    private readonly LocalDbContext _context;
    private readonly ILogger<LocalHerbDataSource> _logger;

    public LocalHerbDataSource(LocalDbContext context, ILogger<LocalHerbDataSource> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Herb?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Herb.GetById - Id={Id}", id);
        return await _context.Herbs
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, ct);
    }

    public async Task<(List<Herb> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        return await GetPagedAsync(page, pageSize, keyword, null, ct);
    }

    public async Task<(List<Herb> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? category,
        CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Herb.GetPaged - Page={Page}, Keyword={Keyword}, Category={Category}",
            page, keyword, category);

        var query = _context.Herbs.AsNoTracking();

        // 关键词搜索
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(h =>
                h.Name.Contains(keyword) ||
                (h.PinYinCode != null && h.PinYinCode.Contains(keyword)));
        }

        // 分类过滤
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(h => h.Category == category);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(h => h.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Herb> CreateAsync(Herb entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Herb.Create - Name={Name}", entity.Name);

        entity.Id = Guid.NewGuid();
        _context.Herbs.Add(entity);
        await _context.SaveChangesAsync(ct);

        return entity;
    }

    public async Task<Herb> UpdateAsync(Herb entity, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Herb.Update - Id={Id}", entity.Id);

        var existing = await _context.Herbs.FindAsync([entity.Id], ct)
            ?? throw new InvalidOperationException($"药材不存在: {entity.Id}");

        _context.Entry(existing).CurrentValues.SetValues(entity);
        await _context.SaveChangesAsync(ct);

        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Herb.Delete - Id={Id}", id);

        var entity = await _context.Herbs.FindAsync([id], ct);
        if (entity == null)
            return false;

        entity.IsDeleted = true;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ToggleStatusAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Herb.ToggleStatus - Id={Id}", id);

        var entity = await _context.Herbs.FindAsync([id], ct);
        if (entity == null)
            return false;

        entity.Status = entity.Status == CommonStatus.Enabled
            ? CommonStatus.Disabled
            : CommonStatus.Enabled;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Herb?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Herb.Restore - Id={Id}", id);

        var entity = await _context.Herbs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.Id == id, ct);

        if (entity == null)
            return null;

        entity.IsDeleted = false;
        await _context.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<List<string>> GetCategoriesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Herb.GetCategories");

        return await _context.Herbs
            .AsNoTracking()
            .Where(h => h.Category != null)
            .Select(h => h.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);
    }
}
