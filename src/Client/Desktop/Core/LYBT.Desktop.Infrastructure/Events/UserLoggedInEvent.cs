using LYBT.Shared.Models.Contracts.Users;
using Prism.Events;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 用户登录事件
    /// </summary>
    public class UserLoggedInEvent : PubSubEvent<UserLoggedInEventArgs>
    {
    }

    /// <summary>
    /// 用户登录事件参数
    /// </summary>
    public class UserLoggedInEventArgs
    {
        public UserLoggedInEventArgs(UserDetailDto user, string token)
        {
            User = user;
            Token = token;
        }

        /// <summary>
        /// 用户信息
        /// </summary>
        public UserDetailDto User { get; }

        /// <summary>
        /// 访问令牌
        /// </summary>
        public string Token { get; }

        /// <summary>
        /// 用户名（兼容属性）
        /// </summary>
        public string Username => User?.UserName ?? User?.RealName ?? string.Empty;
    }

    /// <summary>
    /// 用户登出事件
    /// </summary>
    public class UserLoggedOutEvent : PubSubEvent
    {
    }
}
