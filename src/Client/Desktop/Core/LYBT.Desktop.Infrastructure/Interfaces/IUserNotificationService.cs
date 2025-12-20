namespace LYBT.Desktop.Infrastructure.Interfaces
{
    /// <summary>
    /// 用户通知服务接口 - UltraThink架构
    /// 提供简单的消息提示和用户交互对话框功能
    /// Issue #840: 从 IErrorHandlingService 重命名以消除与 Services.ErrorHandling.IErrorHandlingService 的命名冲突
    /// optimize-desktop-core: 移除RegisterGlobalExceptionHandlers，由IDesktopExceptionHandler统一处理
    /// </summary>
    public interface IUserNotificationService
    {
        /// <summary>
        /// 处理异常并显示给用户
        /// </summary>
        Task HandleExceptionAsync(Exception exception, string? context = null);

        /// <summary>
        /// 显示错误消息
        /// </summary>
        Task ShowErrorAsync(string message, string? title = null);

        /// <summary>
        /// 显示成功消息
        /// </summary>
        Task ShowSuccessAsync(string message, string? title = null);

        /// <summary>
        /// 显示警告消息
        /// </summary>
        Task ShowWarningAsync(string message, string? title = null);

        /// <summary>
        /// 显示信息消息
        /// </summary>
        Task ShowInfoAsync(string message, string? title = null);

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        Task<bool> ShowConfirmAsync(string message, string? title = null);
    }
}
