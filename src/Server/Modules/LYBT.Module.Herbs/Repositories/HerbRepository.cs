using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Infrastructure.Repositories.Optimized;
using LYBT.Entities.Herbs;
using LYBT.Module.Herbs.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.Module.Herbs.Repositories
{

    /// <summary>
    /// 药材仓储实现类 - 数据层统一化重构
    /// 继承OptimizedBaseRepository获得缓存和性能优化，实现药材特定业务逻辑
    /// </summary>
    public class HerbRepository : OptimizedBaseRepository<Herb>, IHerbRepository
    {
        public HerbRepository(
            AppDbContext context,
            ILogger<HerbRepository> logger,
            IMemoryCache cache) : base(context, logger, cache)
        {
        }

        // 注意：GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync等基础CRUD方法由OptimizedBaseRepository提供，带有缓存优化

        /// <summary>
        /// 批量新增药材
        /// </summary>
        public async Task<bool> AddRangeAsync(List<Herb> herbs)
        {
            if (herbs == null || herbs.Count == 0)
                return false;

            await _dbSet.AddRangeAsync(herbs);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        /// <summary>
        /// 检查药材名称是否存在 - 缓存优化版
        /// </summary>
        public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
        {
            var cacheKey = $"{CacheKeyPrefix}exists:name:{name}:{excludeId}";
            
            if (_cache.TryGetValue<bool>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取药材名称存在性检查 {Name}", name);
                return cached;
            }
            
            var query = _dbSet.AsNoTracking()
                .Where(h => h.Name == name);

            if (excludeId.HasValue)
            {
                query = query.Where(h => h.Id != excludeId.Value);
            }

            var exists = await query.AnyAsync();
            _cache.Set(cacheKey, exists, DefaultCacheDuration);
            return exists;
        }

        /// <summary>
        /// 根据拼音码搜索药材 - 缓存优化版
        /// </summary>
        public async Task<List<Herb>> SearchByPinyinAsync(string pinyin)
        {
            var cacheKey = $"{CacheKeyPrefix}pinyin:{pinyin.ToUpperInvariant()}";
            
            if (_cache.TryGetValue<List<Herb>>(cacheKey, out var cached) && cached != null)
            {
                _logger.LogDebug("从缓存获取拼音搜索结果 {Pinyin}", pinyin);
                return cached;
            }
            
            var herbs = await _dbSet
                .AsNoTracking()
                .Where(h => h.PinYinCode != null && h.PinYinCode.Contains(pinyin.ToUpperInvariant()))
                .OrderBy(h => h.Name)
                .ToListAsync();
                
            _cache.Set(cacheKey, herbs, DefaultCacheDuration);
            return herbs;
        }
    }
}