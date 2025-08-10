using System.Collections.Generic;

namespace LYBT.WPF.Client.Workbenches.Core
{
    /// <summary>
    /// 工作台路由接口
    /// 管理角色到工作台的映射和导航
    /// </summary>
    public interface IWorkbenchRouter
    {
        /// <summary>
        /// 根据用户角色获取对应的工作台视图名称
        /// </summary>
        /// <param name="role">用户角色（管理员、医生、前台等）</param>
        /// <returns>工作台视图名称</returns>
        string GetWorkbenchForRole(string role);

        /// <summary>
        /// 检查角色是否可以访问指定模块
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <param name="module">模块名称</param>
        /// <returns>是否有访问权限</returns>
        bool CanAccessModule(string role, string module);

        /// <summary>
        /// 获取角色对应的导航项列表
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>导航项列表</returns>
        IEnumerable<NavigationItem> GetNavigationItems(string role);

        /// <summary>
        /// 获取角色可访问的模块列表
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>可访问的模块名称列表</returns>
        IEnumerable<string> GetAccessibleModules(string role);

        /// <summary>
        /// 获取工作台的默认视图
        /// </summary>
        /// <param name="workbench">工作台名称</param>
        /// <returns>默认视图名称</returns>
        string GetDefaultView(string workbench);

        /// <summary>
        /// 注册新的工作台
        /// 用于扩展新角色时动态注册
        /// </summary>
        /// <param name="role">角色名称</param>
        /// <param name="workbench">工作台名称</param>
        /// <param name="modules">可访问的模块列表</param>
        void RegisterWorkbench(string role, string workbench, List<string> modules);

        /// <summary>
        /// 获取所有已注册的工作台
        /// </summary>
        /// <returns>工作台信息字典</returns>
        Dictionary<string, string> GetAllWorkbenches();

        /// <summary>
        /// 检查工作台是否已注册
        /// </summary>
        /// <param name="workbench">工作台名称</param>
        /// <returns>是否已注册</returns>
        bool IsWorkbenchRegistered(string workbench);

        /// <summary>
        /// 获取角色的欢迎消息
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <param name="userName">用户姓名</param>
        /// <returns>欢迎消息</returns>
        string GetWelcomeMessage(string role, string userName);

        /// <summary>
        /// 获取角色显示名称
        /// </summary>
        /// <param name="role">角色标识</param>
        /// <returns>角色显示名称</returns>
        string GetRoleDisplayName(string role);
    }
}