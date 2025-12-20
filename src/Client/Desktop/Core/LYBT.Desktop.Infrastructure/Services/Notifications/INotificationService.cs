namespace LYBT.Desktop.Infrastructure.Services.Notifications
{
    /// <summary>
    /// 通知服务接口 - 简化版本
    /// 遵循"适度设计、拒绝过度工程"原则
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// 显示信息消息
        /// </summary>
        void ShowInfo(string message, string? title = null);

        /// <summary>
        /// 显示成功消息
        /// </summary>
        void ShowSuccess(string message, string? title = null);

        /// <summary>
        /// 显示警告消息
        /// </summary>
        void ShowWarning(string message, string? title = null);

        /// <summary>
        /// 显示错误消息
        /// </summary>
        void ShowError(string message, string? title = null);

        /// <summary>
        /// 显示错误消息（异步）
        /// </summary>
        Task ShowErrorAsync(string message, string? title = null);

        /// <summary>
        /// 显示信息消息（异步）
        /// </summary>
        Task ShowInfoAsync(string message, string? title = null);

        /// <summary>
        /// 显示成功消息（异步）
        /// </summary>
        Task ShowSuccessAsync(string message, string? title = null);

        /// <summary>
        /// 显示警告消息（异步）
        /// </summary>
        Task ShowWarningAsync(string message, string? title = null);

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        Task<bool> ShowConfirmAsync(string message, string title = "确认");

        /// <summary>
        /// 显示加载状态
        /// </summary>
        void ShowLoading(string message = "正在加载...");

        /// <summary>
        /// 隐藏加载状态
        /// </summary>
        void HideLoading();

        /// <summary>
        /// 消息显示事件
        /// </summary>
        event EventHandler<NotificationEventArgs>? NotificationShown;

        /// <summary>
        /// 加载状态变化事件
        /// </summary>
        event EventHandler<LoadingStateChangedEventArgs>? LoadingStateChanged;
    }

    /// <summary>
    /// 通知类型
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// 通知事件参数
    /// </summary>
    public class NotificationEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
        public string? Title { get; set; }
        public NotificationType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 加载状态变化事件参数
    /// </summary>
    public class LoadingStateChangedEventArgs : EventArgs
    {
        public bool IsLoading { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
