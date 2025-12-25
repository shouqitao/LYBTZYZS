namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 对话框管理服务接口
    /// OpenSpec: refactor-viewmodel-composition
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
        /// 显示信息消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        Task ShowInfoAsync(string message, string? title = null);

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <returns>用户是否确认</returns>
        Task<bool> ShowConfirmAsync(string message, string? title = null);

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        /// <param name="message">提示消息</param>
        /// <param name="title">标题</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>用户输入的值，取消返回null</returns>
        Task<string?> ShowInputAsync(string message, string? title = null, string? defaultValue = null);

        /// <summary>
        /// 显示自定义对话框
        /// </summary>
        /// <typeparam name="TResult">返回结果类型</typeparam>
        /// <param name="dialogName">对话框名称</param>
        /// <param name="parameters">参数</param>
        /// <returns>对话框结果</returns>
        Task<TResult?> ShowDialogAsync<TResult>(string dialogName, IDictionary<string, object>? parameters = null);
    }
}
