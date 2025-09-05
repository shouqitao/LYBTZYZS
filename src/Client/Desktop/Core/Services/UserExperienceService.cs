using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using LYBT.Desktop.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// 用户体验增强服务 - P7-04 UltraThink用户体验优化
    /// 
    /// 功能特性：
    /// 1. 操作反馈管理（成功/失败提示）
    /// 2. 加载状态智能管理
    /// 3. 键盘快捷键支持
    /// 4. 错误友好显示
    /// 5. 界面响应性优化
    /// </summary>
    public class UserExperienceService : IUserExperienceService, INotifyPropertyChanged
    {
        #region 私有字段

        private readonly ILogger<UserExperienceService> _logger;
        private readonly ICustomDialogService _dialogService;
        private readonly DispatcherTimer _feedbackTimer;

        private bool _isGlobalLoading;
        private string _loadingMessage = "加载中...";
        private string _statusMessage = "";
        private FeedbackType _currentFeedbackType = FeedbackType.None;
        private int _operationProgress;

        #endregion

        #region 构造函数

        public UserExperienceService(
            ILogger<UserExperienceService> logger,
            ICustomDialogService dialogService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // 初始化反馈定时器
            _feedbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3) // 3秒后自动清除状态消息
            };
            _feedbackTimer.Tick += OnFeedbackTimerTick;
        }

        #endregion

        #region 公共属性

        /// <summary>全局加载状态</summary>
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

        /// <summary>加载消息</summary>
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

        /// <summary>状态消息</summary>
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

        /// <summary>当前反馈类型</summary>
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

        /// <summary>操作进度 (0-100)</summary>
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

        #region IUserExperienceService 实现

        /// <summary>开始全局加载</summary>
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

        /// <summary>停止全局加载</summary>
        public void StopGlobalLoading()
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                IsGlobalLoading = false;
                OperationProgress = 100;
                _logger.LogDebug("停止全局加载");
            });
        }

        /// <summary>更新操作进度</summary>
        public void UpdateProgress(int progress, string message = null)
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

        /// <summary>显示成功反馈</summary>
        public void ShowSuccessFeedback(string message)
        {
            ShowStatusMessage(message, FeedbackType.Success);
            _logger.LogInformation("用户操作成功: {Message}", message);
        }

        /// <summary>显示错误反馈</summary>
        public void ShowErrorFeedback(string message)
        {
            ShowStatusMessage(message, FeedbackType.Error);
            _logger.LogWarning("用户操作错误: {Message}", message);
        }

        /// <summary>显示警告反馈</summary>
        public void ShowWarningFeedback(string message)
        {
            ShowStatusMessage(message, FeedbackType.Warning);
            _logger.LogWarning("用户操作警告: {Message}", message);
        }

        /// <summary>显示信息反馈</summary>
        public void ShowInfoFeedback(string message)
        {
            ShowStatusMessage(message, FeedbackType.Info);
            _logger.LogDebug("用户操作信息: {Message}", message);
        }

        /// <summary>执行操作并提供用户反馈</summary>
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
                ShowErrorFeedback($"{errorMessage}: {ex.Message}");
                throw;
            }
        }

        /// <summary>执行操作并提供用户反馈（无返回值）</summary>
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
                ShowErrorFeedback($"{errorMessage}: {ex.Message}");
                throw;
            }
        }

        /// <summary>显示友好的错误信息</summary>
        public async Task ShowFriendlyErrorAsync(Exception exception, string context = "")
        {
            var userFriendlyMessage = GetUserFriendlyErrorMessage(exception);
            var fullMessage = string.IsNullOrEmpty(context)
                ? userFriendlyMessage
                : $"{context}: {userFriendlyMessage}";

            await _dialogService.ShowErrorAsync(fullMessage, "操作失败");
            ShowErrorFeedback(userFriendlyMessage);
        }

        /// <summary>清除状态消息</summary>
        public void ClearStatusMessage()
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                StatusMessage = "";
                CurrentFeedbackType = FeedbackType.None;
                _feedbackTimer.Stop();
            });
        }

        #endregion

        #region 私有方法

        /// <summary>显示状态消息</summary>
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

        /// <summary>反馈定时器触发事件</summary>
        private void OnFeedbackTimerTick(object sender, EventArgs e)
        {
            ClearStatusMessage();
        }

        /// <summary>获取用户友好的错误消息</summary>
        private string GetUserFriendlyErrorMessage(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => "输入参数不能为空",
                ArgumentException => "输入参数格式不正确",
                UnauthorizedAccessException => "您没有执行此操作的权限",
                TimeoutException => "操作超时，请稍后重试",
                System.Net.Http.HttpRequestException => "网络连接失败，请检查网络状态",
                InvalidOperationException => "当前状态下无法执行此操作",
                NotSupportedException => "当前不支持此功能",
                _ => exception.Message.Contains("SQL") || exception.Message.Contains("Database")
                    ? "数据库操作失败，请稍后重试"
                    : exception.Message
            };
        }

        #endregion

        #region INotifyPropertyChanged 实现

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable 实现

        public void Dispose()
        {
            _feedbackTimer?.Stop();
            Mouse.OverrideCursor = null;
        }

        #endregion
    }

    /// <summary>反馈类型枚举</summary>
    public enum FeedbackType
    {
        None,
        Success,
        Error,
        Warning,
        Info
    }
}
