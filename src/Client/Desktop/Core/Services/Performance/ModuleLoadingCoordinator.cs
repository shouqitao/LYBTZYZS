using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Prism.Modularity;

namespace LYBT.Desktop.Core.Services.Performance
{
    public interface IModuleLoadingCoordinator
    {
        Task PreloadModulesAsync(string userRole);
        void TrackModuleInitialization(string moduleName, TimeSpan initializationTime);
        Task<bool> IsModuleReadyAsync(string moduleName, TimeSpan timeout);
        Dictionary<string, ModuleLoadingMetrics> GetLoadingMetrics();
        void OptimizeModuleLoadingOrder();
    }

    public class ModuleLoadingMetrics
    {
        public string ModuleName { get; set; } = string.Empty;
        public TimeSpan InitializationTime { get; set; }
        public DateTime LastLoaded { get; set; }
        public int LoadCount { get; set; }
        public List<string> Dependencies { get; set; } = new();
        public ModuleLoadingPriority Priority { get; set; }
    }

    public enum ModuleLoadingPriority
    {
        Critical = 1,    // 认证等必需模块
        High = 2,        // 用户角色相关核心模块
        Medium = 3,      // 常用业务模块
        Low = 4          // 辅助功能模块
    }

    public class ModuleLoadingCoordinator : IModuleLoadingCoordinator
    {
        private readonly IModuleManager _moduleManager;
        private readonly ILogger<ModuleLoadingCoordinator> _logger;
        private readonly Dictionary<string, ModuleLoadingMetrics> _moduleMetrics;
        private readonly Dictionary<string, TaskCompletionSource<bool>> _moduleReadyTasks;

        // UltraThink 角色基础模块预加载策略
        private readonly Dictionary<string, List<string>> _roleBasedPreloadModules = new()
        {
            ["Doctor"] = new() { "PatientsModule", "ConsultationModule", "MedicalCaseModule", "PrescriptionsModule" },
            ["Admin"] = new() { "UsersModule", "PatientsModule", "HerbsModule", "FormulaModule" },
            ["Receptionist"] = new() { "PatientsModule", "ConsultationModule" }
        };

        // 模块优先级定义
        private readonly Dictionary<string, ModuleLoadingPriority> _modulePriorities = new()
        {
            ["AuthenticationModule"] = ModuleLoadingPriority.Critical,
            ["PatientsModule"] = ModuleLoadingPriority.High,
            ["ConsultationModule"] = ModuleLoadingPriority.High,
            ["MedicalCaseModule"] = ModuleLoadingPriority.High,
            ["UsersModule"] = ModuleLoadingPriority.Medium,
            ["PrescriptionsModule"] = ModuleLoadingPriority.Medium,
            ["HerbsModule"] = ModuleLoadingPriority.Medium,
            ["FormulaModule"] = ModuleLoadingPriority.Low,
            ["ConsultationWorkbenchModule"] = ModuleLoadingPriority.Low
        };

        public ModuleLoadingCoordinator(IModuleManager moduleManager, ILogger<ModuleLoadingCoordinator> logger)
        {
            _moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _moduleMetrics = new Dictionary<string, ModuleLoadingMetrics>();
            _moduleReadyTasks = new Dictionary<string, TaskCompletionSource<bool>>();

            InitializeModuleMetrics();
        }

        private void InitializeModuleMetrics()
        {
            foreach (var (moduleName, priority) in _modulePriorities)
            {
                _moduleMetrics[moduleName] = new ModuleLoadingMetrics
                {
                    ModuleName = moduleName,
                    Priority = priority,
                    Dependencies = GetModuleDependencies(moduleName)
                };
                _moduleReadyTasks[moduleName] = new TaskCompletionSource<bool>();
            }
        }

        public async Task PreloadModulesAsync(string userRole)
        {
            _logger.LogInformation("UltraThink模块预加载: 开始为角色 {Role} 预加载模块", userRole);
            
            if (!_roleBasedPreloadModules.TryGetValue(userRole, out var modulesToPreload))
            {
                _logger.LogWarning("未找到角色 {Role} 的预加载配置", userRole);
                return;
            }

            var preloadTasks = new List<Task>();
            var stopwatch = Stopwatch.StartNew();

            // 按优先级排序模块
            var orderedModules = modulesToPreload
                .Where(m => _modulePriorities.ContainsKey(m))
                .OrderBy(m => _modulePriorities[m])
                .ToList();

            _logger.LogDebug("预加载顺序: {Modules}", string.Join(" -> ", orderedModules));

            foreach (var moduleName in orderedModules)
            {
                preloadTasks.Add(PreloadSingleModuleAsync(moduleName));
            }

            await Task.WhenAll(preloadTasks);
            
            stopwatch.Stop();
            _logger.LogInformation("UltraThink模块预加载: 角色 {Role} 的 {Count} 个模块预加载完成，耗时 {Duration}ms", 
                userRole, modulesToPreload.Count, stopwatch.ElapsedMilliseconds);
        }

