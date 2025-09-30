using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Interfaces
{
    /// <summary>
    /// 权限服务接口 - UltraThink架构权限抽象
    /// 负责用户权限验证、角色检查、功能访问控制
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// 检查用户是否有指定权限
        /// </summary>
        /// <param name="permission">权限标识</param>
        /// <returns>是否有权限</returns>
        bool HasPermission(string permission);

        /// <summary>
        /// 检查用户是否有指定角色
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>是否有角色</returns>
        bool HasRole(UserRole role);

        /// <summary>
        /// 检查是否为管理员
        /// </summary>
        /// <returns>是否为管理员</returns>
        bool IsAdmin();

        /// <summary>
        /// 检查是否为医生
        /// </summary>
        /// <returns>是否为医生</returns>
        bool IsDoctor();

        /// <summary>
        /// 获取当前用户角色显示名称
        /// </summary>
        /// <returns>角色显示名称</returns>
        string GetCurrentUserRoleDisplay();

        /// <summary>
        /// 检查功能模块访问权限
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <returns>是否可访问</returns>
        bool CanAccessModule(string moduleName);

        /// <summary>
        /// 检查操作权限
        /// </summary>
        /// <param name="operation">操作名称</param>
        /// <returns>是否可执行</returns>
        bool CanExecuteOperation(string operation);

        /// <summary>
        /// 获取用户可访问的模块列表
        /// </summary>
        /// <returns>模块名称列表</returns>
        IEnumerable<string> GetAccessibleModules();
    }
}