using System.ComponentModel;

namespace LYBT.Shared.Interfaces.Caching
{

    /// <summary>
    /// 简化缓存服务接口 - UltraThink精简架构标准
    /// </summary>
    /// <remarks>
    /// <para>设计理念: 从复杂14方法精简至核心8方法，专注实用性和开发效率</para>
    /// <para>技术特性: 基于IMemoryCache的智能缓存，适合小型诊所部署</para>
    /// <para>性能优化: 同步+异步双模式，支持高频调用和复杂数据获取场景</para>
    /// <para>使用场景: 用户信息、药材数据、验方模板等频繁访问数据的缓存</para>
    /// </remarks>
    [Description("简化缓存服务 - 智能内存缓存，8个核心方法")]
    public interface ISimplifiedCacheService
    {

        #region 同步操作 - 高频快速访问

        /// <summary>
        /// 获取缓存项 (同步)
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存键 - 建议使用模块前缀，如 "users:123", "herbs:active"</param>
        /// <returns>缓存的数据项，不存在时返回default(T)</returns>
        /// <remarks>
        /// <para>适用: 高频访问的简单数据，如用户基本信息、枚举列表</para>
        /// <para>性能: 直接内存访问，微秒级响应</para>
        /// <para>示例: var user = cache.Get&lt;UserDto&gt;("user:123");</para>
        /// </remarks>
        T? Get<T>(string key);

        /// <summary>
        /// 设置缓存项 (同步)
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存键 - 遵循命名规范</param>
        /// <param name="value">缓存数据</param>
        /// <param name="expiration">过期时间 - null表示使用默认10分钟过期</param>
        /// <remarks>
        /// <para>适用: 立即缓存计算结果、配置信息、静态数据</para>
        /// <para>策略: LRU淘汰策略，自动清理过期数据</para>
        /// <para>示例: cache.Set("herbs:list", herbsList, TimeSpan.FromHours(1));</para>
        /// </remarks>
        void Set<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// 移除缓存项 (同步)
        /// </summary>
        /// <param name="key">要移除的缓存键</param>
        /// <returns>true: 成功移除; false: 键不存在</returns>
        /// <remarks>
        /// <para>适用: 数据更新后立即失效相关缓存</para>
        /// <para>场景: 用户信息修改、药材信息更新、权限变更后</para>
        /// <para>示例: cache.Remove("user:123"); // 用户更新后</para>
        /// </remarks>
        bool Remove(string key);

        /// <summary>
        /// 清空所有缓存 (同步)
        /// </summary>
        /// <remarks>
        /// <para>适用: 系统重启、配置重载、紧急数据清理</para>
        /// <para>影响: 清空所有模块缓存，下次访问将重新加载</para>
        /// <para>场景: 切换诊所、权限系统重载、内存清理</para>
        /// </remarks>
        void Clear();

        #endregion 同步操作 - 高频快速访问

        #region 异步操作 - 复杂数据处理

        /// <summary>
        /// 获取缓存项 (异步)
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存的数据项</returns>
        /// <remarks>
        /// <para>适用: 与异步工作流集成，保持调用链异步一致性</para>
        /// <para>场景: 在异步方法中获取缓存，避免同步调用阻塞</para>
        /// <para>示例: var users = await cache.GetAsync&lt;List&lt;UserDto&gt;&gt;("users:active");</para>
        /// </remarks>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// 设置缓存项 (异步)
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存数据</param>
        /// <param name="expiration">过期时间</param>
        /// <returns>异步操作任务</returns>
        /// <remarks>
        /// <para>适用: 异步数据处理流程中的缓存设置</para>
        /// <para>场景: 数据库查询结果缓存、API响应缓存、计算结果缓存</para>
        /// <para>示例: await cache.SetAsync("report:monthly", report, TimeSpan.FromHours(6));</para>
        /// </remarks>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// 移除缓存项 (异步)
        /// </summary>
        /// <param name="key">要移除的缓存键</param>
        /// <returns>移除操作结果</returns>
        /// <remarks>
        /// <para>适用: 异步数据更新流程中的缓存失效</para>
        /// <para>场景: 异步数据保存后的缓存清理、批量操作后的缓存更新</para>
        /// <para>示例: await cache.RemoveAsync("patients:stats"); // 患者数据更新后</para>
        /// </remarks>
        Task<bool> RemoveAsync(string key);

        /// <summary>
        /// 获取或设置缓存项 (异步) - 核心缓存模式方法
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">数据工厂方法 - 缓存未命中时调用获取数据</param>
        /// <param name="expiration">过期时间</param>
        /// <returns>缓存或新获取的数据</returns>
        /// <remarks>
        /// <para>核心模式: 缓存命中直接返回，未命中时调用工厂方法获取数据并缓存</para>
        /// <para>适用场景: 数据库查询、API调用、复杂计算结果的缓存</para>
        /// <para>性能优势: 一次调用处理缓存逻辑，避免重复的存在性检查</para>
        /// <para>典型用法:</para>
        /// <code>
        /// var activeUsers = await cacheService.GetOrSetAsync(
        ///     "users:active",
        ///     async () => await userService.GetActiveUsersAsync(),
        ///     TimeSpan.FromMinutes(10)
        /// );
        /// </code>
        /// </remarks>
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

        #endregion 异步操作 - 复杂数据处理
    }
}
