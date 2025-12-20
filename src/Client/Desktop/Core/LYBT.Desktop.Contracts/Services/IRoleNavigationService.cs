using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// 角色导航服务接口
    /// 根据用户角色自动路由到对应的主页视图
    /// Issue #1553: 角色模块化重构
    /// OpenSpec: refactor-role-navigation
    /// </summary>
    public interface IRoleNavigationService
    {
        /// <summary>
        /// 根据角色名称导航到对应的角色主页
        /// </summary>
        /// <param name="roleName">角色名称（Doctor/Admin/Receptionist/Pharmacist）</param>
        void NavigateToRoleHome(string roleName);

        /// <summary>
        /// 导航到当前用户角色对应的主页
        /// OpenSpec: refactor-role-navigation
        /// </summary>
        void NavigateToHome();

        /// <summary>
        /// 获取当前用户角色对应的主页视图名称
        /// OpenSpec: refactor-role-navigation
        /// </summary>
        string GetHomeViewForCurrentRole();

        /// <summary>
        /// 当前用户角色
        /// OpenSpec: refactor-role-navigation
        /// </summary>
        UserRole? CurrentUserRole { get; }
    }
}
