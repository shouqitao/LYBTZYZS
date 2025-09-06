using System.ComponentModel;

namespace LYBT.Shared.Models.Enums {
    // UltraThink重构：删除冗余BusinessLogLevel，直接使用Microsoft.Extensions.Logging.LogLevel

    /// <summary>
    /// LogLevel扩展方法
    /// </summary>
    public static class LogLevelExtensions {

        /// <summary>
        /// 转换为中文描述
        /// </summary>
        public static string ToChineseDescription(this Microsoft.Extensions.Logging.LogLevel logLevel) {
            return logLevel switch {
                Microsoft.Extensions.Logging.LogLevel.Trace => "跟踪",
                Microsoft.Extensions.Logging.LogLevel.Debug => "调试",
                Microsoft.Extensions.Logging.LogLevel.Information => "信息",
                Microsoft.Extensions.Logging.LogLevel.Warning => "警告",
                Microsoft.Extensions.Logging.LogLevel.Error => "错误",
                Microsoft.Extensions.Logging.LogLevel.Critical => "严重错误",
                Microsoft.Extensions.Logging.LogLevel.None => "无",
                _ => logLevel.ToString()
            };
        }

        /// <summary>
        /// 判断是否为错误级别
        /// </summary>
        public static bool IsError(this Microsoft.Extensions.Logging.LogLevel logLevel) {
            return logLevel >= Microsoft.Extensions.Logging.LogLevel.Error;
        }

        /// <summary>
        /// 判断是否为警告及以上级别
        /// </summary>
        public static bool IsWarningOrAbove(this Microsoft.Extensions.Logging.LogLevel logLevel) {
            return logLevel >= Microsoft.Extensions.Logging.LogLevel.Warning;
        }
    }

    // UltraThink重构：保留业务相关的枚举，删除过度复杂的日志分类

    /// <summary>
    /// 操作类型枚举 - 简化版
    /// </summary>
    public enum ActionType {
        [Description("查看")] View = 0,
        [Description("创建")] Create = 1,
        [Description("更新")] Update = 2,
        [Description("删除")] Delete = 3,
        [Description("登录")] Login = 4,
        [Description("登出")] Logout = 5
    }
}
