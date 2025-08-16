using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using LYBT.Desktop.Core.Configuration;

using LYBT.Desktop.Core.Models.Users;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 用户会话管理器实现
    /// </summary>
    public class UserSessionManager : IUserSessionManager
    {
        private readonly IPermissionService _permissionService;
        private UserInfo? _currentUser;
        private string? _sessionToken;
        private DateTime? _loginTime;

        public UserSessionManager(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        /// <summary>
        /// 当前登录用户
        /// </summary>
        public UserInfo? CurrentUser => _currentUser;

        /// <summary>
        /// 是否已登录
        /// </summary>
        public bool IsLoggedIn => _currentUser != null && !string.IsNullOrEmpty(_sessionToken);

        /// <summary>
        /// 登录时间
        /// </summary>
        public DateTime? LoginTime => _loginTime;

        /// <summary>
        /// 会话令牌
        /// </summary>
        public string? SessionToken => _sessionToken;

        /// <summary>
        /// 设置用户会话
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <param name="token">会话令牌</param>
        public void SetUserSession(UserInfo user, string token)
        {
            _currentUser = user ?? throw new ArgumentNullException(nameof(user));
            _sessionToken = token ?? throw new ArgumentNullException(nameof(token));
            _loginTime = DateTime.Now;
        }

        /// <summary>
        /// 清除用户会话
        /// </summary>
        public void ClearUserSession()
        {
            _currentUser = null;
            _sessionToken = null;
            _loginTime = null;
        }

        /// <summary>
        /// 检查用户是否有指定权限
        /// </summary>
        /// <param name="permission">权限名称</param>
        /// <returns>是否有权限</returns>
        public bool HasPermission(string permission)
        {
            if (_currentUser == null) return false;
            return _permissionService.HasPermission(_currentUser, permission);
        }

        /// <summary>
        /// 检查用户是否有指定角色
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>是否有该角色</returns>
        public bool HasRole(string role)
        {
            // 不再有角色概念
            return false;
        }

        /// <summary>
        /// 检查用户是否有管理员权限
        /// </summary>
        /// <returns>是否有管理员权限</returns>
        public bool IsAdmin()
        {
            return _currentUser?.Username == "sysadmin";
        }

        /// <summary>
        /// 检查用户是否有超级管理员权限
        /// </summary>
        /// <returns>是否有超级管理员权限</returns>
        public bool IsSuperAdmin()
        {
            return _currentUser?.Username == "sysadmin";
        }

        /// <summary>
        /// 获取当前用户的角色显示名称
        /// </summary>
        /// <returns>角色显示名称</returns>
        public string GetCurrentUserRoleDisplay()
        {
            if (_currentUser == null) return "未登录";
            return _currentUser?.Username == "sysadmin" ? "管理员" : "用户";
        }

        /// <summary>
        /// 刷新用户信息
        /// </summary>
        /// <param name="user">更新的用户信息</param>
        public void RefreshUserInfo(UserInfo user)
        {
            if (_currentUser != null && user.Id == _currentUser.Id)
            {
                _currentUser = user;
            }
        }

        
        /// <summary>
        /// 获取当前用户角色
        /// </summary>
        /// <returns>当前用户角色</returns>
        public UserRole? GetCurrentUserRole()
        {
            if (_currentUser == null) return UserRole.Admin; // 默认角色

            // 简化的角色判断逻辑：只有管理员和普通用户
            return _currentUser.Username == "sysadmin" ? UserRole.Admin : UserRole.Doctor;
        }

        /// <summary>
        /// 检查用户是否有指定角色
        /// </summary>
        /// <param name="role">角色</param>
        /// <returns>是否有该角色</returns>
        public bool HasUserRole(UserRole role)
        {
            var currentRole = GetCurrentUserRole();
            return currentRole.HasValue && currentRole.Value == role;
        }

        /// <summary>
        /// 检查是否可访问指定模块
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <returns>是否可访问</returns>
        public bool CanAccessModule(string moduleName)
        {
            var currentRole = GetCurrentUserRole();
            return currentRole.HasValue && _permissionService.CanAccessModule(currentRole.Value, moduleName);
        }

        /// <summary>
        /// 获取可访问的模块列表
        /// </summary>
        /// <returns>模块列表</returns>
        public IEnumerable<string> GetAccessibleModules()
        {
            var currentRole = GetCurrentUserRole();
            return currentRole.HasValue ? _permissionService.GetAccessibleModules(currentRole.Value) : 
                   Enumerable.Empty<string>();
        }

        /// <summary>
        /// 检查是否有管理权限
        /// </summary>
        /// <returns>是否有管理权限</returns>
        public bool HasManagementAccess()
        {
            var currentRole = GetCurrentUserRole();
            return currentRole.HasValue && _permissionService.HasManagementAccess(currentRole.Value);
        }

        /// <summary>
        /// 检查是否有医疗权限
        /// </summary>
        /// <returns>是否有医疗权限</returns>
        public bool HasMedicalAccess()
        {
            var currentRole = GetCurrentUserRole();
            return currentRole.HasValue && _permissionService.HasMedicalAccess(currentRole.Value);
        }

        /// <summary>
        /// 获取当前用户工作台类型
        /// </summary>
        /// <returns>工作台类型</returns>
        public string GetCurrentUserWorkbench()
        {
            var currentRole = GetCurrentUserRole();
            return currentRole switch
            {
                UserRole.Admin => "AdminWorkbench",
                UserRole.Doctor => "ConsultationWorkbench", 
                UserRole.Receptionist => "ReceptionistWorkbench",
                UserRole.Cashier => "CashierWorkbench",
                UserRole.Pharmacist => "PharmacistWorkbench",
                UserRole.Therapist => "TherapistWorkbench",
                null => "ConsultationWorkbench",
                _ => "ConsultationWorkbench"
            };
        }
    }
}