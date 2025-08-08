namespace LYBT.WPF.Client.Core.Models.Common
{
    /// <summary>
    /// 错误严重程度
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// 信息提示，不影响操作
        /// </summary>
        Info = 0,

        /// <summary>
        /// 警告，操作可能受影响
        /// </summary>
        Warning = 1,

        /// <summary>
        /// 错误，操作失败但可恢复
        /// </summary>
        Error = 2,

        /// <summary>
        /// 严重错误，可能影响系统稳定性
        /// </summary>
        Critical = 3,

        /// <summary>
        /// 致命错误，需要立即处理
        /// </summary>
        Fatal = 4
    }
}