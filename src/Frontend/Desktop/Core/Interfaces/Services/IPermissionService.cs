using System;
using System.Collections.Generic;
using LYBT.WPF.Client.Core.Enums;
using LYBT.WPF.Client.Core.Models.Users;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 权限服务接口
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
        string GetRoleDisplayName(UserRole role);
    }
}