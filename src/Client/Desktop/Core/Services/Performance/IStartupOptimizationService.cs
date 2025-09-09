namespace LYBT.Desktop.Core.Services.Performance
{

    /// <summary>
    /// UltraThink Phase H: 启动性能优化服务接口
    /// 提供应用启动速度优化和资源预加载管理
    /// </summary>
    public interface IStartupOptimizationService
    {

        /// <summary>
        /// 预热应用程序关键组件
        /// </summary>
        Task WarmupApplicationAsync();

        /// <summary>
        /// 基于用户角色预加载必要资源
        /// </summary>
        /// <param name="userRole">用户角色</param>
        Task PreloadRoleBasedResourcesAsync(string userRole);

        /// <summary>
        /// 获取启动性能指标
        /// </summary>
        StartupPerformanceMetrics GetStartupMetrics();

        /// <summary>
        /// 优化内存使用
        /// </summary>
        void OptimizeMemoryUsage();
    }

    /// <summary>
    /// 启动性能指标
    /// </summary>
    public class StartupPerformanceMetrics
    {
        public TimeSpan ApplicationStartupTime { get; set; }
        public TimeSpan ModuleLoadingTime { get; set; }
        public int LoadedModulesCount { get; set; }
        public long MemoryUsage { get; set; }
        public DateTime StartupTimestamp { get; set; }
    }
}
