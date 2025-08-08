using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 业务日志级别枚举（与.NET标准LogLevel对应）
    /// 建议直接使用 Microsoft.Extensions.Logging.LogLevel
    /// </summary>
    [Obsolete("建议使用 Microsoft.Extensions.Logging.LogLevel")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BusinessLogLevel
    {
        /// <summary>跟踪</summary>
        [Description("跟踪")]
        Trace = 0,

        /// <summary>调试</summary>
        [Description("调试")]
        Debug = 1,

        /// <summary>信息</summary>
        [Description("信息")]
        Information = 2,

        /// <summary>警告</summary>
        [Description("警告")]
        Warning = 3,

        /// <summary>错误</summary>
        [Description("错误")]
        Error = 4,

        /// <summary>严重错误</summary>
        [Description("严重错误")]
        Critical = 5
    }

    /// <summary>
    /// LogLevel扩展方法
    /// </summary>
    public static class LogLevelExtensions
    {
        /// <summary>
        /// 转换为中文描述
        /// </summary>
        public static string ToChineseDescription(this Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return logLevel switch
            {
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
        public static bool IsError(this Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return logLevel >= Microsoft.Extensions.Logging.LogLevel.Error;
        }

        /// <summary>
        /// 判断是否为警告及以上级别
        /// </summary>
        public static bool IsWarningOrAbove(this Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return logLevel >= Microsoft.Extensions.Logging.LogLevel.Warning;
        }
    }

    /// <summary>
    /// 日志类型枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LogType
    {
        /// <summary>系统日志</summary>
        [Description("系统日志")]
        System = 0,

        /// <summary>用户操作</summary>
        [Description("用户操作")]
        UserAction = 1,

        /// <summary>业务日志</summary>
        [Description("业务日志")]
        Business = 2,

        /// <summary>安全日志</summary>
        [Description("安全日志")]
        Security = 3,

        /// <summary>性能日志</summary>
        [Description("性能日志")]
        Performance = 4,

        /// <summary>异常日志</summary>
        [Description("异常日志")]
        Exception = 5,

        /// <summary>操作日志</summary>
        [Description("操作日志")]
        Operation = 6
    }

    /// <summary>
    /// 操作类型枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ActionType
    {
        /// <summary>查看</summary>
        [Description("查看")]
        View = 0,

        /// <summary>创建</summary>
        [Description("创建")]
        Create = 1,

        /// <summary>更新</summary>
        [Description("更新")]
        Update = 2,

        /// <summary>删除</summary>
        [Description("删除")]
        Delete = 3,

        /// <summary>导出</summary>
        [Description("导出")]
        Export = 4,

        /// <summary>导入</summary>
        [Description("导入")]
        Import = 5,

        /// <summary>登录</summary>
        [Description("登录")]
        Login = 6,

        /// <summary>登出</summary>
        [Description("登出")]
        Logout = 7,

        /// <summary>打印</summary>
        [Description("打印")]
        Print = 8,

        /// <summary>编辑</summary>
        [Description("编辑")]
        Edit = 9,

        /// <summary>禁用</summary>
        [Description("禁用")]
        Disable = 10,

        /// <summary>启用</summary>
        [Description("启用")]
        Enable = 11,

        /// <summary>其他</summary>
        [Description("其他")]
        Other = 12,

        /// <summary>重置密码</summary>
        [Description("重置密码")]
        ResetPassword = 13
    }

    /// <summary>
    /// 日志操作类型枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LogActionType
    {
        /// <summary>查看</summary>
        [Description("查看")]
        View = 0,

        /// <summary>创建</summary>
        [Description("创建")]
        Create = 1,

        /// <summary>更新</summary>
        [Description("更新")]
        Update = 2,

        /// <summary>删除</summary>
        [Description("删除")]
        Delete = 3,

        /// <summary>登录</summary>
        [Description("登录")]
        Login = 4,

        /// <summary>登出</summary>
        [Description("登出")]
        Logout = 5,

        /// <summary>备份</summary>
        [Description("备份")]
        Backup = 6,

        /// <summary>恢复</summary>
        [Description("恢复")]
        Restore = 7
    }

    /// <summary>
    /// 对象类型枚举
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ObjectType
    {
        /// <summary>用户</summary>
        [Description("用户")]
        User = 0,

        /// <summary>患者</summary>
        [Description("患者")]
        Patient = 1,

        /// <summary>医生</summary>
        [Description("医生")]
        Doctor = 2,

        /// <summary>药材</summary>
        [Description("药材")]
        Herb = 3,

        /// <summary>处方</summary>
        [Description("处方")]
        Prescription = 4,

        /// <summary>医疗案例</summary>
        [Description("医疗案例")]
        MedicalCase = 5,

        /// <summary>验方模板</summary>
        [Description("验方模板")]
        Formula = 6,

        /// <summary>看诊记录</summary>
        [Description("看诊记录")]
        Consultation = 7,

        /// <summary>系统</summary>
        [Description("系统")]
        System = 8
    }
}