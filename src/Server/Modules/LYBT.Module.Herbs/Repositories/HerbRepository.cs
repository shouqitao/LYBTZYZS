using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

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

        /// <inheritdoc/>
        public async Task<PagedResult<Herb>> GetPagedAsync(HerbSearchDto query)
        {
            var queryable = _context.Herbs.AsNoTracking().Where(h => !h.IsDeleted);

            if (!string.IsNullOrEmpty(query.Keyword))
            {
                queryable = queryable.Where(h =>
                    h.Name.Contains(query.Keyword) ||
                    h.PinYinCode.Contains(query.Keyword) ||
                    h.PinYinCode.Contains(query.Keyword));
            }

            // 简化查询条件，只保留基本搜索

            var total = await queryable.CountAsync();
            var items = await queryable
                .OrderBy(h => h.Name)
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<Herb>(items, total, query.PageIndex, query.PageSize);
        }

        /// <inheritdoc/>
        public async Task<List<Herb>> SearchAsync(string keyword, int maxResults = 50)
        {
            var queryable = _context.Herbs.AsNoTracking().Where(h => !h.IsDeleted);

            if (!string.IsNullOrEmpty(keyword))
            {
                queryable = queryable.Where(h =>
                    h.Name.Contains(keyword) ||
                    h.PinYinCode.Contains(keyword) ||
                    h.PinYinCode.Contains(keyword) ||
                    h.Effect.Contains(keyword));
            }

            return await queryable
                .OrderBy(h => h.Name)
                .Take(maxResults)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<Herb>> GetByIdsAsync(List<Guid> ids)
        {
            return await _context.Herbs
                .AsNoTracking()
                .Where(h => ids.Contains(h.Id) && !h.IsDeleted)
                .OrderBy(h => h.Name)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<Herb>> GetByCategoryAsync(string category)
        {
            return await _context.Herbs
                .AsNoTracking()
                .Where(h => h.PinYinCode == category && !h.IsDeleted)
                .OrderBy(h => h.Name)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Herbs
                .AsNoTracking()
                .AnyAsync(h => h.Name == name && !h.IsDeleted);
        }

    }
}
