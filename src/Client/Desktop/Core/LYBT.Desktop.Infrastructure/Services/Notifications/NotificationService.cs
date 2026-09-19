using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services.Notifications
{
    /// <summary>
    /// 通知服务实现 - 简化版本
    /// 遵循"适度设计、拒绝过度工程"原则。
    /// N6：消息走 <see cref="IToastService"/>（ADR-0003），确认走 <see cref="IDialogManager"/>；禁止 MessageBox。
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly IDialogManager _dialogManager;
        private readonly IToastService _toastService;

        public NotificationService(
            ILogger<NotificationService> logger,
            IDialogManager dialogManager,
            IToastService toastService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dialogManager = dialogManager ?? throw new ArgumentNullException(nameof(dialogManager));
            _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        }

        /// <summary>
        /// 消息显示事件
        /// </summary>
        public event EventHandler<NotificationEventArgs>? NotificationShown;

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
                var result = await _dialogManager.ShowConfirmAsync(message, title);
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
        /// 显示通知的核心方法 — 事件 + Toast（替代 MessageBox）
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

                // ADR-0003：轻量 Toast 替代 MessageBox
                switch (type)
                {
                    case NotificationType.Info:
                        _toastService.ShowInfo(message);
                        break;
                    case NotificationType.Success:
                        _toastService.ShowSuccess(message);
                        break;
                    case NotificationType.Warning:
                        _toastService.ShowWarning(message);
                        break;
                    case NotificationType.Error:
                        _toastService.ShowError(message);
                        break;
                    default:
                        _toastService.ShowInfo(message);
                        break;
                }

                _logger.LogInformation("显示{Type}消息: {Message}", type, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示通知时发生异常: {Message}", message);
            }
        }
    }
}
