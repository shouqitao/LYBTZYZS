using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LYBT.Desktop.Core.Services.Modules
{
    /// <summary>
    /// 模块加载服务接口
    /// 提供模块按需加载和状态管理
    /// </summary>
    public interface IModuleLoadingService
    {
        /// <summary>
        /// 检查模块是否已加载
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <returns>是否已加载</returns>
        bool IsModuleLoaded(string moduleName);

        /// <summary>
        /// 异步加载指定模块
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <returns>加载是否成功</returns>
        Task<bool> LoadModuleAsync(string moduleName);

        /// <summary>
        /// 异步加载多个模块
        /// </summary>
        /// <param name="moduleNames">模块名称列表</param>
        /// <returns>加载结果字典</returns>
        Task<Dictionary<string, bool>> LoadModulesAsync(params string[] moduleNames);

        /// <summary>
        /// 获取模块加载进度
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <returns>加载进度(0-100)</returns>
        int GetModuleLoadingProgress(string moduleName);

        /// <summary>
        /// 模块加载完成事件
        /// </summary>
        event EventHandler<ModuleLoadedEventArgs> ModuleLoaded;

        /// <summary>
        /// 模块加载失败事件
        /// </summary>
        event EventHandler<ModuleLoadFailedEventArgs> ModuleLoadFailed;

        /// <summary>
        /// 已加载的模块集合
        /// </summary>
        ObservableCollection<ModuleInfo> LoadedModules { get; }

        /// <summary>
        /// 正在加载的模块集合
        /// </summary>
        ObservableCollection<string> LoadingModules { get; }
    }

    /// <summary>
    /// 模块信息
    /// </summary>
    public class ModuleInfo
    {
        public string ModuleName { get; set; } = string.Empty;
        public DateTime LoadedTime { get; set; }
        public long LoadTimeMilliseconds { get; set; }
        public string Version { get; set; } = "1.0.0";
        public ModuleState State { get; set; }
        public List<string> Dependencies { get; set; } = new();
    }

    /// <summary>
    /// 模块状态枚举
    /// </summary>
    public enum ModuleState
    {
        /// <summary>
        /// 未加载
        /// </summary>
        NotLoaded,

        /// <summary>
        /// 正在加载
        /// </summary>
        Loading,

        /// <summary>
        /// 已加载
        /// </summary>
        Loaded,

        /// <summary>
        /// 加载失败
        /// </summary>
        Failed,

        /// <summary>
        /// 已禁用
        /// </summary>
        Disabled
    }

    /// <summary>
    /// 模块加载完成事件参数
    /// </summary>
    public class ModuleLoadedEventArgs : EventArgs
    {
        public ModuleInfo ModuleInfo { get; }
        public TimeSpan LoadTime { get; }

        public ModuleLoadedEventArgs(ModuleInfo moduleInfo, TimeSpan loadTime)
        {
            ModuleInfo = moduleInfo;
            LoadTime = loadTime;
        }
    }

    /// <summary>
    /// 模块加载失败事件参数
    /// </summary>
    public class ModuleLoadFailedEventArgs : EventArgs
    {
        public string ModuleName { get; }
        public Exception Error { get; }
        public string ErrorMessage { get; }

        public ModuleLoadFailedEventArgs(string moduleName, Exception error)
        {
            ModuleName = moduleName;
            Error = error;
            ErrorMessage = error?.Message ?? "未知错误";
        }
    }
}