namespace LYBT.Desktop.Infrastructure.Interfaces
{
    /// <summary>
    /// 角色导航服务接口
    /// 根据用户角色自动路由到对应的主页视图
    /// Issue #1553: 角色模块化重构
    /// </summary>
    public interface IRoleNavigationService
    {
        /// <summary>
        /// 根据角色名称导航到对应的角色主页
        /// </summary>
        /// <param name="roleName">角色名称（Doctor/Admin/Receptionist/Pharmacist）</param>
        void NavigateToRoleHome(string roleName);
    }
}
