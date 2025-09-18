/// <summary>
/// P3-Fix 缓存服务接口 - 解决测试项目编译错误
/// 最小化接口定义，仅用于编译通过
/// </summary>

namespace LYBT.Infrastructure.Interfaces
{
    /// <summary>
    /// 缓存服务接口
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// 获取缓存值
        /// </summary>
        Task<T?> GetAsync<T>(string key);
        
        /// <summary>
        /// 设置缓存值
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        
        /// <summary>
        /// 删除缓存
        /// </summary>
        Task RemoveAsync(string key);
        
        /// <summary>
        /// 清空缓存
        /// </summary>
        Task ClearAsync();
    }
}