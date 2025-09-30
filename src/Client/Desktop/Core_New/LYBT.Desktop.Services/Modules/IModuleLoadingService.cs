using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Desktop.Services.Modules
{
    /// <summary>
    /// 模块加载服务接口 - 管理应用程序模块的加载
    /// </summary>
    public interface IModuleLoadingService
    {
        /// <summary>
        /// 异步加载指定模块
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        Task LoadModuleAsync(string moduleName);

        /// <summary>
        /// 异步加载所有可用模块
        /// </summary>
        Task LoadAllModulesAsync();

        /// <summary>
        /// 获取已加载的模块列表
        /// </summary>
        IEnumerable<string> GetLoadedModules();

        /// <summary>
        /// 检查指定模块是否已加载
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        bool IsModuleLoaded(string moduleName);

        /// <summary>
        /// 模块加载完成事件
        /// </summary>
        event EventHandler<string> ModuleLoaded;

        /// <summary>
        /// 异步加载模块集合
        /// </summary>
        /// <param name="moduleNames">模块名称集合</param>
        Task LoadModulesAsync(IEnumerable<string>? moduleNames = null);
    }
}