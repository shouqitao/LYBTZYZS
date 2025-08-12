using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Domain.SeedWork;
using LYBT.Domain.Aggregates.HerbAggregate;

namespace LYBT.Infrastructure.Repositories.DDD
{
    /// <summary>
    /// 中药材聚合根Repository实现 - DDD模式
    /// </summary>
    public class HerbRepository : DomainRepositoryBase<Herb>, IHerbRepository
    {
        public HerbRepository(AppDbContext context, ILogger<HerbRepository> logger) 
            : base(context, logger)
        {
        }

        /// <summary>
        /// 根据名称查找中药材
        /// </summary>
        public async Task<List<Herb>> GetByNameAsync(string name)
        {
            Logger.LogDebug("Getting herbs by name: {Name}", name);
            
            if (string.IsNullOrWhiteSpace(name))
            {
                return new List<Herb>();
            }

            return await QueryAsNoTracking()
                .Where(h => h.Name.Contains(name) || 
                           h.CommonName.Contains(name) ||
                           h.LatinName.Contains(name) ||
                           h.BasicInfo.PinYinCode.Contains(name.ToUpper()) ||
                           h.BasicInfo.WuBiCode.Contains(name.ToUpper()))
                .OrderBy(h => h.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据药材分类获取中药材
        /// </summary>
        public async Task<List<Herb>> GetByCategoryAsync(HerbCategory category)
        {
            Logger.LogDebug("Getting herbs by category: {Category}", category);
            
            return await QueryAsNoTracking()
                .Where(h => h.Category == category)
                .OrderBy(h => h.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 获取活跃状态的中药材
        /// </summary>
        public async Task<List<Herb>> GetActiveHerbsAsync()
        {
            Logger.LogDebug("Getting active herbs");
            
            return await QueryAsNoTracking()
                .Where(h => h.IsActive)
                .OrderBy(h => h.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据药性查找中药材
        /// </summary>
        public async Task<List<Herb>> GetByNatureAsync(HerbNature nature)
        {
            Logger.LogDebug("Getting herbs by nature: {Nature}", nature);
            
            return await QueryAsNoTracking()
                .Where(h => h.Properties.Nature == nature)
                .OrderBy(h => h.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据药味查找中药材
        /// </summary>
        public async Task<List<Herb>> GetByFlavorAsync(HerbFlavor flavor)
        {
            Logger.LogDebug("Getting herbs by flavor: {Flavor}", flavor);
            
            return await QueryAsNoTracking()
                .Where(h => h.Properties.Flavor == flavor)
                .OrderBy(h => h.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据归经查找中药材
        /// </summary>
        public async Task<List<Herb>> GetByMeridiansAsync(string meridians)
        {
            Logger.LogDebug("Getting herbs by meridians: {Meridians}", meridians);
            
            if (string.IsNullOrWhiteSpace(meridians))
            {
                return new List<Herb>();
            }

            return await QueryAsNoTracking()
                .Where(h => h.Properties.Meridians.Contains(meridians))
                .OrderBy(h => h.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据主要功效查找中药材
        /// </summary>
        public async Task<List<Herb>> GetByMainEffectsAsync(string effects)
        {
            Logger.LogDebug("Getting herbs by main effects: {Effects}", effects);
            
            if (string.IsNullOrWhiteSpace(effects))
            {
                return new List<Herb>();
            }

            return await QueryAsNoTracking()
                .Where(h => h.Efficacy.MainEffects.Contains(effects))
                .OrderBy(h => h.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据适应症查找中药材
        /// </summary>
        public async Task<List<Herb>> GetByIndicationsAsync(string indications)
        {
            Logger.LogDebug("Getting herbs by indications: {Indications}", indications);
            
            if (string.IsNullOrWhiteSpace(indications))
            {
                return new List<Herb>();
            }

            return await QueryAsNoTracking()
                .Where(h => h.Efficacy.Indications.Contains(indications))
                .OrderBy(h => h.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据毒性等级获取中药材
        /// </summary>
        public async Task<List<Herb>> GetByToxicityAsync(HerbToxicity toxicity)
        {
            Logger.LogDebug("Getting herbs by toxicity: {Toxicity}", toxicity);
            
            return await QueryAsNoTracking()
                .Where(h => h.Properties.Toxicity == toxicity)
                .OrderBy(h => h.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据价格范围查找中药材
        /// </summary>
        public async Task<List<Herb>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            Logger.LogDebug("Getting herbs by price range: {MinPrice} - {MaxPrice}", minPrice, maxPrice);
            
            return await QueryAsNoTracking()
                .Where(h => h.PriceInfo.UnitPrice >= minPrice && h.PriceInfo.UnitPrice <= maxPrice)
                .OrderBy(h => h.PriceInfo.UnitPrice)
                .ToListAsync();
        }

        /// <summary>
        /// 检查药材名称是否已存在
        /// </summary>
        public async Task<bool> IsNameExistsAsync(string name, Guid? excludeHerbId = null)
        {
            Logger.LogDebug("Checking if herb name exists: {Name}, excluding: {ExcludeId}", 
                name, excludeHerbId);
            
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            var query = QueryAsNoTracking().Where(h => h.Name == name);
            
            if (excludeHerbId.HasValue)
            {
                query = query.Where(h => h.Id != excludeHerbId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// 获取常用中药材（根据使用频率）
        /// </summary>
        public async Task<List<Herb>> GetFrequentlyUsedHerbsAsync(int topCount = 50)
        {
            Logger.LogDebug("Getting frequently used herbs, top: {TopCount}", topCount);
            
            // 这里可以根据处方使用频率来排序，暂时按照名称排序
            return await QueryAsNoTracking()
                .Where(h => h.IsActive)
                .OrderBy(h => h.Name)
                .Take(topCount)
                .ToListAsync();
        }

        /// <summary>
        /// 获取中药材统计信息
        /// </summary>
        public async Task<HerbStatistics> GetStatisticsAsync()
        {
            Logger.LogDebug("Getting herb statistics");
            
            var totalCount = await CountAsync();
            var activeCount = await CountAsync(h => h.IsActive);
            var categoryStats = await QueryAsNoTracking()
                .GroupBy(h => h.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();
            
            var toxicHerbsCount = await CountAsync(h => h.Properties.Toxicity == HerbToxicity.Toxic);
            
            return new HerbStatistics
            {
                TotalHerbs = totalCount,
                ActiveHerbs = activeCount,
                InactiveHerbs = totalCount - activeCount,
                ToxicHerbs = toxicHerbsCount,
                CategoryStatistics = categoryStats.ToDictionary(x => x.Category, x => x.Count)
            };
        }

        /// <summary>
        /// 搜索中药材（综合搜索）
        /// </summary>
        public async Task<List<Herb>> SearchHerbsAsync(string searchTerm)
        {
            Logger.LogDebug("Searching herbs with term: {SearchTerm}", searchTerm);
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetActiveHerbsAsync();
            }

            var term = searchTerm.Trim().ToUpper();
            
            return await QueryAsNoTracking()
                .Where(h => h.IsActive && (
                    h.Name.Contains(searchTerm) ||
                    h.CommonName.Contains(searchTerm) ||
                    h.LatinName.Contains(searchTerm) ||
                    h.BasicInfo.PinYinCode.Contains(term) ||
                    h.BasicInfo.WuBiCode.Contains(term) ||
                    h.Efficacy.MainEffects.Contains(searchTerm) ||
                    h.Efficacy.Indications.Contains(searchTerm)
                ))
                .OrderBy(h => h.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 包含导航属性的查询重写
        /// </summary>
        protected override IQueryable<Herb> IncludeNavigationProperties(IQueryable<Herb> query)
        {
            // 中药材聚合根已经通过EF Core的OwnsOne配置包含了所有值对象
            // 无需额外的Include，EF Core会自动加载拥有的实体
            return query;
        }
    }

    /// <summary>
    /// 中药材统计信息
    /// </summary>
    public class HerbStatistics
    {
        public int TotalHerbs { get; set; }
        public int ActiveHerbs { get; set; }
        public int InactiveHerbs { get; set; }
        public int ToxicHerbs { get; set; }
        public Dictionary<HerbCategory, int> CategoryStatistics { get; set; } = new();
    }
}