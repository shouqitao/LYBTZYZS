using System;
using System.Threading.Tasks;

namespace LYBT.Desktop.Services.Performance
{
    /// <summary>
    /// 启动优化服务接口 - 提升应用程序启动性能
    /// </summary>
    public interface IStartupOptimizationService
    {
        /// <summary>
        /// 异步执行启动预热操作
        /// </summary>
        Task WarmupAsync();

        /// <summary>
        /// 异步预加载关键资源
        /// </summary>
        Task PreloadCriticalResourcesAsync();

        /// <summary>
        /// 异步优化启动流程
        /// </summary>
        Task OptimizeStartupAsync();

        /// <summary>
        /// 获取启动耗时统计
        /// </summary>
        TimeSpan GetStartupDuration();

        /// <summary>
        /// 清理启动缓存
        /// </summary>
        void ClearStartupCache();

        /// <summary>
        /// 启动优化完成事件
        /// </summary>
        event EventHandler OptimizationCompleted;

        /// <summary>
        /// 异步执行应用程序预热操作
        /// </summary>
        Task WarmupApplicationAsync();
    }
}