#nullable enable

using LYBT.Infrastructure.Caching.Interfaces;
using LYBT.Shared.Interfaces.Caching;

namespace LYBT.Infrastructure.Caching.Adapters
{
    /// <summary>
    /// ISimplifiedCacheService到ICacheService的适配器
    /// </summary>
    /// <remarks>
    /// <para>过渡性适配器: 确保现有ISimplifiedCacheService使用者可以无缝迁移</para>
    /// <para>兼容性: 完全兼容ISimplifiedCacheService的8个方法</para>
    /// <para>逐步淘汰: 配合ISimplifiedCacheService的Obsolete标记</para>
    /// <para>迁移路径: 现有代码 → SimplifiedCacheServiceAdapter → 直接使用ICacheService</para>
    /// </remarks>
    [System.Obsolete("This adapter is for transitional compatibility. Use ICacheService directly.", false)]
    public class SimplifiedCacheServiceAdapter : ISimplifiedCacheService
    {
        private readonly ICacheService _cacheService;

        public SimplifiedCacheServiceAdapter(ICacheService cacheService)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        #region ISimplifiedCacheService 同步操作

        public T? Get<T>(string key)
        {
            return _cacheService.Get<T>(key);
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            _cacheService.Set(key, value, expiration);
        }

        public bool Remove(string key)
        {
            return _cacheService.Remove(key);
        }

        public void Clear()
        {
            _cacheService.Clear();
        }

        #endregion

        #region ISimplifiedCacheService 异步操作

        public Task<T?> GetAsync<T>(string key)
        {
            return _cacheService.GetAsync<T>(key);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            return _cacheService.SetAsync(key, value, expiration);
        }

        public Task<bool> RemoveAsync(string key)
        {
            return _cacheService.RemoveAsync(key);
        }

        public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            return _cacheService.GetOrSetAsync(key, factory, expiration);
        }

        #endregion
    }

    /// <summary>
    /// ICacheService到ISimplifiedCacheService的反向适配器
    /// </summary>
    /// <remarks>
    /// <para>反向适配: 将新的ICacheService包装为旧的ISimplifiedCacheService接口</para>
    /// <para>使用场景: 需要注入ISimplifiedCacheService但实际使用ICacheService实现的情况</para>
    /// <para>功能限制: 只暴露ISimplifiedCacheService的8个核心方法</para>
    /// </remarks>
    public class CacheServiceToSimplifiedAdapter : ISimplifiedCacheService
    {
        private readonly ICacheService _cacheService;

        public CacheServiceToSimplifiedAdapter(ICacheService cacheService)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        #region ISimplifiedCacheService 同步操作

        public T? Get<T>(string key)
        {
            return _cacheService.Get<T>(key);
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            _cacheService.Set(key, value, expiration);
        }

        public bool Remove(string key)
        {
            return _cacheService.Remove(key);
        }

        public void Clear()
        {
            _cacheService.Clear();
        }

        #endregion

        #region ISimplifiedCacheService 异步操作

        public Task<T?> GetAsync<T>(string key)
        {
            return _cacheService.GetAsync<T>(key);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            return _cacheService.SetAsync(key, value, expiration);
        }

        public Task<bool> RemoveAsync(string key)
        {
            return _cacheService.RemoveAsync(key);
        }

        public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            return _cacheService.GetOrSetAsync(key, factory, expiration);
        }

        #endregion
    }
}