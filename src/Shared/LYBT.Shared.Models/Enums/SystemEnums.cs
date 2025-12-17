using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 通用状态枚举
    /// </summary>
    public enum CommonStatus
    {
        /// <summary>禁用</summary>
        [Description("禁用")]
        Disabled = 0,

        /// <summary>启用</summary>
        [Description("启用")]
        Enabled = 1
    }

    /// <summary>
    /// 操作结果枚举
    /// </summary>
    public enum OperationResult
    {
        /// <summary>失败</summary>
        [Description("失败")]
        Failed = 0,

        /// <summary>成功</summary>
        [Description("成功")]
        Success = 1,

        /// <summary>错误</summary>
        [Description("错误")]
        Error = 2,

        /// <summary>警告</summary>
        [Description("警告")]
        Warning = 3,

        /// <summary>权限不足</summary>
        [Description("权限不足")]
        Forbidden = 4,

        /// <summary>未授权</summary>
        [Description("未授权")]
        Unauthorized = 5,

        /// <summary>已取消</summary>
        [Description("已取消")]
        Cancelled = 6,

        /// <summary>超时</summary>
        [Description("超时")]
        Timeout = 7
    }
}
