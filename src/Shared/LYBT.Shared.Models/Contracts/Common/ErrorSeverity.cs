namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 错误严重程度枚举
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// 信息级别 - 仅为通知，不影响正常使用
        /// </summary>
        Info = 0,

        /// <summary>
        /// 警告级别 - 可能影响功能，但不会阻止使用
        /// </summary>
        Warning = 1,

        /// <summary>
        /// 错误级别 - 影响功能正常使用，需要用户注意
        /// </summary>
        Error = 2,

        /// <summary>
        /// 严重级别 - 严重影响系统功能，需要立即处理
        /// </summary>
        Critical = 3,

        /// <summary>
        /// 致命级别 - 系统无法继续运行，需要重启或修复
        /// </summary>
        Fatal = 4
    }
}
