using LYBT.Desktop.Foundation.Performance;
using LYBT.Desktop.Presentation.Notifications;
using Microsoft.Extensions.Logging;
using Prism.Modularity;

namespace LYBT.Desktop.Shell.Services
{
    /// <summary>
    /// 应用程序初始化服务 - 去除ServiceLocator反模式
    /// 集中管理应用启动时的各种初始化任务
    /// </summary>
    public interface IApplicationInitializationService
    {
        /// <summary>
        /// 初始化应用程序核心服务
        /// </summary>
        Task InitializeCoreServicesAsync();

        /// <summary>
        /// 初始化错误处理
        /// </summary>
        void InitializeErrorHandling();

        /// <summary>
        /// 预热应用程序
        /// </summary>
        Task WarmupApplicationAsync();

        /// <summary>
        /// 初始化模块协调器
        /// </summary>
        void InitializeModuleCoordinator();
    }

    /// <summary>
    /// 应用程序初始化服务实现
    /// </summary>
    public class ApplicationInitializationService : IApplicationInitializationService
    {
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly IStartupOptimizationService _startupOptimizationService;
        private readonly IModuleManager _moduleManager;
        private readonly IModuleCatalog _moduleCatalog;
        private readonly ILogger<ApplicationInitializationService> _logger;

        public ApplicationInitializationService(
            IErrorHandlingService errorHandlingService,
            IStartupOptimizationService startupOptimizationService,
            IModuleManager moduleManager,
            IModuleCatalog moduleCatalog,
            ILogger<ApplicationInitializationService> logger)
        {
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _startupOptimizationService = startupOptimizationService ?? throw new ArgumentNullException(nameof(startupOptimizationService));
            _moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
            _moduleCatalog = moduleCatalog ?? throw new ArgumentNullException(nameof(moduleCatalog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 初始化应用程序核心服务
        /// </summary>
        public async Task InitializeCoreServicesAsync()
        {
            _logger.LogInformation("开始初始化应用程序核心服务");

            try
            {
                // 初始化错误处理
                InitializeErrorHandling();

                // 预热应用程序
                await WarmupApplicationAsync().ConfigureAwait(false);

                // 初始化模块协调器
                InitializeModuleCoordinator();

                _logger.LogInformation("应用程序核心服务初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化应用程序核心服务失败");
                throw;
            }
        }

        /// <summary>
        /// 初始化错误处理
        /// </summary>
        public void InitializeErrorHandling()
        {
            try
            {
                _logger.LogDebug("初始化全局错误处理");
                _errorHandlingService.RegisterGlobalExceptionHandlers();
                _logger.LogInformation("全局错误处理初始化成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化错误处理服务失败");
                // 错误处理服务初始化失败是严重问题，但不应阻止应用启动
                // 使用降级方案
                System.Windows.Application.Current.DispatcherUnhandledException += (s, e) =>
                {
                    _logger.LogError(e.Exception, "未处理的应用程序异常");
                    e.Handled = true;
                };
            }
        }

        /// <summary>
        /// 预热应用程序
        /// </summary>
        public async Task WarmupApplicationAsync()
        {
            try
            {
                _logger.LogDebug("开始应用程序预热");
                await _startupOptimizationService.WarmupApplicationAsync().ConfigureAwait(false);
                _logger.LogInformation("应用程序预热完成");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "应用程序预热失败，继续启动");
                // 预热失败不影响主流程
            }
        }

        /// <summary>
        /// 初始化模块协调器
        /// </summary>
        public void InitializeModuleCoordinator()
        {
            try
            {
                _logger.LogDebug("初始化模块协调器");

                // 记录已加载的模块
                foreach (var module in _moduleCatalog.Modules)
                {
                    _logger.LogDebug("模块已注册 {ModuleName} - {ModuleType}",
                        module.ModuleName, module.ModuleType);
                }

                // 获取已加载的模块信息
                var loadedModules = _moduleManager.GetType()
                    .GetProperty("LoadedModules",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance)
                    ?.GetValue(_moduleManager);

                if (loadedModules != null)
                {
                    _logger.LogInformation("模块协调器初始化完成，已加载模块数 {Count}",
                        ((IEnumerable<object>)loadedModules).Count());
                }
                else
                {
                    _logger.LogInformation("模块协调器初始化完成");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "模块协调器初始化异常");
                // 模块协调器初始化失败不应阻塞应用启动
            }
        }
    }
}
