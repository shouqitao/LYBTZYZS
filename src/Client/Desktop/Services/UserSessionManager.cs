using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Users;
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

        /// <inheritdoc/>
        public UserDto? CurrentUser => _currentUser;

        /// <inheritdoc/>
        public bool IsLoggedIn => _currentUser != null && !string.IsNullOrEmpty(_sessionToken);

        /// <inheritdoc/>
        public DateTime? LoginTime => _loginTime;

        /// <inheritdoc/>
        public string? SessionToken => _sessionToken;

        /// <inheritdoc/>
        public void SetUserSession(UserDto user, string token)
        {
            _currentUser = user ?? throw new ArgumentNullException(nameof(user));
            _sessionToken = token ?? throw new ArgumentNullException(nameof(token));
            _loginTime = DateTime.Now;
        }

        /// <inheritdoc/>
        public void ClearUserSession()
        {
            _currentUser = null;
            _sessionToken = null;
            _loginTime = null;
        }

        /// <inheritdoc/>
        public bool HasPermission(string permission)
            => _currentUser != null && _permissionService.HasPermission(_currentUser, permission);

        /// <inheritdoc/>
        public bool HasRole(string role) => false; // 已废弃

        /// <inheritdoc/>
        public bool IsAdmin() => _currentUser?.Username == "sysadmin";

        /// <inheritdoc/>
        public bool IsSuperAdmin() => IsAdmin(); // 简化：与IsAdmin相同

        /// <inheritdoc/>
        public string GetCurrentUserRoleDisplay()
            => _currentUser == null ? "未登录" : (IsAdmin() ? "管理员" : "医生");

        /// <inheritdoc/>
        public void RefreshUserInfo(UserDto user)
        {
            if (_currentUser?.Id == user?.Id)
            {
                _currentUser = user;
            }
        }

        /// <inheritdoc/>
        public UserRole? GetCurrentUserRole()
            => _currentUser == null ? UserRole.Doctor : (IsAdmin() ? UserRole.Admin : UserRole.Doctor);

        /// <inheritdoc/>
        public bool HasUserRole(UserRole role)
            => GetCurrentUserRole() == role;

        /// <inheritdoc/>
        public bool CanAccessModule(string moduleName)
        {
            var role = GetCurrentUserRole();
            return role.HasValue && _permissionService.CanAccessModule(role.Value, moduleName);
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetAccessibleModules()
        {
            var role = GetCurrentUserRole();
            return role.HasValue ? _permissionService.GetAccessibleModules(role.Value) : Enumerable.Empty<string>();
        }

        /// <inheritdoc/>
        public bool HasManagementAccess()
        {
            var role = GetCurrentUserRole();
            return role.HasValue && _permissionService.HasManagementAccess(role.Value);
        }

        /// <inheritdoc/>
        public bool HasMedicalAccess()
        {
            var role = GetCurrentUserRole();
            return role.HasValue && _permissionService.HasMedicalAccess(role.Value);
        }

        /// <inheritdoc/>
        public string GetCurrentUserWorkbench() => "MainWorkspace"; // 主工作区

        #region ITokenManager 实现

        /// <inheritdoc/>
        public string? GetToken() => _sessionToken;

        /// <inheritdoc/>
        public void SetToken(string token)
            => _sessionToken = token ?? throw new ArgumentNullException(nameof(token));

        /// <inheritdoc/>
        public void ClearToken() => _sessionToken = null;

        #endregion ITokenManager 实现
    }
}
