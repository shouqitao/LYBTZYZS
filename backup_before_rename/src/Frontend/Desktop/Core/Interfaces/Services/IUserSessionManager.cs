using System;
using LYBT.Desktop.Core.Models.Users;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// 用户会话管理器接口
    /// </summary>
    public interface IUserSessionManager
    {
        /// <summary>
        /// 当前登录用户
        /// </summary>
        UserInfo? CurrentUser { get; }

        /// <summary>
        /// 是否已登录
        /// </summary>
        bool IsLoggedIn { get; }

        /// <summary>
        /// 登录时间
        /// </summary>
        DateTime? LoginTime { get; }

        /// <summary>
        /// 会话令牌
        /// </summary>
        string? SessionToken { get; }

        /// <summary>
        /// 设置用户会话
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <param name="token">会话令牌</param>
        void SetUserSession(UserInfo user, string token);

        /// <summary>
        /// 清除用户会话
        /// </summary>
        void ClearUserSession();

        /// <summary>
        /// 检查用户是否有指定权限
        /// </summary>
        /// <param name="permission">权限名称</param>
        /// <returns>是否有权限</returns>
        bool HasPermission(string permission);

        /// <summary>
        /// 检查用户是否有指定角色
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>是否有该角色</returns>
        bool HasRole(string role);

        /// <summary>
        /// 检查用户是否有管理员权限
        /// </summary>
        /// <returns>是否有管理员权限</returns>
        bool IsAdmin();

        /// <summary>
        /// 检查用户是否有超级管理员权限
        /// </summary>
        /// <returns>是否有超级管理员权限</returns>
        bool IsSuperAdmin();

        /// <summary>
        /// 获取当前用户的角色显示名称
        /// </summary>
        /// <returns>角色显示名称</returns>
        string GetCurrentUserRoleDisplay();

        /// <summary>
        /// 刷新用户信息
        /// </summary>
        /// <param name="user">更新的用户信息</param>
        void RefreshUserInfo(UserInfo user);
    }
}