using LYBT.Desktop.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 角色导航服务实现
    /// 根据用户角色自动路由到对应的主页视图
    /// Issue #1553: 角色模块化重构
    /// </summary>
    public class RoleNavigationService : IRoleNavigationService
    {
        private readonly IRegionManager _regionManager;
        private readonly ILogger<RoleNavigationService> _logger;

        public RoleNavigationService(
            IRegionManager regionManager,
            ILogger<RoleNavigationService> logger)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 根据角色名称导航到对应的角色主页
        /// </summary>
        /// <param name="roleName">角色名称（Doctor/Admin/Receptionist/Pharmacist）</param>
        public void NavigateToRoleHome(string roleName)
        {
            try
            {
                var viewName = roleName switch
                {
                    "Doctor" => "ClinicalHomeView",
                    "Admin" => "AdminHomeView",
                    // MVP后期扩展
                    "Receptionist" => "ReceptionHomeView",
                    "Pharmacist" => "PharmacyHomeView",
                    _ => throw new ArgumentException($"未知角色: {roleName}", nameof(roleName))
                };

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
    }
}
