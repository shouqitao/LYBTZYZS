using Microsoft.Extensions.Logging;
using Prism.Modularity;

namespace LYBT.Desktop.Services.Modules
{
    /// <summary>
    /// 模块加载服务实现 - UltraThink架构
    /// </summary>
    public class ModuleLoadingService : IModuleLoadingService
    {
        private readonly IModuleManager _moduleManager;
        private readonly IModuleCatalog _moduleCatalog;
        private readonly ILogger<ModuleLoadingService> _logger;
        private readonly HashSet<string> _loadedModules = new();

        public event EventHandler<string>? ModuleLoaded;

        public ModuleLoadingService(
            IModuleManager moduleManager,
            IModuleCatalog moduleCatalog,
            ILogger<ModuleLoadingService> logger)
        {
            _moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
            _moduleCatalog = moduleCatalog ?? throw new ArgumentNullException(nameof(moduleCatalog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task LoadModuleAsync(string moduleName)
        {
            try
            {
                await Task.Run(() =>
                {
                    if (!_loadedModules.Contains(moduleName))
                    {
                        _moduleManager.LoadModule(moduleName);
                        _loadedModules.Add(moduleName);
                        _logger.LogInformation("模块加载成功：{ModuleName}", moduleName);
                        ModuleLoaded?.Invoke(this, moduleName);
                    }
                    else
                    {
                        _logger.LogDebug("模块已加载：{ModuleName}", moduleName);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "模块加载失败：{ModuleName}", moduleName);
                throw;
            }
        }

        public async Task LoadAllModulesAsync()
        {
            try
            {
                var modules = _moduleCatalog.Modules.Select(m => m.ModuleName);
                foreach (var moduleName in modules)
                {
                    await LoadModuleAsync(moduleName);
                }
                _logger.LogInformation("所有模块加载完成，共 {Count} 个模块", _loadedModules.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量加载模块失败");
                throw;
            }
        }

        public IEnumerable<string> GetLoadedModules()
        {
            return _loadedModules.ToList();
        }

        public bool IsModuleLoaded(string moduleName)
        {
            return _loadedModules.Contains(moduleName);
        }

        public async Task LoadModulesAsync(IEnumerable<string>? moduleNames = null)
        {
            if (moduleNames == null)
            {
                await LoadAllModulesAsync();
                return;
            }

            foreach (var moduleName in moduleNames)
            {
                try
                {
                    await LoadModuleAsync(moduleName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "加载模块 {ModuleName} 失败，继续加载其他模块", moduleName);
                    // 继续加载其他模块
                }
            }
        }
    }
}
