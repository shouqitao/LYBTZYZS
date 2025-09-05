using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Desktop.Core.Configuration;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 用户会话管理器 - UltraThink精简版
    /// </summary>
    public class UserSessionManager : IUserSessionManager, ITokenManager
    {
        private readonly IPermissionService _permissionService;
        private UserDto? _currentUser;
        private string? _sessionToken;
        private DateTime? _loginTime;

        public UserSessionManager(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public UserDto? CurrentUser => _currentUser;
        public bool IsLoggedIn => _currentUser != null && !string.IsNullOrEmpty(_sessionToken);
        public DateTime? LoginTime => _loginTime;
        public string? SessionToken => _sessionToken;

        public void SetUserSession(UserDto user, string token)
        {
            _currentUser = user ?? throw new ArgumentNullException(nameof(user));
            _sessionToken = token ?? throw new ArgumentNullException(nameof(token));
            _loginTime = DateTime.Now;
        }

        public void ClearUserSession()
        {
            _currentUser = null;
            _sessionToken = null;
            _loginTime = null;
        }

        public bool HasPermission(string permission)
            => _currentUser != null && _permissionService.HasPermission(_currentUser, permission);

        public bool HasRole(string role) => false; // 已废弃

        public bool IsAdmin() => _currentUser?.Username == "sysadmin";

        public bool IsSuperAdmin() => IsAdmin(); // 简化：与IsAdmin相同

        public string GetCurrentUserRoleDisplay()
            => _currentUser == null ? "未登录" : (IsAdmin() ? "管理员" : "医生");

        public void RefreshUserInfo(UserDto user)
        {
            if (_currentUser?.Id == user?.Id)
            {
                _currentUser = user;
            }
        }

        public UserRole? GetCurrentUserRole()
            => _currentUser == null ? UserRole.Doctor : (IsAdmin() ? UserRole.Admin : UserRole.Doctor);

        public bool HasUserRole(UserRole role)
            => GetCurrentUserRole() == role;

        public bool CanAccessModule(string moduleName)
        {
            var role = GetCurrentUserRole();
            return role.HasValue && _permissionService.CanAccessModule(role.Value, moduleName);
        }

        public IEnumerable<string> GetAccessibleModules()
        {
            var role = GetCurrentUserRole();
            return role.HasValue ? _permissionService.GetAccessibleModules(role.Value) : Enumerable.Empty<string>();
        }

        public bool HasManagementAccess()
        {
            var role = GetCurrentUserRole();
            return role.HasValue && _permissionService.HasManagementAccess(role.Value);
        }

        public bool HasMedicalAccess()
        {
            var role = GetCurrentUserRole();
            return role.HasValue && _permissionService.HasMedicalAccess(role.Value);
        }

        public string GetCurrentUserWorkbench() => "MainWorkspace"; // 主工作区

        #region ITokenManager 实现

        public string? GetToken() => _sessionToken;

        public void SetToken(string token)
            => _sessionToken = token ?? throw new ArgumentNullException(nameof(token));

        public void ClearToken() => _sessionToken = null;

        #endregion
    }
}
