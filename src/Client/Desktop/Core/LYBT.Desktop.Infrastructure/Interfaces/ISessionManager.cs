using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Interfaces
{
    /// <summary>
    /// 会话管理器接口 - UltraThink架构（功能完整版）
    /// 合并 Foundation.Session.ISessionManager 的功能，消除重复定义
    /// Issue #1194 Phase 2: 扩展功能，支持 CurrentUserId、RefreshToken 等
    /// optimize-desktop-core: 移除Token相关属性，Token由ITokenStorageService管理
    /// </summary>
    public interface ISessionManager
    {
        // ==================== 用户信息属性 ====================

        /// <summary>
        /// 当前用户
        /// </summary>
        UserDetailDto? CurrentUser { get; }

        /// <summary>
        /// 当前用户ID
        /// </summary>
        Guid? CurrentUserId { get; }

        /// <summary>
        /// 当前用户名
        /// </summary>
        string? CurrentUserName { get; }

        // ==================== 认证状态属性 ====================

        /// <summary>
        /// 是否已认证
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// 是否已登录（IsAuthenticated 的别名）
        /// </summary>
        bool IsLoggedIn { get; }

        // ==================== 会话管理方法 ====================

        /// <summary>
        /// 设置当前用户
        /// </summary>
        void SetCurrentUser(UserDetailDto user, string token);

        /// <summary>
        /// 设置会话信息（支持刷新令牌）
        /// </summary>
        void SetSession(UserDetailDto user, string accessToken, string? refreshToken = null);

        /// <summary>
        /// 设置用户会话（SetSession 的别名，兼容性保留）
        /// </summary>
        void SetUserSession(UserDetailDto user, string token);

        /// <summary>
        /// 清除会话
        /// </summary>
        void ClearSession();

        /// <summary>
        /// 清除用户会话（ClearSession 的别名，兼容性保留）
        /// </summary>
        void ClearUserSession();

        // ==================== 权限检查方法 ====================

        /// <summary>
        /// 检查权限（基于 UserRole 枚举）
        /// </summary>
        bool HasPermission(UserRole requiredRole);

        /// <summary>
        /// 检查权限（基于字符串）
        /// </summary>
        bool HasPermission(string permission);

        /// <summary>
        /// 检查角色（基于字符串）
        /// </summary>
        bool HasRole(string role);

        /// <summary>
        /// 是否为管理员
        /// </summary>
        bool IsAdmin();

        /// <summary>
        /// 获取当前用户角色显示名称
        /// </summary>
        string GetCurrentUserRoleDisplay();

        // ==================== 会话事件 ====================

        /// <summary>
        /// 会话即将过期事件
        /// </summary>
        event EventHandler? SessionExpiring;

        /// <summary>
        /// 会话已过期事件
        /// </summary>
        event EventHandler? SessionExpired;

        /// <summary>
        /// 会话变化事件
        /// </summary>
        event EventHandler<SessionChangedEventArgs>? SessionChanged;
    }

    /// <summary>
    /// 会话变化事件参数
    /// </summary>
    public class SessionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public SessionChangedEventArgs(bool isLoggedIn, UserDetailDto? user = null)
        {
            IsLoggedIn = isLoggedIn;
            User = user;
        }

        /// <summary>
        /// 是否已登录
        /// </summary>
        public bool IsLoggedIn { get; }

        /// <summary>
        /// 用户信息
        /// </summary>
        public UserDetailDto? User { get; }
    }
}
