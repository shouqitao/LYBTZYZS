using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Services.Notifications;

namespace LYBT.Desktop.Services.UserExperience
{
    /// <summary>
    /// 反馈类型枚举
    /// </summary>
    public enum FeedbackType
    {
        None,
        Success,
        Error,
        Warning,
        Info
    }

    /// <summary>
    /// 用户体验增强服务接口 - 简化版本，遵循"适度设计、拒绝过度工程"原则
    /// </summary>
    public interface IUserExperienceService : INotifyPropertyChanged, IDisposable
    {
        // 属性
        bool IsGlobalLoading { get; }
        string LoadingMessage { get; }
        string StatusMessage { get; }
        FeedbackType CurrentFeedbackType { get; }
        int OperationProgress { get; }

        // 加载状态管理
        void StartGlobalLoading(string message = "加载中...");
        void StopGlobalLoading();
        void UpdateProgress(int progress, string? message = null);

        // 用户反馈
        void ShowSuccessFeedback(string message);
        void ShowErrorFeedback(string message);
        void ShowWarningFeedback(string message);
        void ShowInfoFeedback(string message);
        void ClearStatusMessage();

        // 操作执行与反馈
        Task<T> ExecuteWithFeedbackAsync<T>(
            Func<Task<T>> operation,
            string loadingMessage = "处理中...",
            string successMessage = "操作成功",
            string errorMessage = "操作失败");

        Task ExecuteWithFeedbackAsync(
            Func<Task> operation,
            string loadingMessage = "处理中...",
            string successMessage = "操作成功",
            string errorMessage = "操作失败");

        Task ShowFriendlyErrorAsync(Exception exception, string context = "");
    }

    /// <summary>
    /// 简化的用户体验增强服务 - 遵循"适度设计、拒绝过度工程"原则
    /// 提供核心的用户体验功能，避免过度复杂的依赖
    /// </summary>
    public class UserExperienceService : IUserExperienceService
    {
        private readonly ILogger<UserExperienceService> _logger;
        private readonly INotificationService _notificationService;
        private readonly DispatcherTimer _feedbackTimer;

        private bool _isGlobalLoading = false;
        private string _loadingMessage = "加载中...";
        private string _statusMessage = string.Empty;
        private FeedbackType _currentFeedbackType = FeedbackType.None;
        private int _operationProgress = 0;

