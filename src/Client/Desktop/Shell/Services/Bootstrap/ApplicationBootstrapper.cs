using LYBT.Desktop.Contracts.Roles;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Modularity;

namespace LYBT.Desktop.Shell.Services.Bootstrap
{
    /// <summary>
    /// 应用程序启动引导服务实现
    /// 职责：角色驱动的模块加载
    /// refactor-auth-role-system Phase 2.3.1: 使用RoleRegistry进行动态模块加载
    /// </summary>
    public class ApplicationBootstrapper : IApplicationBootstrapper
    {
        private readonly IModuleManager _moduleManager;
        private readonly IRoleRegistry _roleRegistry;
        private readonly ILogger<ApplicationBootstrapper> _logger;

        public ApplicationBootstrapper(
            IModuleManager moduleManager,
            IRoleRegistry roleRegistry,
            ILogger<ApplicationBootstrapper> logger)
        {
            _moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
            _roleRegistry = roleRegistry ?? throw new ArgumentNullException(nameof(roleRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task LoadModulesForRoleAsync(UserRole userRole)
        {
            try
            {
                _logger.LogInformation("开始为角色 {UserRole} 加载模块", userRole);

                // 从RoleRegistry获取模块列表
                var modulesToLoad = _roleRegistry.GetModulesForRole(userRole);
                var loadedCount = 0;
                var failedCount = 0;

                foreach (var moduleName in modulesToLoad)
                {
                    try
                    {
                        _moduleManager.LoadModule(moduleName);
                        loadedCount++;
                        _logger.LogDebug("模块 {ModuleName} 加载成功", moduleName);
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        _logger.LogError(ex, "模块 {ModuleName} 加载失败", moduleName);
                    }
                }

                _logger.LogInformation(
                    "角色 {UserRole} 模块加载完成: 成功 {LoadedCount}, 失败 {FailedCount}",
                    userRole, loadedCount, failedCount);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为角色 {UserRole} 加载模块时发生错误", userRole);
                throw;
            }
        }
    }
}
