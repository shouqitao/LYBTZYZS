using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Mappers;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
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
    private readonly LocalHerbMapper _mapper = new();

    public LocalHerbDataSource(LocalDbContext context, ILogger<LocalHerbDataSource> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HerbDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("[LocalDataSource] Herb.GetById - Id={Id}", id);
        var entity = await _context.Herbs
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, ct);

        return entity == null ? null : _mapper.ToDetailDto(entity);
    }

    public async Task<(List<HerbDetailDto> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken ct = default)
    {
        return await GetPagedAsync(page, pageSize, keyword, null, ct);
    }

    public async Task<(List<HerbDetailDto> Items, int Total)> GetPagedAsync(
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

        return (items.Select(e => _mapper.ToDetailDto(e)).ToList(), total);
    }

    public async Task<HerbDetailDto> CreateAsync(HerbInputDto input, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Herb.Create - Name={Name}", input.Name);

        var entity = _mapper.ToEntity(input);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;
        entity.Status = CommonStatus.Enabled;

        _context.Herbs.Add(entity);
        await _context.SaveChangesAsync(ct);

        return _mapper.ToDetailDto(entity);
    }

    public async Task<HerbDetailDto> UpdateAsync(HerbInputDto input, CancellationToken ct = default)
    {
        var id = input.Id ?? throw new InvalidOperationException("更新药材时必须提供ID");
        _logger.LogInformation("[LocalDataSource] Herb.Update - Id={Id}", id);

        var existing = await _context.Herbs.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"药材不存在: {id}");

        // 更新可变字段
        existing.Name = input.Name;
        existing.PinYinCode = input.PinYinCode;
        existing.Category = input.Category;
        existing.Origin = input.Origin;
        existing.Spec = input.Spec;
        existing.Unit = input.Unit;
        existing.Price = input.Price;
        existing.CostPrice = input.CostPrice ?? existing.CostPrice;
        existing.Effect = input.Effect;
        existing.Usage = input.Usage;
        existing.Remark = input.Remark;
        existing.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync(ct);

        return _mapper.ToDetailDto(existing);
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

    public async Task<HerbDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Herb.Restore - Id={Id}", id);

        var entity = await _context.Herbs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.Id == id, ct);

        if (entity == null)
            return null;

        entity.IsDeleted = false;
        await _context.SaveChangesAsync(ct);
        return _mapper.ToDetailDto(entity);
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

    public async Task<BatchOperationResultDto> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default)
    {
        _logger.LogInformation("[LocalDataSource] Herb.BatchDelete - Count={Count}", ids.Count);

        var result = new BatchOperationResultDto
        {
            TotalCount = ids.Count,
            IsSuccess = true
        };

        foreach (var id in ids)
        {
            var entity = await _context.Herbs.FindAsync([id], ct);
            if (entity != null)
            {
                entity.IsDeleted = true;
                result.SuccessCount++;
                result.SuccessfulIds.Add(id);
            }
            else
            {
                result.FailureCount++;
                result.FailedIds.Add(id);
                result.FailedItems.Add(new BatchOperationFailureItem
                {
                    Id = id,
                    Reason = "药材不存在"
                });
            }
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[LocalDataSource] Herb.BatchDelete completed - Success={Success}, Failure={Failure}",
            result.SuccessCount, result.FailureCount);

        return result;
    }
}
