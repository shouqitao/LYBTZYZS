namespace LYBT.WebAPI.Services
{
    /// <summary>
    /// 智能缓存服务接口
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// 获取或设置缓存
        /// </summary>
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

        /// <summary>
        /// 获取缓存
        /// </summary>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// 设置缓存
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// 移除缓存
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// 根据模式移除缓存
        /// </summary>
        Task RemoveByPatternAsync(string pattern);

        /// <summary>
        /// 根据标签移除缓存
        /// </summary>
        Task RemoveByTagAsync(string tag);

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        Task<CacheStatistics> GetStatisticsAsync();

        /// <summary>
        /// 生成标准化缓存键
        /// </summary>
        string GenerateKey(string module, string method, params object[] parameters);

        /// <summary>
        /// 生成分页查询缓存键
        /// </summary>
        string GeneratePagedKey(string module, int page, int pageSize, string? filter = null);

        /// <summary>
        /// 生成列表查询缓存键  
        /// </summary>
        string GenerateListKey(string module, string? filter = null);
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStatistics
    {
        public int TotalKeys { get; set; }
        public long TotalMemoryUsed { get; set; }
        public int HitCount { get; set; }
        public int MissCount { get; set; }
        public double HitRate { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public Dictionary<string, int> KeysByModule { get; set; } = new();
    }
}