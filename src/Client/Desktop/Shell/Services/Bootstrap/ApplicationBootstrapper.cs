using System.Windows;
using LYBT.Desktop.Presentation.Notifications;
using LYBT.Desktop.Foundation.Performance;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Modularity;

namespace LYBT.Desktop.Shell.Services.Bootstrap
{
    /// <summary>
    /// 应用程序启动引导服务实现
    /// 集中管理所有初始化逻辑，通过构造函数注入所有依赖
    /// 避免使用Service Locator反模式
    /// </summary>
    public class ApplicationBootstrapper : IApplicationBootstrapper
    {
        private readonly IApplicationInitializationService _initializationService;
        private readonly IStartupOptimizationService _startupOptimizationService;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly IModuleManager _moduleManager;
        private readonly IModuleCatalog _moduleCatalog;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<ApplicationBootstrapper> _logger;

        public ApplicationBootstrapper(
            IApplicationInitializationService initializationService,
            IStartupOptimizationService startupOptimizationService,
            IErrorHandlingService errorHandlingService,
            IModuleManager moduleManager,
            IModuleCatalog moduleCatalog,
            IEventAggregator eventAggregator,
            ILogger<ApplicationBootstrapper> logger)
        {
            _initializationService = initializationService ?? throw new ArgumentNullException(nameof(initializationService));
            _startupOptimizationService = startupOptimizationService ?? throw new ArgumentNullException(nameof(startupOptimizationService));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
            _moduleCatalog = moduleCatalog ?? throw new ArgumentNullException(nameof(moduleCatalog));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 初始化核心服务
        /// Issue #1239: 移除异常降级处理，让异常向上传播
        /// </summary>
        public async Task InitializeCoreServicesAsync()
        {
            _logger.LogInformation("开始初始化核心服务");
            
            // ✅ 不捕获异常，让异常向上传播到 App.InitializeApplicationAsync
            await _initializationService.InitializeCoreServicesAsync();
            
            _logger.LogInformation("核心服务初始化完成");
        }

        /// <summary>
        /// 初始化应用程序预热
        /// </summary>
        public async Task InitializeApplicationWarmupAsync()
        {
            try
            {
                _logger.LogInformation("开始应用程序预热");
                await _startupOptimizationService.WarmupApplicationAsync().ConfigureAwait(false);
                _logger.LogInformation("应用程序预热完成");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "应用预热失败，但不影响主流程");
                // 预热失败不影响主流程，仅记录日志
            }
        }

        /// <summary>
        /// 初始化错误处理服务
        /// </summary>
        public void InitializeErrorHandlingService()
        {
            try
            {
                _logger.LogInformation("注册全局异常处理器");
                _errorHandlingService.RegisterGlobalExceptionHandlers();
                _logger.LogInformation("全局异常处理器注册完成");
            }
            catch (Exception ex)
            {
                // 如果错误处理服务初始化失败，使用基本的错误处理
                _logger.LogError(ex, "初始化错误处理服务失败");
                MessageBox.Show($"系统初始化失败 {ex.Message}", "凌隐宝堂 - 系统错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 初始化简化的模块协调器
        /// </summary>
        public void InitializeSimplifiedModuleCoordinator()
        {
            try
            {
                _logger.LogInformation("UltraThink简化模块协调器初始化完成");

                // 订阅模块事件
                SubscribeToModuleEvents();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "简化模块协调器初始化异常");
                // 模块协调器初始化失败不应阻塞应用启动
            }
        }

        /// <summary>
        /// 根据用户角色加载模块
        /// </summary>
        public Task LoadModulesForRoleAsync(UserRole userRole)
        {
            try
            {
                _logger.LogInformation("开始为角色 {UserRole} 加载模块", userRole);

                // 根据角色确定需要加载的模块
                var modulesToLoad = DetermineModulesToLoad(userRole);

                foreach (var moduleName in modulesToLoad)
                {
                    try
                    {
                        _moduleManager.LoadModule(moduleName);
                        _logger.LogInformation("模块 {ModuleName} 加载成功", moduleName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "模块 {ModuleName} 加载失败", moduleName);
                    }
                }

                _logger.LogInformation("角色 {UserRole} 的模块加载完成", userRole);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为角色 {UserRole} 加载模块时发生错误", userRole);
                throw;
            }
        }

        /// <summary>
        /// 订阅模块事件
        /// </summary>
        private void SubscribeToModuleEvents()
        {
            var moduleInitTimes = new System.Collections.Generic.Dictionary<string, DateTime>();

            // 模块开始加载事件
            _moduleManager.ModuleDownloadProgressChanged += (sender, e) =>
            {
                if (e.ProgressPercentage == 0) // 开始加载
                {
                    moduleInitTimes[e.ModuleInfo.ModuleName] = DateTime.Now;
                    _logger.LogDebug("模块 {ModuleName} 开始加载", e.ModuleInfo.ModuleName);
                }
            };

            // 模块加载完成事件
            _moduleManager.LoadModuleCompleted += (sender, e) =>
            {
                var moduleName = e.ModuleInfo.ModuleName;
                if (moduleInitTimes.TryGetValue(moduleName, out var startTime))
                {
                    var initializationTime = DateTime.Now - startTime;
                    moduleInitTimes.Remove(moduleName);

                    _logger.LogInformation("模块 {ModuleName} 加载完成，耗时 {ElapsedTime}ms",
                        moduleName, initializationTime.TotalMilliseconds);
                }

                if (e.Error != null)
                {
                    _logger.LogError(e.Error, "模块 {ModuleName} 加载失败", moduleName);
                }
            };
        }

        /// <summary>
        /// 根据用户角色确定需要加载的模块
        /// </summary>
        private string[] DetermineModulesToLoad(UserRole userRole)
        {
            // 基础模块 - 所有角色都需要
            var baseModules = new[]
            {
                "AuthModule",
                "UsersModule"
            };

            // 根据角色添加特定模块
            var roleSpecificModules = userRole switch
            {
                UserRole.Admin => new[]
                {
                    "PatientsModule",
                    "HerbsModule",
                    "FormulaModule",
                    "MedicalCaseModule",
                    "ConsultationModule",
                    "PrescriptionsModule",
                    "ClinicalWorkstationModule"
                },
                UserRole.Doctor => new[]
                {
                    "PatientsModule",
                    "HerbsModule",
                    "FormulaModule",
                    "MedicalCaseModule",
                    "ConsultationModule",
                    "PrescriptionsModule",
                    "ClinicalWorkstationModule"
                },
                // UserRole.Pharmacist 已统一到 Doctor 角色
                _ => Array.Empty<string>()
            };

            return baseModules.Concat(roleSpecificModules).ToArray();
        }
    }
}
