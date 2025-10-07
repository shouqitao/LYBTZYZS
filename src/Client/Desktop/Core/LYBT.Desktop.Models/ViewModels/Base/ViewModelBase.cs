using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Disposables;
using System.Windows;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Mvvm;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 简化的ViewModel基类 - 遵循"适度设计、拒绝过度工程"原则
    /// 提供核心的MVVM功能，避免过度复杂的架构
    /// </summary>
    public abstract class ViewModelBase : BindableBase, IDisposable, INotifyDataErrorInfo
    {
        #region 核心依赖服务

        protected readonly IEventAggregator EventAggregator;
        protected readonly ILoggerFactory LoggerFactory;
        protected readonly ILogger Logger;

        #endregion

        #region 状态属性

        private readonly CompositeDisposable _disposables = new();
        private bool _disposed = false;

        private bool _isLoading = false;
        private bool _isBusy = false;
        private bool _hasError = false;
        private string _errorMessage = string.Empty;
        private string _statusMessage = string.Empty;
        private readonly Dictionary<string, List<string>> _validationErrors = new();

        /// <summary>
        /// 是否正在加载数据
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnLoadingStateChanged(value);
                    RefreshCommands();
                }
            }
        }

        /// <summary>
        /// 是否正在执行操作
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RefreshCommands();
                }
            }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            protected set => SetProperty(ref _hasError, value);
        }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            protected set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    HasError = !string.IsNullOrWhiteSpace(value);
                }
            }
        }

        #endregion

        #region INotifyDataErrorInfo 实现

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        public bool HasErrors => _validationErrors.Any();

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return _validationErrors.SelectMany(x => x.Value);

            return _validationErrors.TryGetValue(propertyName, out var errors) ? errors : Enumerable.Empty<string>();
        }

        #endregion

        #region 构造函数

        protected ViewModelBase(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory)
        {
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            Logger = loggerFactory.CreateLogger(GetType());

            InitializeCommands();
            SubscribeToEvents();
        }

        #endregion

        #region 虚方法 - 子类可重写

        /// <summary>
        /// 初始化命令
        /// </summary>
        protected virtual void InitializeCommands()
        {
            // 子类实现
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        protected virtual void SubscribeToEvents()
        {
            // 子类实现
        }

        /// <summary>
        /// 加载状态变化时触发
        /// </summary>
        protected virtual void OnLoadingStateChanged(bool isLoading)
        {
            // 子类可重写
        }

        /// <summary>
        /// 刷新命令状态
        /// </summary>
        protected virtual void RefreshCommands()
        {
            // 子类实现具体的命令刷新逻辑
        }

        #endregion

        #region 安全执行方法

        /// <summary>
        /// 安全执行异步操作
        /// </summary>
        protected async Task ExecuteSafelyAsync(
            Func<Task> operation,
            string? operationName = null,
            bool showProgress = true)
        {
            try
            {
                IsBusy = true;
                ClearError();

                if (showProgress)
                {
                    StatusMessage = $"正在{operationName ?? "执行操作"}...";
                }

                await operation().ConfigureAwait(false);

                if (showProgress)
                {
                    StatusMessage = $"{operationName ?? "操作"}完成";

                    // 延迟3秒后自动清除StatusMessage，避免永久显示
                    var completionMessage = StatusMessage;
                    _ = Task.Delay(TimeSpan.FromSeconds(3))
                        .ContinueWith(_ =>
                        {
                            // 只清除当前的完成消息，避免误清除后续操作的消息
                            RunOnUIThread(() =>
                            {
                                if (StatusMessage == completionMessage)
                                {
                                    StatusMessage = string.Empty;
                                }
                            });
                        });
                }
            }
            catch (TaskCanceledException)
            {
                StatusMessage = $"{operationName ?? "操作"}已取消";
                Logger.LogInformation("{Operation}已取消", operationName ?? "操作");
            }
            catch (Exception ex)
            {
                StatusMessage = $"{operationName ?? "操作"}失败";
                HandleError(ex, operationName);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 安全执行有返回值的异步操作
        /// </summary>
        protected async Task<T?> ExecuteSafelyAsync<T>(
            Func<Task<T>> operation,
            string? operationName = null,
            T? defaultValue = default,
            bool showProgress = true)
        {
            try
            {
                IsBusy = true;
                ClearError();

                if (showProgress)
                {
                    StatusMessage = $"正在{operationName ?? "执行操作"}...";
                }

                var result = await operation().ConfigureAwait(false);

                if (showProgress)
                {
                    StatusMessage = $"{operationName ?? "操作"}完成";

                    // 延迟3秒后自动清除StatusMessage，避免永久显示
                    var completionMessage = StatusMessage;
                    _ = Task.Delay(TimeSpan.FromSeconds(3))
                        .ContinueWith(_ =>
                        {
                            // 只清除当前的完成消息，避免误清除后续操作的消息
                            RunOnUIThread(() =>
                            {
                                if (StatusMessage == completionMessage)
                                {
                                    StatusMessage = string.Empty;
                                }
                            });
                        });
                }

                return result;
            }
            catch (TaskCanceledException)
            {
                StatusMessage = $"{operationName ?? "操作"}已取消";
                Logger.LogInformation("{Operation}已取消", operationName ?? "操作");
                return defaultValue;
            }
            catch (Exception ex)
            {
                StatusMessage = $"{operationName ?? "操作"}失败";
                HandleError(ex, operationName);
                return defaultValue;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 安全执行同步操作
        /// </summary>
        protected void ExecuteSafely(Action action, string? operationName = null)
        {
            try
            {
                IsBusy = true;
                ClearError();
                action();
                StatusMessage = $"{operationName ?? "操作"}完成";
            }
            catch (Exception ex)
            {
                StatusMessage = $"{operationName ?? "操作"}失败";
                HandleError(ex, operationName ?? "操作");
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region 错误处理

        /// <summary>
        /// 处理错误
        /// </summary>
        protected virtual void HandleError(Exception ex, string? context = null)
        {
            Logger.LogError(ex, "错误发生在: {Context}", context ?? "未知操作");
            ErrorMessage = GetUserFriendlyMessage(ex);

            // 简化的错误显示
            RunOnUIThread(() =>
            {
                MessageBox.Show(
                    ErrorMessage,
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
        }

        /// <summary>
        /// 获取友好的错误消息
        /// </summary>
        protected virtual string GetUserFriendlyMessage(Exception ex)
        {
            return ex switch
            {
                ValidationException => "输入数据验证失败",
                UnauthorizedAccessException => "权限不足",
                TimeoutException => "操作超时",
                TaskCanceledException => "操作已取消",
                _ => "操作失败，请重试"
            };
        }

        /// <summary>
        /// 清除错误状态
        /// </summary>
        protected void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 添加验证错误
        /// </summary>
        protected void AddValidationError(string propertyName, string errorMessage)
        {
            if (!_validationErrors.TryGetValue(propertyName, out var errors))
            {
                errors = new List<string>();
                _validationErrors[propertyName] = errors;
            }

            if (!errors.Contains(errorMessage))
            {
                errors.Add(errorMessage);
                OnErrorsChanged(propertyName);
                RaisePropertyChanged(nameof(HasErrors));
            }
        }

        /// <summary>
        /// 清除验证错误
        /// </summary>
        protected void ClearValidationErrors(string? propertyName = null)
        {
            if (propertyName == null)
            {
                var propertyNames = _validationErrors.Keys.ToList();
                _validationErrors.Clear();

                foreach (var name in propertyNames)
                {
                    OnErrorsChanged(name);
                }
            }
            else if (_validationErrors.ContainsKey(propertyName))
            {
                _validationErrors.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }

            RaisePropertyChanged(nameof(HasErrors));
        }

        /// <summary>
        /// 触发验证错误变化事件
        /// </summary>
        protected virtual void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 设置状态消息
        /// </summary>
        protected void SetStatus(string message)
        {
            StatusMessage = message;
            Logger.LogDebug("状态更新: {Message}", message);
        }

        /// <summary>
        /// 清除状态消息
        /// </summary>
        protected void ClearStatus()
        {
            StatusMessage = string.Empty;
        }

        /// <summary>
        /// 在UI线程上执行操作
        /// </summary>
        protected void RunOnUIThread(Action action)
        {
            Application.Current?.Dispatcher?.Invoke(action);
        }

        /// <summary>
        /// 添加需要释放的资源
        /// </summary>
        protected void AddDisposable(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        #endregion

        #region IDisposable 实现

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _disposables?.Dispose();
                OnDisposing();
            }

            _disposed = true;
        }

        /// <summary>
        /// 释放时的额外清理工作
        /// </summary>
        protected virtual void OnDisposing()
        {
            // 子类可重写
        }

        #endregion
    }
}
