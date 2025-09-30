using System;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Services.Session
{
    /// <summary>
    /// 会话管理服务接口 - 简化版本
    /// 遵循"适度设计、拒绝过度工程"原则，提供基本的用户会话管理
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// 当前登录用户
        /// </summary>
        UserDto? CurrentUser { get; }

        /// <summary>
        /// 当前用户ID
        /// </summary>
        int? CurrentUserId { get; }

        /// <summary>
        /// 当前用户名
        /// </summary>
        string? CurrentUserName { get; }

        /// <summary>
        /// 是否已登录
        /// </summary>
        bool IsLoggedIn { get; }

        /// <summary>
        /// 访问令牌
        /// </summary>
        string? AccessToken { get; }

        /// <summary>
        /// 刷新令牌
        /// </summary>
        string? RefreshToken { get; }

        /// <summary>
        /// 设置会话信息
        /// </summary>
        void SetSession(UserDto user, string accessToken, string? refreshToken = null);

        /// <summary>
        /// 更新访问令牌
        /// </summary>
        void UpdateAccessToken(string accessToken);

        /// <summary>
        /// 清除会话
        /// </summary>
        void ClearSession();

        /// <summary>
        /// 设置用户会话（别名方法，为兼容性保留）
        /// </summary>
        void SetUserSession(UserDto user, string token);

        /// <summary>
        /// 清除用户会话（别名方法，为兼容性保留）
        /// </summary>
        void ClearUserSession();

        /// <summary>
        /// 检查权限
        /// </summary>
        bool HasPermission(string permission);

        /// <summary>
        /// 检查角色
        /// </summary>
        bool HasRole(string role);

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
        public SessionChangedEventArgs(bool isLoggedIn, UserDto? user = null)
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
        public UserDto? User { get; }
    }
}