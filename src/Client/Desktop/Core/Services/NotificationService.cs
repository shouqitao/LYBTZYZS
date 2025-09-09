using System.Windows;
using LYBT.Desktop.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services
{

    /// <summary>
    /// 通知服务实现 - UltraThink简化的消息通知管理
    /// </summary>
    public class NotificationService : INotificationService
    {

        #region 私有字段

        private readonly ILogger<NotificationService> _logger;
        private bool _isLoading;
        private string _loadingMessage = string.Empty;
        private int _currentProgress;

        #endregion 私有字段

        #region 构造函数

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("NotificationService 初始化完成");
        }

        #endregion 构造函数

        #region 事件

        /// <summary>
        /// 消息显示事件
        /// </summary>
        public event EventHandler<NotificationEventArgs>? NotificationShown;

        /// <summary>
        /// 加载状态变化事件
        /// </summary>
        public event EventHandler<LoadingStateChangedEventArgs>? LoadingStateChanged;

        #endregion 事件

        #region 消息显示方法

        /// <summary>
        /// 显示信息消息
        /// </summary>
        public void ShowInfo(string message, string? title = null)
        {
            ShowMessage(message, NotificationType.Info, title ?? "信息");
        }

        /// <summary>
        /// 显示成功消息
        /// </summary>
        public void ShowSuccess(string message, string? title = null)
        {
            ShowMessage(message, NotificationType.Success, title ?? "成功");
        }

        /// <summary>
        /// 显示警告消息
        /// </summary>
        public void ShowWarning(string message, string? title = null)
        {
            ShowMessage(message, NotificationType.Warning, title ?? "警告");
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        public void ShowError(string message, string? title = null)
        {
            ShowMessage(message, NotificationType.Error, title ?? "错误");
        }

        /// <summary>
        /// 显示自定义消息
        /// </summary>
        public void ShowMessage(string message, NotificationType type, string? title = null,
            bool autoClose = true, int duration = 3000)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning("尝试显示空消息");
                    return;
                }

                // 触发消息事件
                var args = new NotificationEventArgs
                {
                    Message = message,
                    Title = title,
                    Type = type,
                    AutoClose = autoClose,
                    Duration = duration
                };

                NotificationShown?.Invoke(this, args);

                // 在UI线程上显示MessageBox（简单实现）
                if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var messageBoxImage = GetMessageBoxImage(type);
                        MessageBox.Show(message, title ?? GetDefaultTitle(type), MessageBoxButton.OK, messageBoxImage);
                    });
                }

                _logger.LogInformation($"显示{type}消息: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"显示消息时发生异常: {message}");
            }
        }

        #endregion 消息显示方法

        #region 对话框方法

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public async Task<bool> ShowConfirmAsync(string message, string title = "确认")
        {
            return await ShowChoiceAsync(message, title, "确认", "取消");
        }

        /// <summary>
        /// 显示选择对话框
        /// </summary>
        public async Task<bool> ShowChoiceAsync(string message, string title = "选择",
            string yesText = "是", string noText = "否")
        {
            try
            {
                if (Application.Current?.Dispatcher == null)
                {
                    _logger.LogWarning("无法获取UI线程，返回默认值false");
                    return false;
                }

                var result = await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var messageBoxResult = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    return messageBoxResult == MessageBoxResult.Yes;
                });

                _logger.LogInformation($"用户选择对话框结果: {result} - {message}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"显示选择对话框时发生异常: {message}");
                return false;
            }
        }

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        public async Task<string?> ShowInputAsync(string message, string title = "输入", string defaultValue = "")
        {
            try
            {
                // 这里简化处理，实际项目中可以创建自定义的输入对话框
                // 目前使用MessageBox作为占位符
                if (Application.Current?.Dispatcher == null)
                {
                    _logger.LogWarning("无法获取UI线程，返回默认值");
                    return defaultValue;
                }

                var result = await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // TODO: 实现自定义输入对话框
                    // 这里临时使用简单的MessageBox确认是否使用默认值
                    var messageBoxResult = MessageBox.Show(
                        $"{message}\n\n使用默认值 '{defaultValue}' ?",
                        title,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    return messageBoxResult == MessageBoxResult.Yes ? defaultValue : null;
                });

                _logger.LogInformation($"用户输入对话框结果: {result ?? "取消"} - {message}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"显示输入对话框时发生异常: {message}");
                return null;
            }
        }

        #endregion 对话框方法

        #region 加载状态

        /// <summary>
        /// 显示加载状态
        /// </summary>
        public void ShowLoading(string message = "正在加载...")
        {
            try
            {
                if (_isLoading)
                {
                    _logger.LogDebug("加载状态已经显示，更新消息");
                }

                _isLoading = true;
                _loadingMessage = message;

                // 触发加载状态变化事件
                LoadingStateChanged?.Invoke(this, new LoadingStateChangedEventArgs
                {
                    IsLoading = true,
                    Message = message
                });

                _logger.LogInformation($"显示加载状态: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"显示加载状态时发生异常: {message}");
            }
        }

        /// <summary>
        /// 隐藏加载状态
        /// </summary>
        public void HideLoading()
        {
            try
            {
                if (!_isLoading)
                {
                    _logger.LogDebug("加载状态已经隐藏");
                    return;
                }

                _isLoading = false;
                _loadingMessage = string.Empty;
                _currentProgress = 0;

                // 触发加载状态变化事件
                LoadingStateChanged?.Invoke(this, new LoadingStateChangedEventArgs
                {
                    IsLoading = false,
                    Message = string.Empty
                });

                _logger.LogInformation("隐藏加载状态");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "隐藏加载状态时发生异常");
            }
        }

        /// <summary>
        /// 显示进度条
        /// </summary>
        public void ShowProgress(string message, int progress)
        {
            try
            {
                progress = Math.Max(0, Math.Min(100, progress)); // 确保进度在0-100范围内

                _isLoading = true;
                _loadingMessage = message;
                _currentProgress = progress;

                // 触发加载状态变化事件
                LoadingStateChanged?.Invoke(this, new LoadingStateChangedEventArgs
                {
                    IsLoading = true,
                    Message = message,
                    Progress = progress
                });

                _logger.LogDebug($"更新进度: {progress}% - {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"显示进度时发生异常: {message}");
            }
        }

        /// <summary>
        /// 隐藏进度条
        /// </summary>
        public void HideProgress()
        {
            HideLoading();
        }

        #endregion 加载状态

        #region 私有辅助方法

        /// <summary>
        /// 获取MessageBox图标
        /// </summary>
        private MessageBoxImage GetMessageBoxImage(NotificationType type)
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

        /// <summary>
        /// 获取默认标题
        /// </summary>
        private string GetDefaultTitle(NotificationType type)
        {
            return type switch
            {
                NotificationType.Info => "信息",
                NotificationType.Success => "成功",
                NotificationType.Warning => "警告",
                NotificationType.Error => "错误",
                _ => "通知"
            };
        }

        #endregion 私有辅助方法
    }
}
