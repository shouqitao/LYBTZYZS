using LYBT.Desktop.Contracts.Performance;
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
    /// Phase 4 Task 4.4: 集成模块加载性能监控
    /// </summary>
    public class ApplicationBootstrapper : IApplicationBootstrapper
    {
        private readonly IModuleManager _moduleManager;
        private readonly IRoleRegistry _roleRegistry;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILogger<ApplicationBootstrapper> _logger;

        public ApplicationBootstrapper(
            IModuleManager moduleManager,
            IRoleRegistry roleRegistry,
            IPerformanceMonitor performanceMonitor,
            ILogger<ApplicationBootstrapper> logger)
        {
            _moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
            _roleRegistry = roleRegistry ?? throw new ArgumentNullException(nameof(roleRegistry));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task LoadModulesForRoleAsync(UserRole userRole)
        {
            try
            {
                _logger.LogInformation("开始为角色 {UserRole} 加载模块", userRole);

                // Phase 4 Task 4.4: 开始模块加载性能监控
                var totalTimingKey = $"ModuleLoading_Role_{userRole}";
                _performanceMonitor.StartTiming(totalTimingKey);

                // 从RoleRegistry获取模块列表
                var modulesToLoad = _roleRegistry.GetModulesForRole(userRole);
                var loadedCount = 0;
                var failedCount = 0;

                foreach (var moduleName in modulesToLoad)
                {
                    // Phase 4 Task 4.4: 监控单个模块加载
                    var moduleTimingKey = $"ModuleLoading_{moduleName}";
                    _performanceMonitor.StartTiming(moduleTimingKey);

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
                    finally
                    {
                        _performanceMonitor.StopTiming(moduleTimingKey);
                    }
                }

                // Phase 4 Task 4.4: 停止总加载时间监控
                _performanceMonitor.StopTiming(totalTimingKey);

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
