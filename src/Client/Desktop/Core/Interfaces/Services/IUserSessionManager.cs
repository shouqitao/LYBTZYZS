using LYBT.Shared.Models.Contracts.Common;
using System;
using LYBT.Desktop.Core.Redux.States;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Interfaces.Services;

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
        UserDto? CurrentUser { get; }

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
        void SetUserSession(UserDto user, string token);

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
        void RefreshUserInfo(UserDto user);

        // UltraThink Phase 4.3: 基于UserRole枚举的新方法

        /// <summary>
        /// 获取当前用户的UserRole
        /// </summary>
        /// <returns>当前用户角色，null表示未登录</returns>
        UserRole? GetCurrentUserRole();

        /// <summary>
        /// 检查当前用户是否具有指定的UserRole
        /// </summary>
        /// <param name="role">要检查的角色</param>
        /// <returns>是否具有该角色</returns>
        bool HasUserRole(UserRole role);

        /// <summary>
        /// 检查当前用户是否可以访问指定模块
        /// </summary>
        /// <param name="module">模块名称</param>
        /// <returns>是否可以访问</returns>
        bool CanAccessModule(string module);

        /// <summary>
        /// 获取当前用户可访问的所有模块
        /// </summary>
        /// <returns>可访问的模块列表</returns>
        IEnumerable<string> GetAccessibleModules();

        /// <summary>
        /// 检查当前用户是否有管理权限（基于UserRole）
        /// </summary>
        /// <returns>是否有管理权限</returns>
        bool HasManagementAccess();

        /// <summary>
        /// 检查当前用户是否有医疗权限（基于UserRole）
        /// </summary>
        /// <returns>是否有医疗权限</returns>
        bool HasMedicalAccess();

        /// <summary>
        /// 获取当前用户对应的工作台视图名称
        /// </summary>
        /// <returns>工作台视图名称</returns>
        string GetCurrentUserWorkbench();
    }

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// 主窗口服务门面，用于简化MainWindowViewModel的依赖注入
    /// </summary>
    public interface IMainWindowServicesFacade
    {
        /// <summary>
        /// 认证服务
        /// </summary>
        IAuthenticationService AuthenticationService { get; }

        /// <summary>
        /// 权限服务
        /// </summary>
        IPermissionService PermissionService { get; }

        /// <summary>
        /// 对话框服务
        /// </summary>
        ICustomDialogService CustomDialogService { get; }

        /// <summary>
        /// 用户服务
        /// </summary>
        IUserService UserService { get; }

        /// <summary>
        /// 患者服务
        /// </summary>
        IPatientService PatientService { get; }
    }
}
}