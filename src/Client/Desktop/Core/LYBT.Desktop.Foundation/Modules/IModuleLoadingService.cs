namespace LYBT.Desktop.Foundation.Modules
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
        /// 检查指定模块是否已加载
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        bool IsModuleLoaded(string moduleName);

        /// <summary>
        /// 模块加载完成事件
        /// </summary>
        event EventHandler<string> ModuleLoaded;
    }
}