        private async Task PreloadSingleModuleAsync(string moduleName)
        {
            try
            {
                var moduleStopwatch = Stopwatch.StartNew();
                _logger.LogDebug("开始预加载模块: {ModuleName}", moduleName);

                // 使用ModuleManager的LoadModule方法
                await Task.Run(() => _moduleManager.LoadModule(moduleName));
                
                moduleStopwatch.Stop();
                TrackModuleInitialization(moduleName, moduleStopwatch.Elapsed);
                
                // 标记模块就绪
                if (_moduleReadyTasks.TryGetValue(moduleName, out var tcs))
                {
                    tcs.SetResult(true);
                }

                _logger.LogDebug("模块 {ModuleName} 预加载完成，耗时 {Duration}ms", 
                    moduleName, moduleStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预加载模块 {ModuleName} 失败", moduleName);
                
                if (_moduleReadyTasks.TryGetValue(moduleName, out var tcs))
                {
                    tcs.SetException(ex);
                }
            }
        }

        public void TrackModuleInitialization(string moduleName, TimeSpan initializationTime)
        {
            if (_moduleMetrics.TryGetValue(moduleName, out var metrics))
            {
                metrics.InitializationTime = initializationTime;
                metrics.LastLoaded = DateTime.Now;
                metrics.LoadCount++;

                _logger.LogDebug("模块 {ModuleName} 初始化追踪: {Duration}ms (第{Count}次加载)", 
                    moduleName, initializationTime.TotalMilliseconds, metrics.LoadCount);
            }
        }

        public async Task<bool> IsModuleReadyAsync(string moduleName, TimeSpan timeout)
        {
            if (!_moduleReadyTasks.TryGetValue(moduleName, out var tcs))
            {
                _logger.LogWarning("未找到模块 {ModuleName} 的就绪状态追踪", moduleName);
                return false;
            }

            try
            {
                using var cts = new System.Threading.CancellationTokenSource(timeout);
                var result = await tcs.Task.WaitAsync(cts.Token);
                return result;
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("等待模块 {ModuleName} 就绪超时 ({Timeout}ms)", moduleName, timeout.TotalMilliseconds);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查模块 {ModuleName} 就绪状态失败", moduleName);
                return false;
            }
        }

        public Dictionary<string, ModuleLoadingMetrics> GetLoadingMetrics()
        {
            return new Dictionary<string, ModuleLoadingMetrics>(_moduleMetrics);
        }

        public void OptimizeModuleLoadingOrder()
        {
            _logger.LogInformation("UltraThink性能优化: 开始优化模块加载顺序");

            var loadingTimes = _moduleMetrics
                .Where(kvp => kvp.Value.LoadCount > 0)
                .OrderByDescending(kvp => kvp.Value.InitializationTime)
                .ToList();

            foreach (var (moduleName, metrics) in loadingTimes.Take(3))
            {
                _logger.LogWarning("模块 {ModuleName} 初始化时间较长: {Duration}ms，建议优化", 
                    moduleName, metrics.InitializationTime.TotalMilliseconds);
            }

            // 建议性能优化策略
            var slowModules = loadingTimes
                .Where(kvp => kvp.Value.InitializationTime.TotalMilliseconds > 500)
                .Select(kvp => kvp.Key)
                .ToList();

            if (slowModules.Any())
            {
                _logger.LogInformation("UltraThink建议: 以下模块可考虑延迟加载或优化: {SlowModules}", 
                    string.Join(", ", slowModules));
            }
        }

        private List<string> GetModuleDependencies(string moduleName)
        {
            // 基于现有架构的模块依赖关系
            return moduleName switch
            {
                "ConsultationWorkbenchModule" => new() { "PatientsModule", "ConsultationModule", "MedicalCaseModule" },
                "PrescriptionsModule" => new() { "HerbsModule", "FormulaModule" },
                "MedicalCaseModule" => new() { "PatientsModule", "ConsultationModule" },
                _ => new List<string>()
            };
        }
    }
}