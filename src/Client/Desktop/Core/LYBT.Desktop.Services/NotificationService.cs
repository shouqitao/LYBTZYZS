using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 通知服务实现
    /// UltraThink架构优化 - Phase 3
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 显示通知消息
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息内容</param>
        /// <param name="type">通知类型</param>
        public async Task ShowNotificationAsync(string title, string message, NotificationType type = NotificationType.Information)
        {
            try
            {
                _logger.LogDebug("显示通知: {Title} - {Message} ({Type})", title, message, type);

                // TODO: 实现系统通知
                await Task.Run(() =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var messageBoxIcon = type switch
                        {
                            NotificationType.Success => System.Windows.MessageBoxImage.Information,
                            NotificationType.Warning => System.Windows.MessageBoxImage.Warning,
                            NotificationType.Error => System.Windows.MessageBoxImage.Error,
                            _ => System.Windows.MessageBoxImage.Information
                        };

                        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, messageBoxIcon);
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示通知失败: {Title}", title);
            }
        }

        /// <summary>
        /// 显示成功通知
        /// </summary>
        /// <param name="message">消息内容</param>
        public async Task ShowSuccessAsync(string message)
        {
            await ShowNotificationAsync("成功", message, NotificationType.Success);
        }

        /// <summary>
        /// 显示警告通知
        /// </summary>
        /// <param name="message">消息内容</param>
        public async Task ShowWarningAsync(string message)
        {
            await ShowNotificationAsync("警告", message, NotificationType.Warning);
        }

        /// <summary>
        /// 显示错误通知
        /// </summary>
        /// <param name="message">消息内容</param>
        public async Task ShowErrorAsync(string message)
        {
            await ShowNotificationAsync("错误", message, NotificationType.Error);
        }
    }

    /// <summary>
    /// 通知类型枚举
    /// </summary>
    public enum NotificationType
    {
        Information,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// 通知服务接口
    /// </summary>
    public interface INotificationService
    {
        Task ShowNotificationAsync(string title, string message, NotificationType type = NotificationType.Information);
        Task ShowSuccessAsync(string message);
        Task ShowWarningAsync(string message);
        Task ShowErrorAsync(string message);
    }
}
