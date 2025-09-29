using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prism.Modularity;

namespace LYBT.Desktop.Core.Services.Modules
{
    /// <summary>
    /// 模块加载服务实现
    /// 管理模块的按需加载和状态跟踪
    /// </summary>
    public class ModuleLoadingService : IModuleLoadingService
    {
        private readonly IModuleManager _moduleManager;
        private readonly IModuleCatalog _moduleCatalog;
        private readonly ILogger<ModuleLoadingService> _logger;
        private readonly Dictionary<string, int> _loadingProgress;
        private readonly object _lockObject = new();

        public ObservableCollection<ModuleInfo> LoadedModules { get; }
        public ObservableCollection<string> LoadingModules { get; }

        public event EventHandler<ModuleLoadedEventArgs>? ModuleLoaded;
        public event EventHandler<ModuleLoadFailedEventArgs>? ModuleLoadFailed;

        public ModuleLoadingService(
            IModuleManager moduleManager,
            IModuleCatalog moduleCatalog,
            ILogger<ModuleLoadingService> logger)
        {
            _moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
            _moduleCatalog = moduleCatalog ?? throw new ArgumentNullException(nameof(moduleCatalog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            LoadedModules = new ObservableCollection<ModuleInfo>();
            LoadingModules = new ObservableCollection<string>();
            _loadingProgress = new Dictionary<string, int>();

            // 监听模块加载事件
            _moduleManager.LoadModuleCompleted += OnLoadModuleCompleted;

            _logger.LogInformation("模块加载服务初始化完成");
        }

        public bool IsModuleLoaded(string moduleName)
        {
            lock (_lockObject)
            {
                return LoadedModules.Any(m => m.ModuleName == moduleName && m.State == ModuleState.Loaded);
            }
        }

        public async Task<bool> LoadModuleAsync(string moduleName)
        {
            if (IsModuleLoaded(moduleName))
            {
                _logger.LogDebug($"模块 {moduleName} 已加载，跳过");
                return true;
            }

            lock (_lockObject)
            {
                if (LoadingModules.Contains(moduleName))
                {
                    _logger.LogWarning($"模块 {moduleName} 正在加载中，跳过重复加载请求");
                    return false;
                }

                LoadingModules.Add(moduleName);
                _loadingProgress[moduleName] = 0;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation($"开始加载模块: {moduleName}");

                // 模拟加载进度
                _ = Task.Run(async () =>
                {
                    for (int i = 0; i <= 90; i += 10)
                    {
                        await Task.Delay(50);
                        lock (_lockObject)
                        {
                            if (_loadingProgress.ContainsKey(moduleName))
                                _loadingProgress[moduleName] = i;
                        }
                    }
                });

                // 执行实际加载
                await Task.Run(() => _moduleManager.LoadModule(moduleName));

                lock (_lockObject)
                {
                    _loadingProgress[moduleName] = 100;
                }

                stopwatch.Stop();

                var moduleInfo = new ModuleInfo
                {
                    ModuleName = moduleName,
                    LoadedTime = DateTime.Now,
                    LoadTimeMilliseconds = stopwatch.ElapsedMilliseconds,
                    State = ModuleState.Loaded,
                    Dependencies = GetModuleDependencies(moduleName)
                };

                lock (_lockObject)
                {
                    LoadedModules.Add(moduleInfo);
                    LoadingModules.Remove(moduleName);
                    _loadingProgress.Remove(moduleName);
                }

                ModuleLoaded?.Invoke(this, new ModuleLoadedEventArgs(moduleInfo, stopwatch.Elapsed));

                _logger.LogInformation($"模块 {moduleName} 加载成功，耗时: {stopwatch.ElapsedMilliseconds}ms");
                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                lock (_lockObject)
                {
                    LoadingModules.Remove(moduleName);
                    _loadingProgress.Remove(moduleName);

                    // 添加失败记录
                    LoadedModules.Add(new ModuleInfo
                    {
                        ModuleName = moduleName,
                        LoadedTime = DateTime.Now,
                        LoadTimeMilliseconds = stopwatch.ElapsedMilliseconds,
                        State = ModuleState.Failed
                    });
                }

                _logger.LogError(ex, $"模块 {moduleName} 加载失败");
                ModuleLoadFailed?.Invoke(this, new ModuleLoadFailedEventArgs(moduleName, ex));

                return false;
            }
        }

        public async Task<Dictionary<string, bool>> LoadModulesAsync(params string[] moduleNames)
        {
            var results = new Dictionary<string, bool>();

            // 并行加载模块
            var loadTasks = moduleNames.Select(async moduleName =>
            {
                var success = await LoadModuleAsync(moduleName);
                return new { ModuleName = moduleName, Success = success };
            });

            var loadResults = await Task.WhenAll(loadTasks);

            foreach (var result in loadResults)
            {
                results[result.ModuleName] = result.Success;
            }

            return results;
        }

        public int GetModuleLoadingProgress(string moduleName)
        {
            lock (_lockObject)
            {
                if (_loadingProgress.TryGetValue(moduleName, out var progress))
                {
                    return progress;
                }

                if (IsModuleLoaded(moduleName))
                {
                    return 100;
                }

                return 0;
            }
        }

        private void OnLoadModuleCompleted(object? sender, LoadModuleCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                _logger.LogDebug($"模块管理器报告: {e.ModuleInfo.ModuleName} 加载完成");
            }
            else
            {
                _logger.LogError(e.Error, $"模块管理器报告: {e.ModuleInfo.ModuleName} 加载失败");
            }
        }

        private List<string> GetModuleDependencies(string moduleName)
        {
            try
            {
                var moduleInfo = _moduleCatalog.Modules.FirstOrDefault(m => m.ModuleName == moduleName);
                return moduleInfo?.DependsOn?.ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"获取模块 {moduleName} 的依赖信息失败");
                return new List<string>();
            }
        }
    }
}