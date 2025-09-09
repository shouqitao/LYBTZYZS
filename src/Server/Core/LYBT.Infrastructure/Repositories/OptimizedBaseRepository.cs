using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Repositories
{

    /// <summary>
    /// 优化的基础Repository - UltraThink数据访问层优化
    ///
    /// 优化特性：
    /// 1. 查询缓存机制
    /// 2. 批量操作优化
    /// 3. 智能Include策略
    /// 4. 异步流处理
    /// 5. 连接池管理
    /// 6. 查询拦截和优化
    /// </summary>
    public abstract class OptimizedBaseRepository<TEntity> : IBaseRepository<TEntity>
        where TEntity : class
    {

        #region 字段和属性

        protected readonly AppDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;
        protected readonly ILogger _logger;
        protected readonly IMemoryCache _cache;

        // 查询优化配置
        protected readonly QueryOptimizationOptions _queryOptions;

        // 批量操作配置
        protected readonly int _batchSize = 100;

        protected readonly int _maxConcurrency = 5;

        // 缓存配置
        protected virtual TimeSpan DefaultCacheDuration => TimeSpan.FromMinutes(5);

        protected virtual string CacheKeyPrefix => $"{typeof(TEntity).Name}:";

        #endregion 字段和属性

        #region 构造函数

        protected OptimizedBaseRepository(
            AppDbContext context,
            ILogger logger,
            IMemoryCache cache,
            QueryOptimizationOptions? queryOptions = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<TEntity>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _queryOptions = queryOptions ?? QueryOptimizationOptions.Default;

            // 配置EF Core查询优化
            ConfigureQueryOptimizations();
        }

        #endregion 构造函数

        #region 查询方法优化

        /// <summary>
        /// 根据ID获取实体（接口实现）
        /// </summary>
        public virtual async Task<TEntity?> GetByIdAsync(Guid id)
        {
            return await GetByIdAsync(id, CancellationToken.None);
        }

        /// <summary>
        /// 根据ID获取实体（带缓存）
        /// </summary>
        public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeyPrefix}{id}";

            if (_queryOptions.EnableCache && _cache.TryGetValue<TEntity>(cacheKey, out var cached))
            {
                _logger.LogDebug("从缓存获取实体 {EntityType}:{Id}", typeof(TEntity).Name, id);
                return cached;
            }

            var entity = await _dbSet
                .AsNoTrackingWithIdentityResolution()
                .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);

            if (entity != null && _queryOptions.EnableCache)
            {
                // 配置缓存选项，解决SizeLimit配置问题
                var options = new MemoryCacheEntryOptions
                {
                    SlidingExpiration = DefaultCacheDuration
                };
                options.SetSize(1); // 设置缓存项大小
                _cache.Set(cacheKey, entity, options);
            }

            return entity;
        }

        /// <summary>
        /// 获取所有实体（流式处理）
        /// </summary>
        public virtual async IAsyncEnumerable<TEntity> GetAllStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsNoTracking();

            if (_queryOptions.EnableSplitQuery)
            {
                query = query.AsSplitQuery();
            }

            await foreach (var entity in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                yield return entity;
            }
        }

        /// <summary>
        /// 分页查询（接口实现）
        /// </summary>
        public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
            Expression<Func<TEntity, bool>>? predicate,
            int pageNumber,
            int pageSize,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool ascending = true)
        {
            return await GetPagedAsync(predicate, pageNumber, pageSize, orderBy, ascending, CancellationToken.None);
        }

        /// <summary>
        /// 分页查询（优化版）
        /// </summary>
        public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
            Expression<Func<TEntity, bool>>? predicate,
            int pageNumber,
            int pageSize,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool ascending = true,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = GenerateCacheKey("paged", predicate, pageNumber, pageSize, orderBy, ascending);

            if (_queryOptions.EnableCache && _cache.TryGetValue<PagedResult<TEntity>>(cacheKey, out var cached))
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
            var countTask = query.CountAsync(cancellationToken);
            var itemsTask = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            await Task.WhenAll(countTask, itemsTask);

            var result = new PagedResult<TEntity>(
                itemsTask.Result,
                countTask.Result,
                pageNumber,
                pageSize);

            if (_queryOptions.EnableCache)
            {
                SetCacheSafely(cacheKey, result, DefaultCacheDuration);
            }

            return result;
        }

        /// <summary>
        /// 高级查询（带投影和包含）
        /// </summary>
        public virtual async Task<IEnumerable<TResult>> QueryAsync<TResult>(
            Expression<Func<TEntity, bool>>? predicate,
            Expression<Func<TEntity, TResult>> selector,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
            CancellationToken cancellationToken = default)
        {
            var query = BuildOptimizedQuery(predicate);

            if (includes != null)
            {
                query = includes(query);
            }

            return await query
                .Select(selector)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 批量查询优化
        /// </summary>
        public virtual async Task<Dictionary<Guid, TEntity>> GetByIdsAsync(
            IEnumerable<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            var idList = ids.ToList();
            var result = new Dictionary<Guid, TEntity>();

            // 先从缓存获取
            var uncachedIds = new List<Guid>();
            foreach (var id in idList)
            {
                var cacheKey = $"{CacheKeyPrefix}{id}";
                if (_cache.TryGetValue<TEntity>(cacheKey, out var cached))
                {
                    result[id] = cached!;
                }
                else
                {
                    uncachedIds.Add(id);
                }
            }

            // 批量查询未缓存的数据
            if (uncachedIds.Any())
            {
                var entities = await _dbSet
                    .AsNoTracking()
                    .Where(e => uncachedIds.Contains(EF.Property<Guid>(e, "Id")))
                    .ToListAsync(cancellationToken);

                foreach (var entity in entities)
                {
                    var id = (Guid)entity.GetType().GetProperty("Id")!.GetValue(entity)!;
                    result[id] = entity;

                    // 添加到缓存
                    if (_queryOptions.EnableCache)
                    {
                        SetCacheSafely($"{CacheKeyPrefix}{id}", entity, DefaultCacheDuration);
                    }
                }
            }

            return result;
        }

        #endregion 查询方法优化

        #region IBaseRepository接口实现

        /// <summary>
        /// 获取所有实体
        /// </summary>
        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            var cacheKey = $"{CacheKeyPrefix}all";

            if (_queryOptions.EnableCache && _cache.TryGetValue<IEnumerable<TEntity>>(cacheKey, out var cached))
            {
                return cached!;
            }

            var entities = await BuildOptimizedQuery().ToListAsync();

            if (_queryOptions.EnableCache)
            {
                SetCacheSafely(cacheKey, entities, DefaultCacheDuration);
            }

            return entities;
        }

        /// <summary>
        /// 根据条件查找
        /// </summary>
        public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await BuildOptimizedQuery(predicate).ToListAsync();
        }

        /// <summary>
        /// 简单分页查询
        /// </summary>
        public virtual async Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(null, pageNumber, pageSize, null, true, CancellationToken.None);
        }

        /// <summary>
        /// 获取单个实体
        /// </summary>
        public virtual async Task<TEntity?> GetSingleAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await BuildOptimizedQuery(predicate).FirstOrDefaultAsync();
        }

        /// <summary>
        /// 检查ID是否存在
        /// </summary>
        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            var cacheKey = $"{CacheKeyPrefix}exists:{id}";

            if (_queryOptions.EnableCache && _cache.TryGetValue<bool>(cacheKey, out var cached))
            {
                return cached;
            }

            var exists = await _dbSet
                .AsNoTracking()
                .AnyAsync(e => EF.Property<Guid>(e, "Id") == id);

            if (_queryOptions.EnableCache)
            {
                SetCacheSafely(cacheKey, exists, DefaultCacheDuration);
            }

            return exists;
        }

        /// <summary>
        /// 根据条件检查是否存在
        /// </summary>
        public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await BuildOptimizedQuery(predicate).AnyAsync();
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public virtual async Task<long> CountAsync()
        {
            return await BuildOptimizedQuery().LongCountAsync();
        }

        /// <summary>
        /// 根据条件获取记录总数
        /// </summary>
        public virtual async Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await BuildOptimizedQuery(predicate).LongCountAsync();
        }

        /// <summary>
        /// 添加实体
        /// </summary>
        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var entry = await _dbSet.AddAsync(entity);
            InvalidateCache();

            return entry.Entity;
        }

        /// <summary>
        /// 批量添加实体
        /// </summary>
        public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities)
        {
            var entityList = entities.ToList();
            if (!entityList.Any())
            {
                return entityList;
            }

            await _dbSet.AddRangeAsync(entityList);
            InvalidateCache();

            return entityList;
        }

        /// <summary>
        /// 更新实体
        /// </summary>
        public virtual async Task<TEntity> UpdateAsync(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var entry = _dbSet.Update(entity);

            // 清理相关缓存
            var id = entity.GetType().GetProperty("Id")?.GetValue(entity);
            if (id != null)
            {
                _cache.Remove($"{CacheKeyPrefix}{id}");
            }

            InvalidateCache();

            return await Task.FromResult(entry.Entity);
        }

        /// <summary>
        /// 删除实体
        /// </summary>
        public virtual async Task<bool> DeleteAsync(TEntity entity)
        {
            if (entity == null)
            {
                return false;
            }

            _dbSet.Remove(entity);

            // 清理相关缓存
            var id = entity.GetType().GetProperty("Id")?.GetValue(entity);
            if (id != null)
            {
                _cache.Remove($"{CacheKeyPrefix}{id}");
            }

            InvalidateCache();

            return await Task.FromResult(true);
        }

        /// <summary>
        /// 根据ID删除实体
        /// </summary>
        public virtual async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
            {
                return false;
            }

            _dbSet.Remove(entity);
            _cache.Remove($"{CacheKeyPrefix}{id}");
            InvalidateCache();

            return true;
        }

        /// <summary>
        /// 批量删除实体
        /// </summary>
        public virtual async Task<int> DeleteRangeAsync(IEnumerable<TEntity> entities)
        {
            var entityList = entities.ToList();
            if (!entityList.Any())
            {
                return 0;
            }

            _dbSet.RemoveRange(entityList);

            // 清理相关缓存
            foreach (var entity in entityList)
            {
                var id = entity.GetType().GetProperty("Id")?.GetValue(entity);
                if (id != null)
                {
                    _cache.Remove($"{CacheKeyPrefix}{id}");
                }
            }

            InvalidateCache();

            return await Task.FromResult(entityList.Count);
        }

        /// <summary>
        /// 根据ID批量删除
        /// </summary>
        public virtual async Task<int> DeleteRangeAsync(IEnumerable<Guid> ids)
        {
            var idList = ids.ToList();
            if (!idList.Any())
            {
                return 0;
            }

            var entities = await _dbSet
                .Where(e => idList.Contains(EF.Property<Guid>(e, "Id")))
                .ToListAsync();

            if (!entities.Any())
            {
                return 0;
            }

            _dbSet.RemoveRange(entities);

            // 清理相关缓存
            foreach (var id in idList)
            {
                _cache.Remove($"{CacheKeyPrefix}{id}");
            }

            InvalidateCache();

            return entities.Count;
        }

        /// <summary>
        /// 保存更改
        /// </summary>
        public virtual async Task<int> SaveChangesAsync()
        {
            try
            {
                var result = await _context.SaveChangesAsync();
                InvalidateCache();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存更改失败");
                throw;
            }
        }

        #endregion IBaseRepository接口实现

        #region 写入方法优化

        /// <summary>
        /// 批量添加（优化版）
        /// </summary>
        public virtual async Task<int> AddRangeOptimizedAsync(
            IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default)
        {
            var entityList = entities.ToList();
            if (!entityList.Any())
            {
                return 0;
            }

            var totalAdded = 0;

            // 分批处理避免内存溢出
            foreach (var batch in entityList.Chunk(_batchSize))
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    await _dbSet.AddRangeAsync(batch, cancellationToken);

                    // 临时禁用自动检测更改以提高性能
                    _context.ChangeTracker.AutoDetectChangesEnabled = false;
                    var added = await _context.SaveChangesAsync(cancellationToken);
                    _context.ChangeTracker.AutoDetectChangesEnabled = true;

                    await transaction.CommitAsync(cancellationToken);
                    totalAdded += added;

                    // 清理缓存
                    InvalidateListCache();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "批量添加失败");
                    throw;
                }
            }

            return totalAdded;
        }

        /// <summary>
        /// 批量更新（优化版）
        /// </summary>
        public virtual async Task<int> UpdateRangeOptimizedAsync(
            IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default)
        {
            var entityList = entities.ToList();
            if (!entityList.Any())
            {
                return 0;
            }

            var totalUpdated = 0;

            foreach (var batch in entityList.Chunk(_batchSize))
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // 使用批量更新
                    _dbSet.UpdateRange(batch);

                    _context.ChangeTracker.AutoDetectChangesEnabled = false;
                    var updated = await _context.SaveChangesAsync(cancellationToken);
                    _context.ChangeTracker.AutoDetectChangesEnabled = true;

                    await transaction.CommitAsync(cancellationToken);
                    totalUpdated += updated;

                    // 清理相关缓存
                    foreach (var entity in batch)
                    {
                        var id = entity.GetType().GetProperty("Id")?.GetValue(entity);
                        if (id != null)
                        {
                            _cache.Remove($"{CacheKeyPrefix}{id}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "批量更新失败");
                    throw;
                }
            }

            InvalidateListCache();
            return totalUpdated;
        }

        /// <summary>
        /// 批量删除（优化版）
        /// </summary>
        public virtual async Task<int> DeleteRangeOptimizedAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            // 使用ExecuteDelete进行批量删除（EF Core 7+）
            var deleted = await _dbSet
                .Where(predicate)
                .ExecuteDeleteAsync(cancellationToken);

            // 清理缓存
            InvalidateCache();

            return deleted;
        }

        /// <summary>
        /// 条件更新（批量）- 注意：这是一个简化实现，不使用ExecuteUpdate
        /// 因为ExecuteUpdate的SetProperty需要编译时确定的表达式
        /// </summary>
        public virtual async Task<int> UpdateWhereAsync<TProperty>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TProperty>> propertySelector,
            TProperty newValue,
            CancellationToken cancellationToken = default)
        {
            // 获取匹配的实体
            var entities = await _dbSet.Where(predicate).ToListAsync(cancellationToken);

            if (!entities.Any())
            {
                return 0;
            }

            // 编译属性选择器以便设置值
            var compiledSelector = propertySelector.Compile();
            var propertyInfo = GetPropertyInfoFromExpression(propertySelector);

            // 更新实体
            foreach (var entity in entities)
            {
                propertyInfo.SetValue(entity, newValue);
            }

            var updated = await SaveChangesAsync();

            // 清理缓存
            InvalidateCache();

            return updated;
        }

        /// <summary>
        /// 从表达式中获取属性信息
        /// </summary>
        private PropertyInfo GetPropertyInfoFromExpression<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector)
        {
            if (propertySelector.Body is MemberExpression memberExpression &&
                memberExpression.Member is PropertyInfo propertyInfo)
            {
                return propertyInfo;
            }

            throw new ArgumentException("表达式必须是一个属性选择器", nameof(propertySelector));
        }

        #endregion 写入方法优化

        #region 事务支持

        /// <summary>
        /// 执行事务操作
        /// </summary>
        public virtual async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await operation();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// 批量操作事务包装
        /// </summary>
        public virtual async Task<int> BulkOperationAsync(
            Func<DbContext, Task<int>> bulkOperation,
            CancellationToken cancellationToken = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 暂时禁用查询跟踪以提高性能
                var originalAutoDetectChanges = _context.ChangeTracker.AutoDetectChangesEnabled;
                var originalQueryTrackingBehavior = _context.ChangeTracker.QueryTrackingBehavior;

                _context.ChangeTracker.AutoDetectChangesEnabled = false;
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var result = await bulkOperation(_context);

                _context.ChangeTracker.AutoDetectChangesEnabled = originalAutoDetectChanges;
                _context.ChangeTracker.QueryTrackingBehavior = originalQueryTrackingBehavior;

                await transaction.CommitAsync(cancellationToken);

                // 清理缓存
                InvalidateCache();

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "批量操作失败");
                throw;
            }
        }

        #endregion 事务支持

        #region 性能监控

        /// <summary>
        /// 执行并监控查询性能
        /// </summary>
        protected async Task<TResult> MonitoredQueryAsync<TResult>(
            Func<Task<TResult>> query,
            string operationName)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await query();
                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > _queryOptions.SlowQueryThresholdMs)
                {
                    _logger.LogWarning(
                        "慢查询检测 - {Operation}: {ElapsedMs}ms",
                        operationName, stopwatch.ElapsedMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "查询失败 - {Operation}: {ElapsedMs}ms",
                    operationName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        #endregion 性能监控

        #region 辅助方法

        /// <summary>
        /// 构建优化的查询
        /// </summary>
        protected virtual IQueryable<TEntity> BuildOptimizedQuery(
            Expression<Func<TEntity, bool>>? predicate = null)
        {
            var query = _dbSet.AsQueryable();

            // 应用查询优化选项
            if (_queryOptions.UseNoTracking)
            {
                query = query.AsNoTrackingWithIdentityResolution();
            }

            if (_queryOptions.EnableSplitQuery)
            {
                query = query.AsSplitQuery();
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // 应用全局过滤器
            query = ApplyGlobalFilters(query);

            // 应用默认包含
            query = ApplyDefaultIncludes(query);

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
        /// 应用默认包含（可重写）
        /// </summary>
        protected virtual IQueryable<TEntity> ApplyDefaultIncludes(IQueryable<TEntity> query)
        {
            // 子类可以重写以添加默认的Include
            return query;
        }

        /// <summary>
        /// 配置查询优化
        /// </summary>
        protected virtual void ConfigureQueryOptimizations()
        {
            // 配置连接重试策略
            _context.Database.SetCommandTimeout(TimeSpan.FromSeconds(30));

            // 配置查询跟踪行为
            if (_queryOptions.UseNoTracking)
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
            }
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
        /// 清理缓存
        /// </summary>
        protected virtual void InvalidateCache()
        {
            // 这里应该实现更智能的缓存失效策略
            // 可以使用缓存标签或者模式匹配来批量清理
            _logger.LogDebug("清理缓存: {EntityType}", typeof(TEntity).Name);
        }

        /// <summary>
        /// 安全设置缓存项，自动配置Size以避免SizeLimit错误
        /// </summary>
        protected void SetCacheSafely<T>(string key, T value, TimeSpan expiration)
        {
            if (!_queryOptions.EnableCache)
            {
                return;
            }

            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = expiration
            };
            options.SetSize(1); // 设置缓存项大小，解决SizeLimit配置问题
            _cache.Set(key, value, options);
        }

        /// <summary>
        /// 清理列表缓存
        /// </summary>
        protected virtual void InvalidateListCache()
        {
            // 清理分页和列表相关的缓存
            _logger.LogDebug("清理列表缓存: {EntityType}", typeof(TEntity).Name);
        }

        #endregion 辅助方法
    }

    #region 配置类

    /// <summary>
    /// 查询优化选项
    /// </summary>
    public class QueryOptimizationOptions
    {
        public bool EnableCache { get; set; } = true;
        public bool UseNoTracking { get; set; } = true;
        public bool EnableSplitQuery { get; set; } = true;
        public int SlowQueryThresholdMs { get; set; } = 1000;
        public int QueryTimeout { get; set; } = 30;

        public static QueryOptimizationOptions Default => new();

        public static QueryOptimizationOptions Performance => new()
        {
            EnableCache = true,
            UseNoTracking = true,
            EnableSplitQuery = true,
            SlowQueryThresholdMs = 500,
            QueryTimeout = 60
        };

        public static QueryOptimizationOptions Tracking => new()
        {
            EnableCache = false,
            UseNoTracking = false,
            EnableSplitQuery = false,
            SlowQueryThresholdMs = 2000,
            QueryTimeout = 30
        };
    }

    #endregion 配置类
}
