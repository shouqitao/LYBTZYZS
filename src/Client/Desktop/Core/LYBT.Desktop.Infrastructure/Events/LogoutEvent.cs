using Prism.Events;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 登出事件参数
    /// UltraThink架构优化 - 统一事件管理
    /// </summary>
    public class LogoutEventArgs : EventArgs
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// 登出原因
        /// </summary>
        public LogoutReason Reason { get; set; } = LogoutReason.UserInitiated;

        /// <summary>
        /// 登出时间
        /// </summary>
        public DateTime LogoutTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否强制登出
        /// </summary>
        public bool IsForced { get; set; }

        /// <summary>
        /// 附加消息
        /// </summary>
        public string? Message { get; set; }

        public LogoutEventArgs()
        {
        }

        public LogoutEventArgs(string userId, string userName, LogoutReason reason = LogoutReason.UserInitiated)
        {
            UserId = userId;
            UserName = userName;
            Reason = reason;
        }
    }

    /// <summary>
    /// 登出原因枚举
    /// </summary>
    public enum LogoutReason
    {
        /// <summary>
        /// 用户主动登出
        /// </summary>
        UserInitiated,

        /// <summary>
        /// 会话超时
        /// </summary>
        SessionTimeout,

        /// <summary>
        /// 系统强制登出
        /// </summary>
        SystemForced,

        /// <summary>
        /// 安全策略
        /// </summary>
        SecurityPolicy,

        /// <summary>
        /// 系统维护
        /// </summary>
        SystemMaintenance,

        /// <summary>
        /// 错误导致
        /// </summary>
        Error
    }

    /// <summary>
    /// 登出事件
    /// </summary>
    public class LogoutEvent : PubSubEvent<LogoutEventArgs>
    {
    }
}
