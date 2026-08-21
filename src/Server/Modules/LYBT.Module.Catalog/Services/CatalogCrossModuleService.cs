using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Catalog.Infrastructure;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Catalog.Services;

/// <summary>
/// 药材域跨模块服务实现（A-31-C3b 由 HerbCrossModuleService 迁入）。
/// 替代 CrossModuleService 中的药材查询逻辑，供 MedicalCase 等模块同步查询。
/// P2-8-5 越层白名单：注入 CatalogDbContext（模块级）直接查 Herb 表为跨聚合 ID 校验特例（ADR-0017）；
/// P10 守卫对 AppDbContext/IDbContextAccessor 生效，模块级 DbContext 属允许，见 ServerArchTests.P10。
/// </summary>
public class CatalogCrossModuleService : ICatalogCrossModuleService
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<CatalogCrossModuleService> _logger;

    public CatalogCrossModuleService(CatalogDbContext context, ILogger<CatalogCrossModuleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId, CancellationToken cancellationToken = default)
    {
        return await _context.Herbs
            .AsNoTracking()
            .Where(h => h.Id == herbId && !h.IsDeleted)
            .Select(h => new HerbBasicDto
            {
                Id = h.Id,
                Name = h.Name,
                Pinyin = h.PinYinCode,
                Category = h.Category
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, decimal>> GetHerbPricesAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default)
    {
        var idList = herbIds.ToList();
        if (idList.Count == 0) return new Dictionary<Guid, decimal>();

        var herbs = await _context.Herbs
            .AsNoTracking()
            .Where(h => idList.Contains(h.Id) && !h.IsDeleted)
            .Select(h => new { h.Id, h.Price })
            .ToListAsync(cancellationToken);

        return herbs.ToDictionary(h => h.Id, h => h.Price);
    }

    public async Task<HashSet<Guid>> GetExistingHerbIdsAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default)
    {
        var idList = herbIds.Distinct().ToList();
        if (idList.Count == 0) return new HashSet<Guid>();
        var existing = await _context.Herbs.AsNoTracking().Where(h => idList.Contains(h.Id) && !h.IsDeleted).Select(h => h.Id).ToListAsync(cancellationToken);
        return existing.ToHashSet();
    }

    public async Task<HashSet<Guid>> GetDisabledHerbIdsAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default)
    {
        var idList = herbIds.ToList();
        if (idList.Count == 0) return new HashSet<Guid>();

        var disabledIds = new HashSet<Guid>();
        var herbs = await _context.Herbs
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(h => idList.Contains(h.Id))
            .Select(h => new { h.Id, h.Status, h.IsDeleted })
            .ToListAsync(cancellationToken);

        foreach (var herb in herbs)
        {
            if (herb.Status == CommonStatus.Disabled || herb.IsDeleted)
            {
                disabledIds.Add(herb.Id);
            }
        }

        return disabledIds;
    }

    public async Task<List<HerbBasicDto>> GetAllActiveHerbsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Herbs
            .AsNoTracking()
            .Where(h => !h.IsDeleted && h.Status == CommonStatus.Enabled)
            .Select(h => new HerbBasicDto
            {
                Id = h.Id,
                Name = h.Name,
                Pinyin = h.PinYinCode,
                Category = h.Category
            })
            .ToListAsync(cancellationToken);
    }
}
