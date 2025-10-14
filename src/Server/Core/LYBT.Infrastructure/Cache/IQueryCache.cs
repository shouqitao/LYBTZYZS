namespace LYBT.Infrastructure.Cache
{
    /// <summary>
    /// 查询缓存接口
    /// </summary>
    public interface IQueryCache
    {
        /// <summary>
        /// 获取缓存项
        /// </summary>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// 设置缓存项
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// 移除缓存项
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        Task ClearAsync();

        /// <summary>
        /// 检查缓存项是否存在
        /// </summary>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// 获取缓存项（带过期时间）
        /// </summary>
        Task<(T? Value, bool IsExpired)> GetWithExpirationAsync<T>(string key);
    }

    /// <summary>
    /// 内存查询缓存实现
    /// </summary>
    public class MemoryQueryCache : IQueryCache
    {
        private readonly Dictionary<string, CacheItem> _cache = new();
        private readonly object _lock = new object();

        private class CacheItem
        {
            public object? Value { get; set; }
            public DateTime Expiration { get; set; }
        }

        public Task<T?> GetAsync<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Task.FromResult<T?>(default);

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var item) && item.Expiration > DateTime.Now)
                {
                    return Task.FromResult((T?)item.Value);
                }

                _cache.Remove(key);
                return Task.FromResult<T?>(default);
            }
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Task.CompletedTask;

            lock (_lock)
            {
                _cache[key] = new CacheItem
                {
                    Value = value,
                    Expiration = DateTime.Now.Add(expiration ?? TimeSpan.FromMinutes(5))
                };
            }

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                lock (_lock)
                {
                    _cache.Remove(key);
                }
            }

            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            lock (_lock)
            {
                _cache.Clear();
            }

            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Task.FromResult(false);

            lock (_lock)
            {
                return Task.FromResult(_cache.ContainsKey(key) && _cache[key].Expiration > DateTime.Now);
            }
        }

        public Task<(T? Value, bool IsExpired)> GetWithExpirationAsync<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Task.FromResult<(T? Value, bool IsExpired)>((default, false));

            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var item))
                {
                    var isExpired = item.Expiration <= DateTime.Now;
                    if (isExpired)
                    {
                        _cache.Remove(key);
                        return Task.FromResult<(T? Value, bool IsExpired)>((default, true));
                    }

                    return Task.FromResult(((T?)item.Value, false));
                }

                return Task.FromResult<(T? Value, bool IsExpired)>((default, false));
            }
        }
    }
}