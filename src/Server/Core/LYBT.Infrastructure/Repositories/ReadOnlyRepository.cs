using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Repositories
{
    /// <summary>
    /// 只读仓储基类 - 用于QueryService数据访问
    /// 提供缓存优化的查询功能，支持AutoMapper ProjectTo进行DTO映射
    /// </summary>
    public abstract class ReadOnlyRepository<TEntity> : IReadOnlyRepository<TEntity>
        where TEntity : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;
        protected readonly IMapper _mapper;
        protected readonly ILogger _logger;
        protected readonly IMemoryCache _cache;

        // 缓存配置
        protected virtual TimeSpan DefaultCacheDuration => TimeSpan.FromMinutes(5);
        protected virtual string CacheKeyPrefix => $"{typeof(TEntity).Name}:readonly:";
        
        // 缓存穿透防护配置
        protected virtual TimeSpan NullCacheDuration => TimeSpan.FromMinutes(1); // 空结果缓存时间更短
        protected const string NullValueMarker = "__NULL__"; // 空值标记

        protected ReadOnlyRepository(
            AppDbContext context,
            IMapper mapper,
            ILogger logger,
            IMemoryCache cache)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<TEntity>();
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public virtual async Task<TEntity?> GetByIdAsync(Guid id)
        {
            var cacheKey = $"{CacheKeyPrefix}{id}";

            // 检查缓存（包括空值标记）
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                _logger.LogDebug("缓存命中 - 实体 {EntityType}:{Id}, 命中率统计已记录", typeof(TEntity).Name, id);
                
                // 缓存穿透防护：检查是否为空值标记
                if (cached is string marker && marker == NullValueMarker)
                {
                    _logger.LogDebug("缓存命中空值标记 - 实体 {EntityType}:{Id}", typeof(TEntity).Name, id);
                    return null;
                }
                
                return cached as TEntity;
            }

            _logger.LogDebug("缓存未命中 - 实体 {EntityType}:{Id}, 从数据库查询", typeof(TEntity).Name, id);
            
            var entity = await _dbSet
                .AsNoTrackingWithIdentityResolution()
                .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);

            if (entity != null)
            {
                SetCacheSafely(cacheKey, entity, DefaultCacheDuration);
                _logger.LogDebug("查询成功并缓存 - 实体 {EntityType}:{Id}", typeof(TEntity).Name, id);
            }
            else
            {
                // 缓存穿透防护：缓存空结果
                SetCacheSafely(cacheKey, NullValueMarker, NullCacheDuration);
                _logger.LogDebug("查询返回空值，已缓存空值标记 - 实体 {EntityType}:{Id}", typeof(TEntity).Name, id);
            }

            return entity;
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            var cacheKey = $"{CacheKeyPrefix}all";

            if (_cache.TryGetValue<IEnumerable<TEntity>>(cacheKey, out var cached))
            {
                _logger.LogDebug("缓存命中 - 获取全部 {EntityType} 实体", typeof(TEntity).Name);
                return cached!;
            }

            _logger.LogDebug("缓存未命中 - 获取全部 {EntityType} 实体，从数据库查询", typeof(TEntity).Name);
            var entities = await BuildOptimizedQuery().ToListAsync();
            
            // 缓存穿透防护：即使结果为空也缓存
            if (entities.Count == 0)
            {
                SetCacheSafely(cacheKey, entities, NullCacheDuration);
                _logger.LogDebug("查询返回空集合，已缓存 - {EntityType}", typeof(TEntity).Name);
            }
            else
            {
                SetCacheSafely(cacheKey, entities, DefaultCacheDuration);
                _logger.LogDebug("查询成功并缓存 {Count} 条 {EntityType} 记录", entities.Count, typeof(TEntity).Name);
            }

            return entities;
        }

        public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await BuildOptimizedQuery(predicate).ToListAsync();
        }

        public virtual async Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(null, pageNumber, pageSize);
        }

        public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
            Expression<Func<TEntity, bool>>? predicate,
            int pageNumber,
            int pageSize,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool ascending = true)
        {
            var cacheKey = GenerateCacheKey("paged", predicate, pageNumber, pageSize, orderBy, ascending);

            if (_cache.TryGetValue<PagedResult<TEntity>>(cacheKey, out var cached))
            {
                _logger.LogDebug("缓存命中 - 分页查询 {EntityType} Page:{PageNumber} Size:{PageSize}", 
                    typeof(TEntity).Name, pageNumber, pageSize);
                return cached!;
            }

            _logger.LogDebug("缓存未命中 - 分页查询 {EntityType} Page:{PageNumber} Size:{PageSize}，从数据库查询", 
                typeof(TEntity).Name, pageNumber, pageSize);

            var query = BuildOptimizedQuery(predicate);

            // 应用排序
            if (orderBy != null)
            {
                query = ascending
                    ? query.OrderBy(orderBy)
                    : query.OrderByDescending(orderBy);
            }

            // 并行执行计数和数据查询
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<TEntity>(
                items,
                totalCount,
                pageNumber,
                pageSize);

            SetCacheSafely(cacheKey, result, DefaultCacheDuration);
            return result;
        }

        public virtual async Task<TEntity?> GetSingleAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await BuildOptimizedQuery(predicate).FirstOrDefaultAsync();
        }

        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            var cacheKey = $"{CacheKeyPrefix}exists:{id}";

            if (_cache.TryGetValue<bool>(cacheKey, out var cached))
            {
                return cached;
            }

            var exists = await _dbSet
                .AsNoTracking()
                .AnyAsync(e => EF.Property<Guid>(e, "Id") == id);

            SetCacheSafely(cacheKey, exists, DefaultCacheDuration);
            return exists;
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await BuildOptimizedQuery(predicate).AnyAsync();
        }

        public virtual async Task<long> CountAsync()
        {
            return await BuildOptimizedQuery().LongCountAsync();
        }

        public virtual async Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await BuildOptimizedQuery(predicate).LongCountAsync();
        }

        /// <summary>
        /// 使用AutoMapper ProjectTo进行DTO映射的分页查询
        /// </summary>
        public virtual async Task<PagedResult<TDto>> GetPagedAsync<TDto>(
            Expression<Func<TEntity, bool>>? predicate,
            int pageNumber,
            int pageSize,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool ascending = true)
        {
            var cacheKey = GenerateCacheKey($"paged_dto_{typeof(TDto).Name}", predicate, pageNumber, pageSize, orderBy, ascending);

            if (_cache.TryGetValue<PagedResult<TDto>>(cacheKey, out var cached))
            {
                return cached!;
            }

            var query = BuildOptimizedQuery(predicate);

            // 应用排序
            if (orderBy != null)
            {
                query = ascending
                    ? query.OrderBy(orderBy)
                    : query.OrderByDescending(orderBy);
            }

            // 并行执行计数和数据查询
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var result = new PagedResult<TDto>(
                items,
                totalCount,
                pageNumber,
                pageSize);

            SetCacheSafely(cacheKey, result, DefaultCacheDuration);
            return result;
        }

        /// <summary>
        /// 使用AutoMapper ProjectTo进行DTO映射的列表查询
        /// </summary>
        public virtual async Task<List<TDto>> FindAndProjectAsync<TDto>(
            Expression<Func<TEntity, bool>>? predicate = null,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool ascending = true,
            int? take = null)
        {
            var query = BuildOptimizedQuery(predicate);

            // 应用排序
            if (orderBy != null)
            {
                query = ascending
                    ? query.OrderBy(orderBy)
                    : query.OrderByDescending(orderBy);
            }

            // 应用限制
            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            return await query
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        /// <summary>
        /// 构建优化的查询
        /// </summary>
        protected virtual IQueryable<TEntity> BuildOptimizedQuery(
            Expression<Func<TEntity, bool>>? predicate = null)
        {
            var query = _dbSet.AsNoTrackingWithIdentityResolution();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // 应用全局过滤器
            query = ApplyGlobalFilters(query);

            return query;
        }

        /// <summary>
        /// 应用全局过滤器（可重写）
        /// </summary>
        protected virtual IQueryable<TEntity> ApplyGlobalFilters(IQueryable<TEntity> query)
        {
            // 子类可以重写以应用软删除等全局过滤器
            return query;
        }

        /// <summary>
        /// 生成缓存键
        /// </summary>
        protected string GenerateCacheKey(string operation, params object?[] parameters)
        {
            var key = $"{CacheKeyPrefix}{operation}";
            foreach (var param in parameters.Where(p => p != null))
            {
                key += $":{param!.GetHashCode()}";
            }
            return key;
        }

        /// <summary>
        /// 安全设置缓存项
        /// </summary>
        protected void SetCacheSafely<T>(string key, T value, TimeSpan expiration)
        {
            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = expiration
            };
            options.SetSize(1);
            _cache.Set(key, value, options);
        }
    }
}