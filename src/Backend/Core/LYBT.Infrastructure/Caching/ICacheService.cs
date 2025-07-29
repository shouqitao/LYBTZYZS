namespace LYBT.Infrastructure.Caching {

    /// <summary>
    /// 缓存服务接口
    /// </summary>
    public interface ICacheService {

        /// <summary>
        /// 获取缓存值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存值</returns>
        Task<T?> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// 设置缓存值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expiry">过期时间</param>
        /// <returns>是否成功</returns>
        Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;

        /// <summary>
        /// 删除缓存值
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否成功</returns>
        Task<bool> RemoveAsync(string key);

        /// <summary>
        /// 检查键是否存在
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否存在</returns>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// 批量删除缓存（根据模式）
        /// </summary>
        /// <param name="pattern">键模式</param>
        /// <returns>删除的键数量</returns>
        Task<long> RemoveByPatternAsync(string pattern);

        /// <summary>
        /// 获取或设置缓存（如果不存在则执行工厂方法）
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">值工厂方法</param>
        /// <param name="expiry">过期时间</param>
        /// <returns>缓存值</returns>
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiry = null) where T : class;

        /// <summary>
        /// 刷新缓存过期时间
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="expiry">新的过期时间</param>
        /// <returns>是否成功</returns>
        Task<bool> RefreshAsync(string key, TimeSpan expiry);
    }
}