using LYBT.Shared.Models.Contracts.Common;
using System;
using LYBT.Desktop.Core.Models.Users;
using System.Collections.Generic;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Core;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// 权限服务接口
    /// </summary>
    /// <summary>
    /// UltraThink Phase 4.2: 基于UserRole枚举的权限服务接口扩展
    /// 支持新的UserRole枚举同时保持向后兼容
    /// </summary>

    public interface IPermissionService
    {
        /// <summary>
        /// 检查用户是否有指定权限
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <param name="permission">权限名称</param>
        /// <returns>是否有权限</returns>
        bool HasPermission(UserInfo user, string permission);

        /// <summary>
        /// 检查用户是否有管理员权限
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <returns>是否有管理员权限</returns>
        bool HasAdminPermission(UserInfo user);

        /// <summary>
        /// 检查用户是否有超级管理员权限
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <returns>是否有超级管理员权限</returns>
        bool HasSuperAdminPermission(UserInfo user);

        /// <summary>
        /// 获取用户可访问的模块列表
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <returns>可访问的模块列表</returns>
        List<string> GetAccessibleModules(UserInfo user);

        /// <summary>
        /// 获取用户角色的显示名称
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <returns>角色显示名称</returns>
        string GetRoleDisplayName(string role);

        // UltraThink Phase 4.2: 基于UserRole枚举的新方法

        /// <summary>
        /// 检查UserRole是否可以访问指定模块
        /// </summary>
        /// <param name="role">用户角色枚举</param>
        /// <param name="module">模块名称</param>
        /// <returns>是否可以访问</returns>
        bool CanAccessModule(UserRole role, string module);

        /// <summary>
        /// 获取UserRole可访问的所有模块
        /// </summary>
        /// <param name="role">用户角色枚举</param>
        /// <returns>可访问的模块列表</returns>
        IEnumerable<string> GetAccessibleModules(UserRole role);

        /// <summary>
        /// 获取UserRole的显示名称
        /// </summary>
        /// <param name="role">用户角色枚举</param>
        /// <returns>角色显示名称</returns>
        string GetRoleDisplayName(UserRole role);

        /// <summary>
        /// 检查UserRole是否有管理权限
        /// </summary>
        /// <param name="role">用户角色枚举</param>
        /// <returns>是否有管理权限</returns>
        bool HasManagementAccess(UserRole role);

        /// <summary>
        /// 检查UserRole是否有医疗权限
        /// </summary>
        /// <param name="role">用户角色枚举</param>
        /// <returns>是否有医疗权限</returns>
        bool HasMedicalAccess(UserRole role);

        /// <summary>
        /// 检查用户是否有指定权限（基于UserRole）
        /// </summary>
        /// <param name="role">用户角色枚举</param>
        /// <param name="permission">权限名称</param>
        /// <returns>是否有权限</returns>
        bool HasPermission(UserRole role, string permission);
    }
}