using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Cache;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 基于内存的缓存服务实现
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly HashSet<string> _cacheKeys = new();
        private readonly object _lockObject = new();

        public MemoryCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        #region 同步方法

        public T? Get<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return default;

            return _memoryCache.TryGetValue(key, out T? value) ? value : default;
        }

        public void Set<T>(string key, T value, TimeSpan expiration)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            _memoryCache.Set(key, value, options);
            
            lock (_lockObject)
            {
                _cacheKeys.Add(key);
            }
        }

        public void Set<T>(string key, T value, CachePolicy policy)
        {
            if (string.IsNullOrWhiteSpace(key) || policy == null)
                return;

            var options = new MemoryCacheEntryOptions();
            
            if (policy.AbsoluteExpiration.HasValue)
                options.AbsoluteExpiration = policy.AbsoluteExpiration;
            
            if (policy.SlidingExpiration.HasValue)
                options.SlidingExpiration = policy.SlidingExpiration;
            
            // 转换优先级类型
            options.Priority = ConvertPriority(policy.Priority);

            _memoryCache.Set(key, value, options);
            
            lock (_lockObject)
            {
                _cacheKeys.Add(key);
            }
        }

        public bool TryGet<T>(string key, out T? value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = default;
                return false;
            }

            return _memoryCache.TryGetValue(key, out value);
        }

        public bool Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            _memoryCache.Remove(key);
            
            lock (_lockObject)
            {
                return _cacheKeys.Remove(key);
            }
        }

        public bool Exists(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            return _memoryCache.TryGetValue(key, out _);
        }

        #endregion

        #region 异步方法

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be empty", nameof(key));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            return await _memoryCache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = expiration;
                
                lock (_lockObject)
                {
                    _cacheKeys.Add(key);
                }
                
                return await factory();
            });
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, CachePolicy policy)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be empty", nameof(key));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            return await _memoryCache.GetOrCreateAsync(key, async entry =>
            {
                if (policy.AbsoluteExpiration.HasValue)
                    entry.AbsoluteExpiration = policy.AbsoluteExpiration;
                
                if (policy.SlidingExpiration.HasValue)
                    entry.SlidingExpiration = policy.SlidingExpiration;
                
                entry.Priority = ConvertPriority(policy.Priority);
                
                lock (_lockObject)
                {
                    _cacheKeys.Add(key);
                }
                
                return await factory();
            });
        }

        #endregion

        #region 批量操作

        public void SetMany(Dictionary<string, object> items, TimeSpan expiration)
        {
            if (items == null || items.Count == 0)
                return;

            foreach (var item in items)
            {
                Set(item.Key, item.Value, expiration);
            }
        }

        public Dictionary<string, object?> GetMany(IEnumerable<string> keys)
        {
            var result = new Dictionary<string, object?>();
            
            if (keys == null)
                return result;

            foreach (var key in keys)
            {
                if (_memoryCache.TryGetValue(key, out var value))
                {
                    result[key] = value;
                }
                else
                {
                    result[key] = null;
                }
            }

            return result;
        }

        public int RemoveMany(IEnumerable<string> keys)
        {
            if (keys == null)
                return 0;

            int count = 0;
            foreach (var key in keys)
            {
                if (Remove(key))
                    count++;
            }

            return count;
        }

        #endregion

        #region 缓存管理

        public int RemoveByPattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return 0;

            List<string> keysToRemove;
            lock (_lockObject)
            {
                // 简单的通配符匹配实现
                if (pattern.Contains("*"))
                {
                    var searchPattern = pattern.Replace("*", "");
                    keysToRemove = _cacheKeys.Where(k => k.Contains(searchPattern)).ToList();
                }
                else
                {
                    keysToRemove = _cacheKeys.Where(k => k == pattern).ToList();
                }
            }

            return RemoveMany(keysToRemove);
        }

        public void Clear()
        {
            List<string> keys;
            lock (_lockObject)
            {
                keys = _cacheKeys.ToList();
                _cacheKeys.Clear();
            }

            foreach (var key in keys)
            {
                _memoryCache.Remove(key);
            }
        }

        public void ClearPartition(string partition)
        {
            if (string.IsNullOrWhiteSpace(partition))
                return;

            RemoveByPattern($"{partition}:*");
        }

        public int Cleanup()
        {
            // IMemoryCache自动处理过期项，这里返回0
            return 0;
        }

        #endregion

        #region 统计与监控

        public CacheStatistics GetStatistics()
        {
            // 返回一个简单的统计信息
            lock (_lockObject)
            {
                return new CacheStatistics
                {
                    ItemCount = _cacheKeys.Count,
                    HitCount = 0,  // 简化实现，不跟踪命中率
                    MissCount = 0
                    // HitRate是只读属性，自动计算
                };
            }
        }

        public void ResetStatistics()
        {
            // 简化实现，不跟踪统计信息
        }

        public IEnumerable<string> GetAllKeys()
        {
            lock (_lockObject)
            {
                return _cacheKeys.ToList();
            }
        }

        public int Count
        {
            get
            {
                lock (_lockObject)
                {
                    return _cacheKeys.Count;
                }
            }
        }

        #endregion

        #region 辅助方法

        private CacheItemPriority ConvertPriority(CachePriority priority)
        {
            return priority switch
            {
                CachePriority.Low => CacheItemPriority.Low,
                CachePriority.Normal => CacheItemPriority.Normal,
                CachePriority.High => CacheItemPriority.High,
                CachePriority.NeverRemove => CacheItemPriority.NeverRemove,
                _ => CacheItemPriority.Normal
            };
        }

        #endregion
    }
}