        public UserExperienceService(
            ILogger<UserExperienceService> logger,
            INotificationService notificationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            // 初始化反馈定时器
            _feedbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3) // 3秒后自动清除状态消息
            };
            _feedbackTimer.Tick += OnFeedbackTimerTick;
        }

        #region 属性

        public bool IsGlobalLoading
        {
            get => _isGlobalLoading;
            private set
            {
                if (_isGlobalLoading != value)
                {
                    _isGlobalLoading = value;
                    OnPropertyChanged();

                    // 更新鼠标指针
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        Mouse.OverrideCursor = value ? Cursors.Wait : null;
                    });
                }
            }
        }

        public string LoadingMessage
        {
            get => _loadingMessage;
            private set
            {
                if (_loadingMessage != value)
                {
                    _loadingMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public FeedbackType CurrentFeedbackType
        {
            get => _currentFeedbackType;
            private set
            {
                if (_currentFeedbackType != value)
                {
                    _currentFeedbackType = value;
                    OnPropertyChanged();
                }
            }
        }

        public int OperationProgress
        {
            get => _operationProgress;
            private set
            {
                if (_operationProgress != value)
                {
                    _operationProgress = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region 加载状态管理

        public void StartGlobalLoading(string message = "加载中...")
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                LoadingMessage = message;
                IsGlobalLoading = true;
                OperationProgress = 0;
                _logger.LogDebug("开始全局加载: {Message}", message);
            });
        }

        public void StopGlobalLoading()
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                IsGlobalLoading = false;
                OperationProgress = 100;
                _logger.LogDebug("停止全局加载");
            });
        }

        public void UpdateProgress(int progress, string? message = null)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                OperationProgress = Math.Max(0, Math.Min(100, progress));
                if (!string.IsNullOrEmpty(message))
                {
                    LoadingMessage = message;
                }
            });
        }

        #endregion

        #region 用户反馈

        public void ShowSuccessFeedback(string message)
        {
            ShowStatusMessage(message, FeedbackType.Success);
            _logger.LogInformation("用户操作成功: {Message}", message);
        }

        public void ShowErrorFeedback(string message)
        {
            ShowStatusMessage(message, FeedbackType.Error);
            _logger.LogWarning("用户操作错误: {Message}", message);
        }

        public void ShowWarningFeedback(string message)
        {
            ShowStatusMessage(message, FeedbackType.Warning);
            _logger.LogWarning("用户操作警告: {Message}", message);
        }

        public void ShowInfoFeedback(string message)
        {
            ShowStatusMessage(message, FeedbackType.Info);
            _logger.LogDebug("用户操作信息: {Message}", message);
        }

        public void ClearStatusMessage()
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                StatusMessage = string.Empty;
                CurrentFeedbackType = FeedbackType.None;
                _feedbackTimer.Stop();
            });
        }

        #endregion

        #region 操作执行与反馈

        public async Task<T> ExecuteWithFeedbackAsync<T>(
            Func<Task<T>> operation,
            string loadingMessage = "处理中...",
            string successMessage = "操作成功",
            string errorMessage = "操作失败")
        {
            StartGlobalLoading(loadingMessage);

            try
            {
                var result = await operation();
                StopGlobalLoading();
                ShowSuccessFeedback(successMessage);
                return result;
            }
            catch (Exception ex)
            {
                StopGlobalLoading();
                var errorMsg = $"{errorMessage}: {GetUserFriendlyErrorMessage(ex)}";
                ShowErrorFeedback(errorMsg);
                throw;
            }
        }

        public async Task ExecuteWithFeedbackAsync(
            Func<Task> operation,
            string loadingMessage = "处理中...",
            string successMessage = "操作成功",
            string errorMessage = "操作失败")
        {
            StartGlobalLoading(loadingMessage);

            try
            {
                await operation();
                StopGlobalLoading();
                ShowSuccessFeedback(successMessage);
            }
            catch (Exception ex)
            {
                StopGlobalLoading();
                var errorMsg = $"{errorMessage}: {GetUserFriendlyErrorMessage(ex)}";
                ShowErrorFeedback(errorMsg);
                throw;
            }
        }

        public async Task ShowFriendlyErrorAsync(Exception exception, string context = "")
        {
            var userFriendlyMessage = GetUserFriendlyErrorMessage(exception);
            var fullMessage = string.IsNullOrEmpty(context)
                ? userFriendlyMessage
                : $"{context}: {userFriendlyMessage}";

            await _notificationService.ShowErrorAsync(fullMessage, "操作失败");
            ShowErrorFeedback(userFriendlyMessage);
        }

        #endregion

        #region 私有方法

        private void ShowStatusMessage(string message, FeedbackType feedbackType)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                StatusMessage = message;
                CurrentFeedbackType = feedbackType;

                // 重新启动定时器
                _feedbackTimer.Stop();
                _feedbackTimer.Start();
            });
        }

        private void OnFeedbackTimerTick(object? sender, EventArgs e)
        {
            ClearStatusMessage();
        }

        private string GetUserFriendlyErrorMessage(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => "输入参数不能为空",
                ArgumentException => "输入参数格式不正确",
                UnauthorizedAccessException => "您没有执行此操作的权限",
                TimeoutException => "操作超时，请稍后重试",
                HttpRequestException => "网络连接失败，请检查网络状态",
                InvalidOperationException => "当前状态下无法执行此操作",
                NotSupportedException => "当前不支持此功能",
                _ => exception.Message.Contains("SQL") || exception.Message.Contains("Database")
                    ? "数据库操作失败，请稍后重试"
                    : exception.Message
            };
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _feedbackTimer?.Stop();
            Mouse.OverrideCursor = null;
        }

        #endregion
    }
}
