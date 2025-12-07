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
        private readonly IModuleManager _moduleManager;
        private readonly ILogger<ApplicationBootstrapper> _logger;

        public ApplicationBootstrapper(
            IModuleManager moduleManager,
            ILogger<ApplicationBootstrapper> logger)
        {
            _moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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
    }
}
