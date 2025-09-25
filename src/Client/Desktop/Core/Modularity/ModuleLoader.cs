using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Core.Modularity
{
    /// <summary>
    /// 模块动态加载器
    /// 支持按需加载、延迟加载和条件加载
    /// </summary>
    public class ModuleLoader : IModuleLoader
    {
        private readonly IModuleManager _moduleManager;
        private readonly IModuleCatalog _moduleCatalog;
        private readonly IContainerProvider _containerProvider;
        private readonly ILogger<ModuleLoader> _logger;
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, ModuleState> _moduleStates;
        private readonly object _lock = new object();

        public ModuleLoader(
            IModuleManager moduleManager,
            IModuleCatalog moduleCatalog,
            IContainerProvider containerProvider,
            ILogger<ModuleLoader> logger,
            IConfiguration configuration)
        {
            _moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
            _moduleCatalog = moduleCatalog ?? throw new ArgumentNullException(nameof(moduleCatalog));
            _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _moduleStates = new Dictionary<string, ModuleState>();

            InitializeModuleStates();
        }

        #region 初始化

        private void InitializeModuleStates()
        {
            foreach (var module in _moduleCatalog.Modules)
            {
                _moduleStates[module.ModuleName] = new ModuleState
                {
                    ModuleName = module.ModuleName,
                    State = ModuleLoadState.NotLoaded,
                    ModuleInfo = module
                };
            }
        }

        #endregion

        #region 模块加载

        /// <summary>
        /// 加载单个模块
        /// </summary>
        public async Task<bool> LoadModuleAsync(string moduleName)
        {
            try
            {
                _logger.LogInformation("开始加载模块: {ModuleName}", moduleName);

                lock (_lock)
                {
                    if (!_moduleStates.ContainsKey(moduleName))
                    {
                        _logger.LogWarning("模块不存在: {ModuleName}", moduleName);
                        return false;
                    }

                    var state = _moduleStates[moduleName];
                    if (state.State == ModuleLoadState.Loaded)
                    {
                        _logger.LogDebug("模块已加载: {ModuleName}", moduleName);
                        return true;
                    }

                    state.State = ModuleLoadState.Loading;
                }

                // 检查依赖
                if (!await CheckDependenciesAsync(moduleName))
                {
                    _logger.LogError("模块依赖检查失败: {ModuleName}", moduleName);
                    UpdateModuleState(moduleName, ModuleLoadState.Failed);
                    return false;
                }

                // 执行加载
                await Task.Run(() => _moduleManager.LoadModule(moduleName));

                UpdateModuleState(moduleName, ModuleLoadState.Loaded);
                _logger.LogInformation("模块加载成功: {ModuleName}", moduleName);

                // 触发事件
                OnModuleLoaded(moduleName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "模块加载失败: {ModuleName}", moduleName);
                UpdateModuleState(moduleName, ModuleLoadState.Failed);
                return false;
            }
        }

        /// <summary>
        /// 批量加载模块
        /// </summary>
        public async Task<Dictionary<string, bool>> LoadModulesAsync(IEnumerable<string> moduleNames)
        {
            var results = new Dictionary<string, bool>();

            foreach (var moduleName in moduleNames)
            {
                results[moduleName] = await LoadModuleAsync(moduleName);
            }

            return results;
        }

        /// <summary>
        /// 按角色加载模块
        /// </summary>
        public async Task<bool> LoadModulesByRoleAsync(string userRole)
        {
            var roleModules = GetModulesForRole(userRole);

            if (!roleModules.Any())
            {
                _logger.LogWarning("角色没有配置模块: {Role}", userRole);
                return false;
            }

            var results = await LoadModulesAsync(roleModules);
            return results.All(r => r.Value);
        }

        /// <summary>
        /// 延迟加载模块
        /// </summary>
        public void LoadModuleOnDemand(string moduleName, Action<bool> callback)
        {
            Task.Run(async () =>
            {
                var result = await LoadModuleAsync(moduleName);
                callback?.Invoke(result);
            });
        }

        #endregion

        #region 模块卸载

        /// <summary>
        /// 卸载模块
        /// </summary>
        public async Task<bool> UnloadModuleAsync(string moduleName)
        {
            try
            {
                lock (_lock)
                {
                    if (!_moduleStates.ContainsKey(moduleName))
                        return false;

                    var state = _moduleStates[moduleName];
                    if (state.State != ModuleLoadState.Loaded)
                        return false;
                }

                // 检查是否有其他模块依赖此模块
                if (HasDependents(moduleName))
                {
                    _logger.LogWarning("无法卸载模块，存在依赖: {ModuleName}", moduleName);
                    return false;
                }

                // 执行卸载逻辑
                await UnloadModuleCore(moduleName);

                UpdateModuleState(moduleName, ModuleLoadState.NotLoaded);
                _logger.LogInformation("模块已卸载: {ModuleName}", moduleName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "模块卸载失败: {ModuleName}", moduleName);
                return false;
            }
        }

        private async Task UnloadModuleCore(string moduleName)
        {
            // 实际的卸载逻辑需要根据具体实现
            // 这里提供基本框架
            await Task.CompletedTask;
        }

        #endregion

        #region 状态查询

        /// <summary>
        /// 获取模块状态
        /// </summary>
        public ModuleState GetModuleState(string moduleName)
        {
            lock (_lock)
            {
                return _moduleStates.TryGetValue(moduleName, out var state) ? state : null;
            }
        }

        /// <summary>
        /// 获取所有模块状态
        /// </summary>
        public IReadOnlyDictionary<string, ModuleState> GetAllModuleStates()
        {
            lock (_lock)
            {
                return new Dictionary<string, ModuleState>(_moduleStates);
            }
        }

        /// <summary>
        /// 检查模块是否已加载
        /// </summary>
        public bool IsModuleLoaded(string moduleName)
        {
            return GetModuleState(moduleName)?.State == ModuleLoadState.Loaded;
        }

        #endregion

        #region 依赖管理

        private async Task<bool> CheckDependenciesAsync(string moduleName)
        {
            var moduleInfo = _moduleCatalog.Modules.FirstOrDefault(m => m.ModuleName == moduleName);
            if (moduleInfo == null)
                return false;

            if (moduleInfo.DependsOn == null || !moduleInfo.DependsOn.Any())
                return true;

            foreach (var dependency in moduleInfo.DependsOn)
            {
                if (!IsModuleLoaded(dependency))
                {
                    _logger.LogInformation("加载依赖模块: {Dependency} for {Module}",
                        dependency, moduleName);

                    if (!await LoadModuleAsync(dependency))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool HasDependents(string moduleName)
        {
            return _moduleCatalog.Modules.Any(m =>
                m.DependsOn != null && m.DependsOn.Contains(moduleName) &&
                IsModuleLoaded(m.ModuleName));
        }

        #endregion

        #region 配置

        private List<string> GetModulesForRole(string userRole)
        {
            var key = $"Modules:Roles:{userRole}";
            var modules = _configuration.GetSection(key).Get<List<string>>();
            return modules ?? new List<string>();
        }

        #endregion

        #region 辅助方法

        private void UpdateModuleState(string moduleName, ModuleLoadState state)
        {
            lock (_lock)
            {
                if (_moduleStates.ContainsKey(moduleName))
                {
                    _moduleStates[moduleName].State = state;
                    _moduleStates[moduleName].LastStateChange = DateTime.Now;
                }
            }
        }

        #endregion

        #region 事件

        public event EventHandler<ModuleLoadedEventArgs> ModuleLoaded;

        private void OnModuleLoaded(string moduleName)
        {
            ModuleLoaded?.Invoke(this, new ModuleLoadedEventArgs { ModuleName = moduleName });
        }

        #endregion
    }

    /// <summary>
    /// 模块加载器接口
    /// </summary>
    public interface IModuleLoader
    {
        Task<bool> LoadModuleAsync(string moduleName);
        Task<Dictionary<string, bool>> LoadModulesAsync(IEnumerable<string> moduleNames);
        Task<bool> LoadModulesByRoleAsync(string userRole);
        void LoadModuleOnDemand(string moduleName, Action<bool> callback);
        Task<bool> UnloadModuleAsync(string moduleName);
        ModuleState GetModuleState(string moduleName);
        IReadOnlyDictionary<string, ModuleState> GetAllModuleStates();
        bool IsModuleLoaded(string moduleName);
        event EventHandler<ModuleLoadedEventArgs> ModuleLoaded;
    }

    /// <summary>
    /// 模块状态
    /// </summary>
    public class ModuleState
    {
        public string ModuleName { get; set; }
        public ModuleLoadState State { get; set; }
        public DateTime LastStateChange { get; set; }
        public IModuleInfo ModuleInfo { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 模块加载状态
    /// </summary>
    public enum ModuleLoadState
    {
        NotLoaded,
        Loading,
        Loaded,
        Failed,
        Unloading
    }

    /// <summary>
    /// 模块加载事件参数
    /// </summary>
    public class ModuleLoadedEventArgs : EventArgs
    {
        public string ModuleName { get; set; }
    }
}