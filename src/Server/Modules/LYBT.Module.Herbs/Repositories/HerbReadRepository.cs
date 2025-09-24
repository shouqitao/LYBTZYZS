using AutoMapper;
using AutoMapper.QueryableExtensions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Repositories
{
    /// <summary>
    /// 药材只读仓储实现 - 专门为QueryService提供数据访问
    /// 继承ReadOnlyRepository获得缓存优化，实现药材特定的查询方法
    /// 使用AutoMapper ProjectTo进行高效的DTO映射
    /// </summary>
    public class HerbReadRepository : ReadOnlyRepository<LYBT.Entities.Herbs.Herb>, IHerbReadRepository
    {
        public HerbReadRepository(
            AppDbContext context,
            IMapper mapper,
            ILogger<HerbReadRepository> logger,
            IMemoryCache cache) : base(context, mapper, logger, cache)
        {
        }

        /// <summary>
        /// 应用全局过滤器 - 排除软删除记录
        /// </summary>
        protected override IQueryable<LYBT.Entities.Herbs.Herb> ApplyGlobalFilters(
            IQueryable<LYBT.Entities.Herbs.Herb> query)
        {
            // 应用软删除过滤
            return query.Where(h => !h.IsDeleted);
        }

        /// <summary>
        /// 构建基础查询 - 只查询启用状态的药材
        /// </summary>
        private IQueryable<LYBT.Entities.Herbs.Herb> BuildBaseQuery()
        {
            return BuildOptimizedQuery().Where(h => h.Status == CommonStatus.Enabled);
        }

        public async Task<HerbDto?> GetHerbDtoByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            var cacheKey = $"{CacheKeyPrefix}detail:{id}";

            if (_cache.TryGetValue<HerbDto?>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取药材详情 {Id}", id);
                return cached;
            }

            var herbDto = await BuildBaseQuery()
                .Where(h => h.Id == id)
                .ProjectTo<HerbDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            SetCacheSafely(cacheKey, herbDto, DefaultCacheDuration);
            return herbDto;
        }

        public async Task<List<HerbDto>> GetAllHerbDtosAsync()
        {
            var cacheKey = $"{CacheKeyPrefix}all_herbs";

            if (_cache.TryGetValue<List<HerbDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取所有药材列表");
                return cached!;
            }

            var herbDtos = await BuildBaseQuery()
                .OrderBy(h => h.Name)
                .ProjectTo<HerbDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, herbDtos, DefaultCacheDuration);
            return herbDtos;
        }

        public async Task<PagedResult<HerbDto>> GetPagedHerbDtosAsync(HerbSearchDto query)
        {
            query ??= new HerbSearchDto();

            var cacheKey = GenerateCacheKey("paged_herbs", query.Keyword, 
                query.MinPrice, query.MaxPrice, query.IncludeExpired,
                query.PageIndex, query.PageSize);

            if (_cache.TryGetValue<PagedResult<HerbDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取分页药材记录 Page:{PageIndex}", query.PageIndex);
                return cached!;
            }

            var queryable = BuildOptimizedQuery();

            // 关键词搜索
            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keyword = query.Keyword.Trim();
                queryable = queryable.Where(h =>
                    h.Name.Contains(keyword) ||
                    (h.PinYinCode != null && h.PinYinCode.Contains(keyword)) ||
                    (h.Origin != null && h.Origin.Contains(keyword)) ||
                    (h.Effect != null && h.Effect.Contains(keyword)));
            }

            // 价格范围筛选
            if (query.MinPrice.HasValue)
            {
                queryable = queryable.Where(h => h.Price >= query.MinPrice.Value);
            }

            if (query.MaxPrice.HasValue)
            {
                queryable = queryable.Where(h => h.Price <= query.MaxPrice.Value);
            }

            // 状态筛选 - 默认只返回启用的药材
            if (!query.IncludeExpired)
            {
                queryable = queryable.Where(h => h.Status == CommonStatus.Enabled);
            }

            // 排序：按名称排序
            queryable = queryable.OrderBy(h => h.Name);

            // 分页参数处理
            var pageIndex = Math.Max(query.PageIndex, 1);
            var pageSize = Math.Clamp(query.PageSize, 10, 100);

            // 执行分页查询并映射为DTO
            var totalCount = await queryable.CountAsync();
            var herbDtos = await queryable
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<HerbDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var result = new PagedResult<HerbDto>(
                herbDtos,
                totalCount,
                pageIndex,
                pageSize);

            SetCacheSafely(cacheKey, result, DefaultCacheDuration);
            return result;
        }

        public async Task<List<HerbDto>> SearchHerbDtosAsync(string keyword, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return await GetAllHerbDtosAsync();
            }

            var cacheKey = GenerateCacheKey("search_herbs", keyword, maxResults);

            if (_cache.TryGetValue<List<HerbDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取搜索结果 Keyword:{Keyword}", keyword);
                return cached!;
            }

            var searchTerm = keyword.Trim();
            var herbDtos = await BuildBaseQuery()
                .Where(h => h.Name.Contains(searchTerm) || 
                           (h.PinYinCode != null && h.PinYinCode.Contains(searchTerm)))
                .OrderByDescending(h => h.Name.StartsWith(searchTerm)) // 以关键词开头的排前面
                .ThenByDescending(h => h.PinYinCode != null && h.PinYinCode.StartsWith(searchTerm.ToUpper()))
                .ThenBy(h => h.Name)
                .Take(maxResults)
                .ProjectTo<HerbDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, herbDtos, TimeSpan.FromMinutes(2)); // 搜索结果较短缓存时间
            return herbDtos;
        }

        public async Task<List<HerbDto>> GetAvailableHerbDtosAsync()
        {
            var cacheKey = $"{CacheKeyPrefix}available_herbs";

            if (_cache.TryGetValue<List<HerbDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取可用药材列表");
                return cached!;
            }

            var herbDtos = await BuildOptimizedQuery()
                .Where(h => h.Status == CommonStatus.Enabled)
                .OrderBy(h => h.Name)
                .ProjectTo<HerbDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, herbDtos, DefaultCacheDuration);
            return herbDtos;
        }

        public async Task<List<HerbDto>> GetHerbDtosByIdsAsync(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return new List<HerbDto>();
            }

            var cacheKey = GenerateCacheKey("herbs_by_ids", string.Join(",", ids));

            if (_cache.TryGetValue<List<HerbDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存批量获取药材");
                return cached!;
            }

            var herbDtos = await BuildOptimizedQuery()
                .Where(h => ids.Contains(h.Id) && h.Status != CommonStatus.Disabled)
                .OrderBy(h => h.Name)
                .ProjectTo<HerbDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, herbDtos, DefaultCacheDuration);
            return herbDtos;
        }

        public async Task<List<HerbDto>> GetHerbDtosByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
            {
                return new List<HerbDto>();
            }

            var cacheKey = GenerateCacheKey("herbs_by_price_range", minPrice, maxPrice);

            if (_cache.TryGetValue<List<HerbDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取价格区间药材 {MinPrice}-{MaxPrice}", minPrice, maxPrice);
                return cached!;
            }

            var herbDtos = await BuildBaseQuery()
                .Where(h => h.Price >= minPrice && h.Price <= maxPrice)
                .OrderBy(h => h.Price)
                .ThenBy(h => h.Name)
                .ProjectTo<HerbDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, herbDtos, DefaultCacheDuration);
            return herbDtos;
        }

        public async Task<HerbDto?> GetHerbDtoByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var cacheKey = $"{CacheKeyPrefix}name:{name}";

            if (_cache.TryGetValue<HerbDto?>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取药材信息 Name:{Name}", name);
                return cached;
            }

            var herbDto = await BuildBaseQuery()
                .Where(h => h.Name == name.Trim())
                .ProjectTo<HerbDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            SetCacheSafely(cacheKey, herbDto, DefaultCacheDuration);
            return herbDto;
        }

        public async Task<List<HerbDto>> GetPopularHerbDtosAsync(int count = 20)
        {
            count = Math.Clamp(count, 1, 50);

            var cacheKey = $"{CacheKeyPrefix}popular_herbs:{count}";

            if (_cache.TryGetValue<List<HerbDto>>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取热门药材列表");
                return cached!;
            }

            // 简化实现，按名称排序。实际项目中可根据处方使用频率统计
            var herbDtos = await BuildBaseQuery()
                .OrderBy(h => h.Name)
                .Take(count)
                .ProjectTo<HerbDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            SetCacheSafely(cacheKey, herbDtos, TimeSpan.FromMinutes(30)); // 热门药材缓存30分钟
            return herbDtos;
        }
    }
}