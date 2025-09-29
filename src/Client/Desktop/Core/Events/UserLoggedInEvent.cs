using Prism.Events;
using LYBT.Shared.Models.Enums;
using System;

namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// 用户登录成功事件
    /// </summary>
    public class UserLoggedInEvent : PubSubEvent<UserLoggedInEventArgs>
    {
    }

    /// <summary>
    /// 用户登录事件参数
    /// </summary>
    public class UserLoggedInEventArgs
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 用户角色
        /// </summary>
        public UserRole Role { get; set; }

        /// <summary>
        /// 登录时间
        /// </summary>
        public DateTime LoginTime { get; set; }

        /// <summary>
        /// JWT Token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; set; }
    }
}