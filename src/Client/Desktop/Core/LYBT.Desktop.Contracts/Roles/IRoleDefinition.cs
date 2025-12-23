using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Contracts.Roles
{
    /// <summary>
    /// 角色定义接口 - 定义角色的模块加载和导航行为
    /// refactor-auth-role-system Phase 2.1.1
    /// </summary>
    /// <remarks>
    /// 每个角色实现此接口以定义:
    /// 1. 需要加载的模块列表
    /// 2. 主页视图名称
    /// 3. 角色描述信息
    /// </remarks>
    public interface IRoleDefinition
    {
        /// <summary>
        /// 角色枚举值
        /// </summary>
        UserRole Role { get; }

        /// <summary>
        /// 角色名称（用于显示）
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 角色描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 主页视图名称（用于导航）
        /// </summary>
        string HomeViewName { get; }

        /// <summary>
        /// 该角色需要加载的模块列表
        /// </summary>
        IReadOnlyList<string> RequiredModules { get; }

        /// <summary>
        /// 基础模块（所有角色共享）
        /// </summary>
        IReadOnlyList<string> BaseModules { get; }

        /// <summary>
        /// 获取该角色需要加载的所有模块（基础模块 + 角色特定模块）
        /// </summary>
        IEnumerable<string> GetAllModules();
    }
}
