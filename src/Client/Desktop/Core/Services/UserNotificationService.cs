using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LYBT.Desktop.Core.Views.Dialogs;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.Logging;
using SharedCommon = LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// 用户通知服务 - 显示各种通知消息
    /// </summary>
    public class UserNotificationService : IUserNotificationService, IDisposable
    {
        private readonly ILogger<UserNotificationService> _loggingService;
        private readonly ILogger<UserNotificationService>? _logger;
        private readonly ConcurrentQueue<NotificationMessage> _messageQueue = new();
        private readonly DispatcherTimer _displayTimer;

        // 通知容器（将在主窗口中创建）
        private Panel? _notificationContainer;
        private Window? _mainWindow;

        // 配置
        private const int MaxSimultaneousNotifications = 3;
        private const int DefaultDisplayDurationSeconds = 5;
        private const int ErrorDisplayDurationSeconds = 8;
        private const int SuccessDisplayDurationSeconds = 3;

        public UserNotificationService(
            ILogger<UserNotificationService> loggingService,
            ILogger<UserNotificationService>? logger = null)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _logger = logger;

            // 初始化显示计时器
            _displayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _displayTimer.Tick += ProcessNotificationQueue;
        }

        /// <summary>
        /// 初始化通知服务
        /// </summary>
        public void Initialize(Window mainWindow)
        {
            _mainWindow = mainWindow;
            CreateNotificationContainer();
            _displayTimer.Start();

            _loggingService.LogInformation("用户通知服务已初始化");
        }

        #region 公共方法

        public async Task ShowSuccessAsync(string message, int? durationSeconds = null)
        {
            await ShowNotificationAsync(new NotificationMessage
            {
                Type = NotificationType.Success,
                Title = "成功",
                Message = message,
                Severity = SharedCommon.ErrorSeverity.Info,
                Duration = TimeSpan.FromSeconds(durationSeconds ?? SuccessDisplayDurationSeconds),
                Icon = "✅"
            });
        }

        public async Task ShowWarningAsync(string message, int? durationSeconds = null)
        {
            await ShowNotificationAsync(new NotificationMessage
            {
                Type = NotificationType.Warning,
                Title = "警告",
                Message = message,
                Severity = SharedCommon.ErrorSeverity.Warning,
                Duration = TimeSpan.FromSeconds(durationSeconds ?? DefaultDisplayDurationSeconds),
                Icon = "⚠️"
            });
        }

        public async Task ShowErrorAsync(string message, SharedCommon.ErrorSeverity severity, int? durationSeconds = null)
        {
            await ShowNotificationAsync(new NotificationMessage
            {
                Type = NotificationType.Error,
                Title = severity >= SharedCommon.ErrorSeverity.Critical ? "严重错误" : "错误",
                Message = message,
                Severity = severity,
                Duration = TimeSpan.FromSeconds(durationSeconds ?? ErrorDisplayDurationSeconds),
                Icon = severity >= SharedCommon.ErrorSeverity.Critical ? "❌" : "⛔"
            });
        }

        public async Task ShowInfoAsync(string message, int? durationSeconds = null)
        {
            await ShowNotificationAsync(new NotificationMessage
            {
                Type = NotificationType.Info,
                Title = "提示",
                Message = message,
                Severity = SharedCommon.ErrorSeverity.Info,
                Duration = TimeSpan.FromSeconds(durationSeconds ?? DefaultDisplayDurationSeconds),
                Icon = "ℹ️"
            });
        }

        public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var result = MessageBox.Show(
                    _mainWindow ?? Application.Current.MainWindow,
                    message,
                    title,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                return result == MessageBoxResult.Yes;
            });
        }

        public async Task<string?> ShowInputAsync(string prompt, string title = "输入", string defaultValue = "")
        {
            // 创建简单的输入对话框
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var inputDialog = new InputDialog
                {
                    Owner = _mainWindow ?? Application.Current.MainWindow,
                    Title = title,
                    Prompt = prompt,
                    InputText = defaultValue
                };

                var result = inputDialog.ShowDialog();
                return result == true ? inputDialog.InputText : null;
            });
        }

        /// <summary>
        /// 增强的信息通知（支持配置）
        /// </summary>
        public async Task ShowInfoAsync(string message, NotificationConfiguration configuration)
        {
            await ShowNotificationAsync(new NotificationMessage
            {
                Type = NotificationType.Info,
                Title = "提示",
                Message = message,
                Severity = SharedCommon.ErrorSeverity.Info,
                Duration = configuration.Duration,
                Icon = "ℹ️"
            });
        }

        /// <summary>
        /// 增强的警告通知（支持配置）
        /// </summary>
        public async Task ShowWarningAsync(string message, NotificationConfiguration configuration)
        {
            await ShowNotificationAsync(new NotificationMessage
            {
                Type = NotificationType.Warning,
                Title = "警告",
                Message = message,
                Severity = SharedCommon.ErrorSeverity.Warning,
                Duration = configuration.Duration,
                Icon = "⚠️"
            });
        }

        /// <summary>
        /// 增强的错误通知（支持建议操作和配置）
        /// </summary>
        public async Task ShowErrorAsync(string message, string[] suggestedActions, NotificationConfiguration configuration)
        {
            var fullMessage = message;
            if (suggestedActions.Length > 0)
            {
                fullMessage += "\n\n建议操作：\n• " + string.Join("\n• ", suggestedActions);
            }

            await ShowNotificationAsync(new NotificationMessage
            {
                Type = NotificationType.Error,
                Title = "错误",
                Message = fullMessage,
                Severity = SharedCommon.ErrorSeverity.Error,
                Duration = configuration.Duration,
                Icon = "⛔"
            });
        }

        /// <summary>
        /// 严重错误通知（支持完整错误信息和配置）
        /// </summary>
        public async Task ShowCriticalErrorAsync(SharedCommon.HandledError handledError, NotificationConfiguration configuration)
        {
            if (configuration.ShowInDialog)
            {
                // 显示详细的错误对话框
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var errorDialog = new CriticalErrorDialog
                    {
                        Owner = _mainWindow ?? Application.Current.MainWindow,
                        ErrorInfo = handledError
                    };
                    errorDialog.ShowDialog();
                });
            }
            else
            {
                // 显示通知
                var message = handledError.UserMessage;
                if (handledError.SuggestedActions.Count > 0)
                {
                    message += "\n\n建议操作：\n• " + string.Join("\n• ", handledError.SuggestedActions);
                }

                await ShowNotificationAsync(new NotificationMessage
                {
                    Type = NotificationType.Error,
                    Title = "严重错误",
                    Message = message,
                    Severity = handledError.Severity,
                    Duration = configuration.Duration,
                    Icon = "❌"
                });
            }
        }

        public void ShowProgress(string message, int percentage)
        {
            // TODO: 实现进度显示
            _logger?.LogDebug("显示进度: {Message} - {Percentage}%", message, percentage);
        }

        public void HideProgress()
        {
            // TODO: 隐藏进度显示
            _logger?.LogDebug("隐藏进度");
        }

        #endregion

        #region 私有方法

        private async Task ShowNotificationAsync(NotificationMessage notification)
        {
            // 添加到队列
            _messageQueue.Enqueue(notification);

            // 记录日志
            _loggingService.LogInformation("添加通知到队列: {Type} - {Message}",
                notification.Type, notification.Message);

            await Task.CompletedTask;
        }

        private void ProcessNotificationQueue(object? sender, EventArgs e)
        {
            if (_notificationContainer == null || !_messageQueue.TryDequeue(out var notification))
            {
                return;
            }

            // 检查当前显示的通知数量
            if (_notificationContainer.Children.Count >= MaxSimultaneousNotifications)
            {
                return;
            }

            // 在UI线程上创建和显示通知
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                DisplayNotification(notification);
            }));
        }

        private void DisplayNotification(NotificationMessage notification)
        {
            try
            {
                // 创建通知UI
                var notificationPanel = CreateNotificationPanel(notification);

                // 添加到容器
                _notificationContainer?.Children.Add(notificationPanel);

                // 应用进入动画
                ApplyEnterAnimation(notificationPanel);

                // 设置自动关闭
                var timer = new DispatcherTimer
                {
                    Interval = notification.Duration
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    RemoveNotification(notificationPanel);
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "显示通知失败");
            }
        }

        private Border CreateNotificationPanel(NotificationMessage notification)
        {
            var panel = new Border
            {
                Background = GetBackgroundBrush(notification.Type),
                BorderBrush = GetBorderBrush(notification.Type),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(5),
                Padding = new Thickness(10),
                MinHeight = 60,
                MaxWidth = 400,
                Opacity = 0 // 初始透明，用于动画
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 图标
            var icon = new TextBlock
            {
                Text = notification.Icon,
                FontSize = 24,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            // 内容
            var contentPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            if (!string.IsNullOrEmpty(notification.Title))
            {
                contentPanel.Children.Add(new TextBlock
                {
                    Text = notification.Title,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = Brushes.White
                });
            }

            contentPanel.Children.Add(new TextBlock
            {
                Text = notification.Message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 2, 0, 0)
            });

            Grid.SetColumn(contentPanel, 1);
            grid.Children.Add(contentPanel);

            // 关闭按钮
            var closeButton = new Button
            {
                Content = "✕",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                FontSize = 16,
                Cursor = System.Windows.Input.Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Top,
                Padding = new Thickness(5)
            };
            closeButton.Click += (s, e) => RemoveNotification(panel);
            Grid.SetColumn(closeButton, 2);
            grid.Children.Add(closeButton);

            panel.Child = grid;
            return panel;
        }

        private void ApplyEnterAnimation(FrameworkElement element)
        {
            var slideIn = new ThicknessAnimation
            {
                From = new Thickness(400, 0, -400, 0),
                To = new Thickness(0),
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            element.BeginAnimation(FrameworkElement.MarginProperty, slideIn);
            element.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private void RemoveNotification(FrameworkElement element)
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            fadeOut.Completed += (s, e) =>
            {
                _notificationContainer?.Children.Remove(element);
            };

            element.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void CreateNotificationContainer()
        {
            if (_mainWindow == null)
            {
                return;
            }

            // 查找或创建通知容器
            if (_mainWindow.Content is Grid mainGrid)
            {
                // 创建通知层
                _notificationContainer = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 50, 10, 0),
                    Width = 420
                };

                // 设置高Z-Index确保显示在最上层
                Panel.SetZIndex(_notificationContainer, 9999);

                // 添加到主网格
                mainGrid.Children.Add(_notificationContainer);
            }
        }

        private Brush GetBackgroundBrush(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                NotificationType.Warning => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                NotificationType.Error => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                NotificationType.Info => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                _ => new SolidColorBrush(Color.FromRgb(96, 125, 139))
            };
        }

        private Brush GetBorderBrush(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => new SolidColorBrush(Color.FromRgb(56, 142, 60)),
                NotificationType.Warning => new SolidColorBrush(Color.FromRgb(245, 124, 0)),
                NotificationType.Error => new SolidColorBrush(Color.FromRgb(211, 47, 47)),
                NotificationType.Info => new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                _ => new SolidColorBrush(Color.FromRgb(69, 90, 100))
            };
        }

        #endregion

        #region 内部类

        private class NotificationMessage
        {
            public NotificationType Type { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public SharedCommon.ErrorSeverity Severity { get; set; }
            public TimeSpan Duration { get; set; }
            public string Icon { get; set; } = string.Empty;
        }

        private enum NotificationType
        {
            Info,
            Success,
            Warning,
            Error
        }

        /// <summary>
        /// 简单的输入对话框
        /// </summary>
        private class InputDialog : Window
        {
            public string Prompt { get; set; } = string.Empty;
            public string InputText { get; set; } = string.Empty;

            private TextBox _inputTextBox;

            public InputDialog()
            {
                Width = 400;
                Height = 150;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                WindowStyle = WindowStyle.ToolWindow;

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.Margin = new Thickness(10);

                var promptLabel = new TextBlock
                {
                    Text = Prompt,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                Grid.SetRow(promptLabel, 0);
                grid.Children.Add(promptLabel);

                _inputTextBox = new TextBox
                {
                    Text = InputText,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                Grid.SetRow(_inputTextBox, 1);
                grid.Children.Add(_inputTextBox);

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var okButton = new Button
                {
                    Content = "确定",
                    Width = 75,
                    Margin = new Thickness(0, 0, 5, 0),
                    IsDefault = true
                };
                okButton.Click += (s, e) =>
                {
                    InputText = _inputTextBox.Text;
                    DialogResult = true;
                };
                buttonPanel.Children.Add(okButton);

                var cancelButton = new Button
                {
                    Content = "取消",
                    Width = 75,
                    IsCancel = true
                };
                cancelButton.Click += (s, e) => DialogResult = false;
                buttonPanel.Children.Add(cancelButton);

                Grid.SetRow(buttonPanel, 2);
                grid.Children.Add(buttonPanel);

                Content = grid;

                Loaded += (s, e) => _inputTextBox.Focus();
            }
        }

        #endregion

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 停止并释放DispatcherTimer
                    _displayTimer?.Stop();
                    if (_displayTimer != null)
                    {
                        _displayTimer.Tick -= ProcessNotificationQueue;
                    }

                    // 清理通知队列
                    while (_messageQueue.TryDequeue(out _)) { }

                    _logger?.LogInformation("用户通知服务已释放资源");
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    /// 用户通知服务接口
    /// UltraThink Phase 5.3: 扩展支持增强错误处理
    /// </summary>
    public interface IUserNotificationService
    {
        void Initialize(Window mainWindow);

        // 基础通知方法
        Task ShowSuccessAsync(string message, int? durationSeconds = null);
        Task ShowWarningAsync(string message, int? durationSeconds = null);
        Task ShowErrorAsync(string message, SharedCommon.ErrorSeverity severity, int? durationSeconds = null);
        Task ShowInfoAsync(string message, int? durationSeconds = null);

        // 增强错误处理支持的方法重载
        Task ShowInfoAsync(string message, NotificationConfiguration configuration);
        Task ShowWarningAsync(string message, NotificationConfiguration configuration);
        Task ShowErrorAsync(string message, string[] suggestedActions, NotificationConfiguration configuration);
        Task ShowCriticalErrorAsync(SharedCommon.HandledError handledError, NotificationConfiguration configuration);

        // 对话框和输入
        Task<bool> ShowConfirmationAsync(string message, string title = "确认");
        Task<string?> ShowInputAsync(string prompt, string title = "输入", string defaultValue = "");

        // 进度显示
        void ShowProgress(string message, int percentage);
        void HideProgress();
    }
}
