namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 对话框管理服务接口
    ///
    /// 提供统一的对话框显示接口，集成Prism IDialogService
    /// </summary>
    public interface IDialogManager
    {
        /// <summary>
        /// 显示成功消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        Task ShowSuccessAsync(string message, string? title = null);

        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        Task ShowErrorAsync(string message, string? title = null);

        /// <summary>
        /// 显示警告消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        Task ShowWarningAsync(string message, string? title = null);

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <returns>用户是否确认</returns>
        Task<bool> ShowConfirmAsync(string message, string? title = null);
    }
}
