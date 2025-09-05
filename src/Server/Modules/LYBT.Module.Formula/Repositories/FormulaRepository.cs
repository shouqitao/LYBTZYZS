using LYBT.Entities.Formula;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formula.Repositories
{
    /// <summary>
    /// 验方仓储实现 - 数据层统一化重构
    /// 继承OptimizedBaseRepository获得缓存和性能优化，实现验方特有业务方法
    /// </summary>
    public class FormulaRepository : OptimizedBaseRepository<LYBT.Entities.Formula.Formula>, IFormulaRepository
    {
        public FormulaRepository(
            AppDbContext context,
            ILogger<FormulaRepository> logger,
            IMemoryCache cache) : base(context, logger, cache)
        {
        }

        // 注意：基础CRUD方法由OptimizedBaseRepository提供，带有缓存优化
        // GetAllAsync, GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync等都由基类实现

        public async Task<List<LYBT.Entities.Formula.Formula>> GetTemplatesAsync()
        {
            var cacheKey = $"{CacheKeyPrefix}templates";

            if (_cache.TryGetValue<List<LYBT.Entities.Formula.Formula>>(cacheKey, out var cached) && cached != null)
            {
                _logger.LogDebug("从缓存获取验方模板列表");
                return cached;
            }

            var templates = await _dbSet
                .Where(f => f.Status == CommonStatus.Enabled)
                .ToListAsync();

            _cache.Set(cacheKey, templates, DefaultCacheDuration);
            return templates;
        }
    }
}
