using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.UltraThink
{
    /// <summary>
    /// UltraThink架构 - 查询服务基类
    /// 职责单一：专注于数据查询操作
    /// 无副作用：不修改任何业务状态
    /// 高性能：支持异步、分页、缓存
    /// </summary>
    public abstract class QueryServiceBase<TEntity> : IQueryService<TEntity> where TEntity : class
    {
        protected readonly ILogger Logger;
        private readonly Dictionary<string, object> _cache = new();
        private readonly TimeSpan _defaultCacheExpiration = TimeSpan.FromMinutes(5);

        protected QueryServiceBase(ILogger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 基础查询方法

        /// <summary>
        /// 根据ID获取实体
        /// </summary>
        public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = GetCacheKey(nameof(GetByIdAsync), id);
                if (TryGetFromCache<TEntity>(cacheKey, out var cached))
                {
                    Logger.LogDebug("从缓存获取实体 {EntityType} ID: {Id}", typeof(TEntity).Name, id);
                    return cached;
                }

                var entity = await GetByIdInternalAsync(id, cancellationToken);
                
                if (entity != null)
                {
                    AddToCache(cacheKey, entity);
                }

                return entity;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取实体失败 {EntityType} ID: {Id}", typeof(TEntity).Name, id);
                throw;
            }
        }

        /// <summary>
        /// 获取所有实体
        /// </summary>
        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = GetCacheKey(nameof(GetAllAsync));
                if (TryGetFromCache<IEnumerable<TEntity>>(cacheKey, out var cached))
                {
                    Logger.LogDebug("从缓存获取所有 {EntityType}", typeof(TEntity).Name);
                    return cached!;
                }

                var entities = await GetAllInternalAsync(cancellationToken);
                var result = entities.ToList();
                
                AddToCache(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取所有实体失败 {EntityType}", typeof(TEntity).Name);
                throw;
            }
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100; // 限制最大页面大小

                var cacheKey = GetCacheKey(nameof(GetPagedAsync), pageNumber, pageSize);
                if (TryGetFromCache<PagedResult<TEntity>>(cacheKey, out var cached))
                {
                    Logger.LogDebug("从缓存获取分页数据 {EntityType} 页码: {Page}, 大小: {Size}", 
                        typeof(TEntity).Name, pageNumber, pageSize);
                    return cached!;
                }

                var result = await GetPagedInternalAsync(pageNumber, pageSize, cancellationToken);
                
                AddToCache(cacheKey, result, TimeSpan.FromMinutes(1)); // 分页数据缓存时间较短
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "分页查询失败 {EntityType} 页码: {Page}, 大小: {Size}", 
                    typeof(TEntity).Name, pageNumber, pageSize);
                throw;
            }
        }

        /// <summary>
        /// 条件查询
        /// </summary>
        public virtual async Task<IEnumerable<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 条件查询通常不缓存，因为条件千变万化
                var entities = await FindInternalAsync(predicate, cancellationToken);
                return entities.ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "条件查询失败 {EntityType}", typeof(TEntity).Name);
                throw;
            }
        }

        /// <summary>
        /// 搜索
        /// </summary>
        public virtual async Task<IEnumerable<TEntity>> SearchAsync(
            string searchTerm, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetAllAsync(cancellationToken);
                }

                var cacheKey = GetCacheKey(nameof(SearchAsync), searchTerm.ToLower());
                if (TryGetFromCache<IEnumerable<TEntity>>(cacheKey, out var cached))
                {
                    Logger.LogDebug("从缓存获取搜索结果 {EntityType} 搜索词: {Term}", 
                        typeof(TEntity).Name, searchTerm);
                    return cached!;
                }

                var result = await SearchInternalAsync(searchTerm, cancellationToken);
                var entities = result.ToList();
                
                AddToCache(cacheKey, entities, TimeSpan.FromMinutes(2));
                return entities;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "搜索失败 {EntityType} 搜索词: {Term}", 
                    typeof(TEntity).Name, searchTerm);
                throw;
            }
        }

        /// <summary>
        /// 获取数量
        /// </summary>
        public virtual async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = GetCacheKey(nameof(GetCountAsync));
                if (TryGetFromCache<int>(cacheKey, out var cached))
                {
                    Logger.LogDebug("从缓存获取数量 {EntityType}", typeof(TEntity).Name);
                    return cached;
                }

                var count = await GetCountInternalAsync(cancellationToken);
                
                AddToCache(cacheKey, count);
                return count;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取数量失败 {EntityType}", typeof(TEntity).Name);
                throw;
            }
        }

        /// <summary>
        /// 检查是否存在
        /// </summary>
        public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await GetByIdAsync(id, cancellationToken);
                return entity != null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "检查存在性失败 {EntityType} ID: {Id}", typeof(TEntity).Name, id);
                throw;
            }
        }

        #endregion

        #region 抽象方法 - 子类实现

        protected abstract Task<TEntity?> GetByIdInternalAsync(Guid id, CancellationToken cancellationToken);
        protected abstract Task<IEnumerable<TEntity>> GetAllInternalAsync(CancellationToken cancellationToken);
        protected abstract Task<PagedResult<TEntity>> GetPagedInternalAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
        protected abstract Task<IEnumerable<TEntity>> FindInternalAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken);
        protected abstract Task<IEnumerable<TEntity>> SearchInternalAsync(string searchTerm, CancellationToken cancellationToken);
        protected abstract Task<int> GetCountInternalAsync(CancellationToken cancellationToken);

        #endregion

        #region 缓存管理

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            Logger.LogDebug("清除缓存 {EntityType}", typeof(TEntity).Name);
        }

        /// <summary>
        /// 获取缓存键
        /// </summary>
        protected string GetCacheKey(string method, params object[] parameters)
        {
            var key = $"{typeof(TEntity).Name}:{method}";
            if (parameters.Length > 0)
            {
                key += ":" + string.Join(":", parameters.Select(p => p?.ToString() ?? "null"));
            }
            return key;
        }

        /// <summary>
        /// 尝试从缓存获取
        /// </summary>
        protected bool TryGetFromCache<T>(string key, out T? value)
        {
            if (_cache.TryGetValue(key, out var cached) && cached is CacheEntry entry)
            {
                if (entry.ExpiresAt > DateTime.UtcNow)
                {
                    value = (T)entry.Value;
                    return true;
                }
                _cache.Remove(key);
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 添加到缓存
        /// </summary>
        protected void AddToCache(string key, object value, TimeSpan? expiration = null)
        {
            var expiresAt = DateTime.UtcNow.Add(expiration ?? _defaultCacheExpiration);
            _cache[key] = new CacheEntry(value, expiresAt);
        }

        /// <summary>
        /// 缓存条目
        /// </summary>
        private record CacheEntry(object Value, DateTime ExpiresAt);

        #endregion
    }

    /// <summary>
    /// 查询服务接口
    /// </summary>
    public interface IQueryService<TEntity> where TEntity : class
    {
        Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
        Task<int> GetCountAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        void ClearCache();
    }

    /// <summary>
    /// 分页结果
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;
    }
}