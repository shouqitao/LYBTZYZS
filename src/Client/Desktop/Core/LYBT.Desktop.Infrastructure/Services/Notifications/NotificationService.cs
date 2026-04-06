using System.Windows;
using LYBT.Desktop.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services.Notifications
{
    /// <summary>
    /// 通知服务实现 - 简化版本
    /// 遵循"适度设计、拒绝过度工程"原则，提供基本的消息通知功能
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly IUiThreadDispatcher _dispatcher;

        public NotificationService(ILogger<NotificationService> logger, IUiThreadDispatcher dispatcher)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        /// <summary>
        /// 消息显示事件
        /// </summary>
        public event EventHandler<NotificationEventArgs>? NotificationShown;

        /// <summary>
        /// 加载状态变化事件
        /// </summary>
        public event EventHandler<LoadingStateChangedEventArgs>? LoadingStateChanged;

        /// <summary>
        /// 显示信息消息
        /// </summary>
        public void ShowInfo(string message, string? title = null)
        {
            ShowNotification(message, NotificationType.Info, title ?? "信息");
        }

        /// <summary>
        /// 显示成功消息
        /// </summary>
        public void ShowSuccess(string message, string? title = null)
        {
            ShowNotification(message, NotificationType.Success, title ?? "成功");
        }

        /// <summary>
        /// 显示警告消息
        /// </summary>
        public void ShowWarning(string message, string? title = null)
        {
            ShowNotification(message, NotificationType.Warning, title ?? "警告");
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        public void ShowError(string message, string? title = null)
        {
            ShowNotification(message, NotificationType.Error, title ?? "错误");
        }

        /// <summary>
        /// 显示错误消息（异步）
        /// </summary>
        public async Task ShowErrorAsync(string message, string? title = null)
        {
            await Task.Run(() => ShowError(message, title));
        }

        /// <summary>
        /// 显示信息消息（异步）
        /// </summary>
        public async Task ShowInfoAsync(string message, string? title = null)
        {
            await Task.Run(() => ShowInfo(message, title));
        }

        /// <summary>
        /// 显示成功消息（异步）
        /// </summary>
        public async Task ShowSuccessAsync(string message, string? title = null)
        {
            await Task.Run(() => ShowSuccess(message, title));
        }

        /// <summary>
        /// 显示警告消息（异步）
        /// </summary>
        public async Task ShowWarningAsync(string message, string? title = null)
        {
            await Task.Run(() => ShowWarning(message, title));
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public async Task<bool> ShowConfirmAsync(string message, string title = "确认")
        {
            try
            {
                var result = await _dispatcher.InvokeAsync(() =>
                {
                    var messageBoxResult = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    return messageBoxResult == MessageBoxResult.Yes;
                });

                _logger.LogInformation("用户确认对话框结果: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示确认对话框时发生异常");
                return false;
            }
        }

        /// <summary>
        /// 显示加载状态
        /// </summary>
        public void ShowLoading(string message = "正在加载...")
        {
            try
            {
                LoadingStateChanged?.Invoke(this, new LoadingStateChangedEventArgs
                {
                    IsLoading = true,
                    Message = message
                });

                _logger.LogDebug("显示加载状态: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示加载状态时发生异常");
            }
        }

        /// <summary>
        /// 隐藏加载状态
        /// </summary>
        public void HideLoading()
        {
            try
            {
                LoadingStateChanged?.Invoke(this, new LoadingStateChangedEventArgs
                {
                    IsLoading = false,
                    Message = string.Empty
                });

                _logger.LogDebug("隐藏加载状态");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "隐藏加载状态时发生异常");
            }
        }

        /// <summary>
        /// 显示通知的核心方法
        /// </summary>
        private void ShowNotification(string message, NotificationType type, string title)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning("尝试显示空消息");
                    return;
                }

                // 触发通知事件
                NotificationShown?.Invoke(this, new NotificationEventArgs
                {
                    Message = message,
                    Title = title,
                    Type = type
                });

                // 在UI线程显示MessageBox
                _dispatcher.Invoke(() =>
                {
                    var messageBoxImage = GetMessageBoxImage(type);
                    MessageBox.Show(message, title, MessageBoxButton.OK, messageBoxImage);
                });

                _logger.LogInformation("显示{Type}消息: {Message}", type, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示通知时发生异常: {Message}", message);
            }
        }

        /// <summary>
        /// 获取MessageBox图标
        /// </summary>
        private static MessageBoxImage GetMessageBoxImage(NotificationType type)
        {
            return type switch
            {
                NotificationType.Info => MessageBoxImage.Information,
                NotificationType.Success => MessageBoxImage.Information,
                NotificationType.Warning => MessageBoxImage.Warning,
                NotificationType.Error => MessageBoxImage.Error,
                _ => MessageBoxImage.None
            };
        }
    }
}
