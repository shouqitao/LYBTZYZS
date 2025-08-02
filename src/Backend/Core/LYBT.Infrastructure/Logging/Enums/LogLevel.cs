using System.ComponentModel;

namespace LYBT.Infrastructure.Logging.Enums {

    /// <summary>
    /// 日志级别枚举
    /// </summary>
    [Description("日志级别")]
    public enum LogLevel {

        [Description("调试")]
        Debug = 0,

        [Description("信息")]
        Information = 1,

        [Description("普通信息")]
        Info = 2,

        [Description("警告")]
        Warning = 3,

        [Description("错误")]
        Error = 4,

        [Description("致命错误")]
        Critical = 5,

        [Description("致命错误")]
        Fatal = 6
    }
}