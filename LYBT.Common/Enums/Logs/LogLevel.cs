using System.ComponentModel;

namespace LYBT.Common.Enums.Logs {

    /// <summary>
    /// 日志级别枚举
    /// </summary>
    [Description("日志级别")]
/// <summary>
/// 表示LogLevel。
/// </summary>
    public enum LogLevel {
        Info,       // 普通信息
        Warning,    // 警告信息
        Error,      // 错误信息
        Debug       // 调试信息
    }
}
