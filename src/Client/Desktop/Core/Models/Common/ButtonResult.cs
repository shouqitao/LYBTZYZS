namespace LYBT.Desktop.Core.Models.Common
{
    /// <summary>
    /// 对话框按钮结果
    /// 替代 Prism ButtonResult，兼容 Prism 8.1.97
    /// </summary>
    public enum ButtonResult
    {
        /// <summary>
        /// 无结果
        /// </summary>
        None = 0,

        /// <summary>
        /// 确定/OK按钮
        /// </summary>
        OK = 1,

        /// <summary>
        /// 取消按钮
        /// </summary>
        Cancel = 2,

        /// <summary>
        /// 是按钮
        /// </summary>
        Yes = 3,

        /// <summary>
        /// 否按钮
        /// </summary>
        No = 4,

        /// <summary>
        /// 重试按钮
        /// </summary>
        Retry = 5,

        /// <summary>
        /// 忽略按钮
        /// </summary>
        Ignore = 6,

        /// <summary>
        /// 中止按钮
        /// </summary>
        Abort = 7,

        /// <summary>
        /// 应用按钮
        /// </summary>
        Apply = 8,

        /// <summary>
        /// 关闭按钮
        /// </summary>
        Close = 9
    }
}
