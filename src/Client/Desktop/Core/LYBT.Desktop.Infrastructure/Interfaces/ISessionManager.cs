using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Interfaces
{
    /// <summary>
    /// 会话管理器接口 - UltraThink架构
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// 当前用户
        /// </summary>
        UserDto? CurrentUser { get; }

        /// <summary>
        /// 是否已认证
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// 当前Token
        /// </summary>
        string? CurrentToken { get; }

        /// <summary>
        /// 设置当前用户
        /// </summary>
        void SetCurrentUser(UserDto user, string token);

        /// <summary>
        /// 清除会话
        /// </summary>
        void ClearSession();

        /// <summary>
        /// 检查权限
        /// </summary>
        bool HasPermission(UserRole requiredRole);

        /// <summary>
        /// 是否为管理员
        /// </summary>
        bool IsAdmin();

        /// <summary>
        /// 获取当前用户角色显示名称
        /// </summary>
        string GetCurrentUserRoleDisplay();

        /// <summary>
        /// 会话即将过期事件
        /// </summary>
        event EventHandler? SessionExpiring;

        /// <summary>
        /// 会话已过期事件
        /// </summary>
        event EventHandler? SessionExpired;
    }
}
