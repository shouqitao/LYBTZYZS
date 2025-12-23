using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 角色导航服务实现
    /// 根据用户角色自动路由到对应的主页视图
    /// Issue #1553: 角色模块化重构
    /// OpenSpec: refactor-role-navigation
    /// refactor-auth-role-system Phase 2.3.2: 使用RoleRegistry动态获取主页视图
    /// </summary>
    public class RoleNavigationService : IRoleNavigationService
    {
        private readonly IRegionManager _regionManager;
        private readonly ISessionManager _sessionManager;
        private readonly IRoleRegistry _roleRegistry;
        private readonly ILogger<RoleNavigationService> _logger;

        public RoleNavigationService(
            IRegionManager regionManager,
            ISessionManager sessionManager,
            IRoleRegistry roleRegistry,
            ILogger<RoleNavigationService> logger)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _roleRegistry = roleRegistry ?? throw new ArgumentNullException(nameof(roleRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 当前用户角色
        /// OpenSpec: refactor-role-navigation
        /// </summary>
        public UserRole? CurrentUserRole => _sessionManager.CurrentUser?.Role;

        /// <summary>
        /// 根据角色名称导航到对应的角色主页
        /// Issue #1909: 四角色体系（SuperAdmin/Admin/Doctor/Receptionist）
        /// refactor-auth-role-system Phase 2.3.2: 使用RoleRegistry动态获取主页视图
        /// </summary>
        /// <param name="roleName">角色名称（SuperAdmin/Admin/Doctor/Receptionist）</param>
        public void NavigateToRoleHome(string roleName)
        {
            try
            {
                // 解析角色名称为枚举
                if (!Enum.TryParse<UserRole>(roleName, ignoreCase: true, out var role))
                {
                    _logger.LogError("无法解析角色名称: {RoleName}", roleName);
                    throw new ArgumentException($"未知角色: {roleName}", nameof(roleName));
                }

                // 从RoleRegistry获取主页视图
                var viewName = _roleRegistry.GetHomeViewName(role);
                _logger.LogInformation("角色 {RoleName} 导航到视图 {ViewName}", roleName, viewName);

                _regionManager.RequestNavigate("ContentRegion", viewName, navigationResult =>
                {
                    if (navigationResult.Result == true)
                    {
                        _logger.LogInformation("角色导航成功：{ViewName}", viewName);
                    }
                    else
                    {
                        _logger.LogError("角色导航失败：{ViewName}，错误：{Error}",
                            viewName, navigationResult.Error?.Message ?? "未知错误");
                        if (navigationResult.Error != null)
                        {
                            _logger.LogError(navigationResult.Error, "角色导航异常详情");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "角色导航异常：{RoleName}", roleName);
                throw;
            }
        }

        /// <summary>
        /// 导航到当前用户角色对应的主页
        /// OpenSpec: refactor-role-navigation
        /// </summary>
        public void NavigateToHome()
        {
            var viewName = GetHomeViewForCurrentRole();
            _logger.LogInformation("导航到主页: {ViewName}", viewName);

            _regionManager.RequestNavigate("ContentRegion", viewName, navigationResult =>
            {
                if (navigationResult.Result == true)
                {
                    _logger.LogInformation("返回主页成功: {ViewName}", viewName);
                }
                else
                {
                    _logger.LogError("返回主页失败: {ViewName}，错误: {Error}",
                        viewName, navigationResult.Error?.Message ?? "未知错误");
                }
            });
        }

        /// <summary>
        /// 获取当前用户角色对应的主页视图名称
        /// OpenSpec: refactor-role-navigation
        /// refactor-auth-role-system Phase 2.3.2: 使用RoleRegistry动态获取主页视图
        /// </summary>
        public string GetHomeViewForCurrentRole()
        {
            var role = _sessionManager.CurrentUser?.Role;
            if (role == null)
            {
                _logger.LogWarning("当前用户角色为空，使用默认主页视图");
                return "ClinicalHomeView";
            }

            return _roleRegistry.GetHomeViewName(role.Value);
        }
    }
}
