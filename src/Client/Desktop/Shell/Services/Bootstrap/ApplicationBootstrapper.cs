using LYBT.Desktop.Foundation.Performance;
using LYBT.Desktop.Presentation.Notifications;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Modularity;

namespace LYBT.Desktop.Shell.Services.Bootstrap
{
    /// <summary>
    /// 应用程序启动引导服务实现
    /// 职责：角色驱动的模块加载
    /// 注意：初始化逻辑已迁移至IStartupPipeline和各StartupStep
    /// </summary>
    public class ApplicationBootstrapper : IApplicationBootstrapper
    {
        private readonly IApplicationInitializationService _initializationService;
        private readonly IStartupOptimizationService _startupOptimizationService;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly IModuleManager _moduleManager;
        private readonly ILogger<ApplicationBootstrapper> _logger;

        public ApplicationBootstrapper(
            IApplicationInitializationService initializationService,
            IStartupOptimizationService startupOptimizationService,
            IErrorHandlingService errorHandlingService,
            IModuleManager moduleManager,
            ILogger<ApplicationBootstrapper> logger)
        {
            _initializationService = initializationService ?? throw new ArgumentNullException(nameof(initializationService));
            _startupOptimizationService = startupOptimizationService ?? throw new ArgumentNullException(nameof(startupOptimizationService));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 已迁移到IStartupPipeline的方法（保留向后兼容）

        /// <inheritdoc />
        [Obsolete("已迁移至IStartupPipeline，使用CoreServicesStartupStep替代")]
        public async Task InitializeCoreServicesAsync()
        {
            _logger.LogWarning("调用已废弃的方法 InitializeCoreServicesAsync，请使用IStartupPipeline");
            await _initializationService.InitializeCoreServicesAsync();
        }

        /// <inheritdoc />
        [Obsolete("已迁移至IStartupPipeline，使用WarmupStartupStep替代")]
        public async Task InitializeApplicationWarmupAsync()
        {
            _logger.LogWarning("调用已废弃的方法 InitializeApplicationWarmupAsync，请使用IStartupPipeline");
            try
            {
                await _startupOptimizationService.WarmupApplicationAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "应用预热失败，但不影响主流程");
            }
        }

        /// <inheritdoc />
        [Obsolete("已迁移至IStartupPipeline，使用ErrorHandlingStartupStep替代")]
        public void InitializeErrorHandlingService()
        {
            _logger.LogWarning("调用已废弃的方法 InitializeErrorHandlingService，请使用IStartupPipeline");
            _errorHandlingService.RegisterGlobalExceptionHandlers();
        }

        /// <inheritdoc />
        [Obsolete("已迁移至IStartupPipeline，使用ModuleCoordinatorStartupStep替代")]
        public void InitializeSimplifiedModuleCoordinator()
        {
            _logger.LogWarning("调用已废弃的方法 InitializeSimplifiedModuleCoordinator，请使用IStartupPipeline");
            // 模块事件订阅已移至ModuleCoordinatorStartupStep
        }

        #endregion

        #region 当前使用的方法

        /// <inheritdoc />
        public Task LoadModulesForRoleAsync(UserRole userRole)
        {
            try
            {
                _logger.LogInformation("开始为角色 {UserRole} 加载模块", userRole);

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

        #endregion

        #region 私有方法

        /// <summary>
        /// 根据用户角色确定需要加载的模块
        /// </summary>
        private static string[] DetermineModulesToLoad(UserRole userRole)
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
                    "PrescriptionsModule"
                },
                UserRole.Doctor => new[]
                {
                    "PatientsModule",
                    "HerbsModule",
                    "FormulaModule",
                    "MedicalCaseModule",
                    "ConsultationModule",
                    "PrescriptionsModule"
                },
                _ => Array.Empty<string>()
            };

            return baseModules.Concat(roleSpecificModules).ToArray();
        }

        #endregion
    }
}
