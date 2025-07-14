using System.ComponentModel;

namespace LYBT.Common.Enums.Logs {

    /// <summary>
    /// 日志类型枚举
    /// </summary>
    [Description("日志类型")]
    public enum LogType {

        /// <summary>
        /// 操作日志（业务相关增删改查）
        /// </summary>
        [Description("操作日志")]
        Operation = 1,

        /// <summary>
        /// 系统日志（异常、系统级事件）
        /// </summary>
        [Description("系统日志")]
        System = 2,

        /// <summary>
        /// 登录日志（登录、登出等）
        /// </summary>
        [Description("登录日志")]
        Login = 3,

        /// <summary>
        /// 其他类型
        /// </summary>
        [Description("其他")]
        Other = 99
    }
}