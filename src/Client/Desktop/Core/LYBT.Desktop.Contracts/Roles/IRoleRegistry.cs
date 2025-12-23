using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Contracts.Roles
{
    /// <summary>
    /// 角色注册表接口 - 管理所有角色定义
    /// refactor-auth-role-system Phase 2.1.2
    /// </summary>
    public interface IRoleRegistry
    {
        /// <summary>
        /// 注册角色定义
        /// </summary>
        /// <param name="roleDefinition">角色定义</param>
        void Register(IRoleDefinition roleDefinition);

        /// <summary>
        /// 获取指定角色的定义
        /// </summary>
        /// <param name="role">角色枚举</param>
        /// <returns>角色定义，如果未注册返回null</returns>
        IRoleDefinition? GetDefinition(UserRole role);

        /// <summary>
        /// 获取所有已注册的角色定义
        /// </summary>
        IReadOnlyCollection<IRoleDefinition> GetAllDefinitions();

        /// <summary>
        /// 检查角色是否已注册
        /// </summary>
        /// <param name="role">角色枚举</param>
        bool IsRegistered(UserRole role);

        /// <summary>
        /// 获取指定角色的主页视图名称
        /// </summary>
        /// <param name="role">角色枚举</param>
        /// <returns>视图名称</returns>
        string GetHomeViewName(UserRole role);

        /// <summary>
        /// 获取指定角色需要加载的所有模块
        /// </summary>
        /// <param name="role">角色枚举</param>
        /// <returns>模块名称列表</returns>
        IEnumerable<string> GetModulesForRole(UserRole role);
    }
}
