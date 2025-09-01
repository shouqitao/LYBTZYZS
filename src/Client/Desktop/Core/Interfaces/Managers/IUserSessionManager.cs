using System;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Interfaces.Managers
{
    /// <summary>
    /// 用户会话管理器接口 - UltraThink架构简化
    /// 专门负责用户会话状态、凭据管理和用户偏好
    /// </summary>
    public interface IUserSessionManager
    {
        #region 会话状态

        /// <summary>
        /// 当前登录用户
        /// </summary>
        UserDto? CurrentUser { get; }

        /// <summary>
        /// 用户是否已认证
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// 会话开始时间
        /// </summary>
        DateTime? SessionStartTime { get; }

        /// <summary>
        /// 上次活动时间
        /// </summary>
        DateTime? LastActivityTime { get; }

        #endregion

        #region 会话管理

        /// <summary>
        /// 开始用户会话
        /// </summary>
        Task<ServiceResult> StartSessionAsync(UserDto user, LoginResponse loginResponse);

        /// <summary>
        /// 结束用户会话
        /// </summary>
        Task<ServiceResult> EndSessionAsync();

        /// <summary>
        /// 更新最后活动时间
        /// </summary>
        void UpdateLastActivity();

        /// <summary>
        /// 检查会话是否过期
        /// </summary>
        bool IsSessionExpired();

        /// <summary>
        /// 获取会话剩余时间（分钟）
        /// </summary>
        int GetSessionRemainingMinutes();

        #endregion

        #region 凭据管理

        /// <summary>
        /// 保存用户凭据（记住密码功能）
        /// </summary>
        ServiceResult SaveCredentials(string username, string password, bool rememberMe);

        /// <summary>
        /// 加载已保存的凭据
        /// </summary>
        ServiceResult<SavedCredentials?> LoadSavedCredentials();

        /// <summary>
        /// 清除保存的凭据
        /// </summary>
        ServiceResult ClearSavedCredentials();

        /// <summary>
        /// 检查是否有保存的凭据
        /// </summary>
        bool HasSavedCredentials();

        #endregion

        #region 用户偏好设置

        /// <summary>
        /// 保存用户偏好设置
        /// </summary>
        ServiceResult SaveUserPreference(string key, object value);

        /// <summary>
        /// 获取用户偏好设置
        /// </summary>
        ServiceResult<T?> GetUserPreference<T>(string key);

        /// <summary>
        /// 移除用户偏好设置
        /// </summary>
        ServiceResult RemoveUserPreference(string key);

        /// <summary>
        /// 清除所有用户偏好设置
        /// </summary>
        ServiceResult ClearUserPreferences();

        #endregion

        #region 事件通知

        /// <summary>
        /// 用户会话状态变更事件
        /// </summary>
        event EventHandler<UserSessionStateChangedEventArgs>? SessionStateChanged;

        /// <summary>
        /// 用户信息更新事件
        /// </summary>
        event EventHandler<UserInfoUpdatedEventArgs>? UserInfoUpdated;

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 保存的凭据信息
    /// </summary>
    public class SavedCredentials
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
        public DateTime SavedAt { get; set; }
    }

    /// <summary>
    /// 用户会话状态变更事件参数
    /// </summary>
    public class UserSessionStateChangedEventArgs : EventArgs
    {
        public bool IsStarted { get; set; }
        public UserDto? User { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// 用户信息更新事件参数
    /// </summary>
    public class UserInfoUpdatedEventArgs : EventArgs
    {
        public UserDto? OldUser { get; set; }
        public UserDto? NewUser { get; set; }
        public string UpdateReason { get; set; } = string.Empty;
    }

    #endregion
}