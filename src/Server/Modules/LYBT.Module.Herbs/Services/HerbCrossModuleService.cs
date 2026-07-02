using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Services;

/// <summary>
/// 药材跨模块服务实现
/// 替代 CrossModuleService 中的药材查询逻辑
/// </summary>
public class HerbCrossModuleService : IHerbCrossModuleService
{
    private readonly AppDbContext _context;
    private readonly ILogger<HerbCrossModuleService> _logger;

    public HerbCrossModuleService(IDbContextAccessor dbAccessor, ILogger<HerbCrossModuleService> logger)
    {
        _context = dbAccessor.Context;
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

    public async Task<HerbBasicDto?> GetHerbByNameOrPinyinAsync(string nameOrPinyin, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nameOrPinyin))
            return null;

        return await _context.Herbs
            .AsNoTracking()
            .Where(h => !h.IsDeleted &&
                (h.Name == nameOrPinyin || h.PinYinCode == nameOrPinyin))
            .Select(h => new HerbBasicDto
            {
                Id = h.Id,
                Name = h.Name,
                Pinyin = h.PinYinCode,
                Category = h.Category
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ReferenceCheckResult> CheckHerbReferenceAsync(Guid herbId, CancellationToken cancellationToken = default)
    {
        var count = await _context.PrescriptionItems
            .AsNoTracking()
            .CountAsync(pi => pi.HerbId == herbId, cancellationToken);

        return new ReferenceCheckResult(
            HasReferences: count > 0,
            ReferenceCount: count,
            Message: count > 0 ? $"药材被 {count} 个处方项引用" : null);
    }

    public async Task<Dictionary<Guid, decimal>> GetHerbPricesAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default)
    {
        var idList = herbIds.ToList();
        if (idList.Count == 0) return new Dictionary<Guid, decimal>();

        var result = new Dictionary<Guid, decimal>();
        foreach (var herbId in idList)
        {
            var herb = await _context.Herbs
                .AsNoTracking()
                .Where(h => h.Id == herbId && !h.IsDeleted)
                .Select(h => new { h.Id, h.Price })
                .FirstOrDefaultAsync(cancellationToken);

            if (herb != null)
            {
                result[herb.Id] = herb.Price;
            }
        }

        return result;
    }

    public async Task<HashSet<Guid>> GetDisabledHerbIdsAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default)
    {
        var idList = herbIds.ToList();
        if (idList.Count == 0) return new HashSet<Guid>();

        var disabledIds = new HashSet<Guid>();
        foreach (var herbId in idList)
        {
            var herb = await _context.Herbs
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(h => h.Id == herbId)
                .Select(h => new { h.Id, h.Status, h.IsDeleted })
                .FirstOrDefaultAsync(cancellationToken);

            if (herb != null && (herb.IsDeleted || herb.Status == CommonStatus.Disabled))
            {
                disabledIds.Add(herb.Id);
            }
        }

        return disabledIds;
    }
}


