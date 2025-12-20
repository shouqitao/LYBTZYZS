using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reactive.Disposables;
using System.Windows;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Localization;
using LYBT.Desktop.Infrastructure.Logging;
using LYBT.Shared.ExceptionHandling.Exceptions;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Mvvm;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>简化的ViewModel基类 - 提供核心的MVVM功能</summary>
    public abstract class ViewModelBase : BindableBase, IDisposable, INotifyDataErrorInfo
    {
        protected readonly IEventAggregator EventAggregator;
        protected readonly ILoggerFactory LoggerFactory;
        protected readonly ILogger Logger;

        private readonly CompositeDisposable _disposables = new();
        private bool _disposed = false;
        private bool _isLoading = false;
        private bool _isBusy = false;
        private bool _hasError = false;
        private string _errorMessage = string.Empty;
        private string _statusMessage = string.Empty;
        private readonly Dictionary<string, List<string>> _validationErrors = new();
        private const int STATUS_MESSAGE_AUTO_CLEAR_DELAY_SECONDS = 3;

        /// <summary>是否正在加载数据</summary>
        public bool IsLoading { get => _isLoading; set { if (SetProperty(ref _isLoading, value)) { OnLoadingStateChanged(value); RefreshCommands(); } } }

        /// <summary>是否正在执行操作</summary>
        public bool IsBusy { get => _isBusy; set { if (SetProperty(ref _isBusy, value)) RefreshCommands(); } }

        /// <summary>状态消息</summary>
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        /// <summary>是否有错误</summary>
        public bool HasError { get => _hasError; protected set => SetProperty(ref _hasError, value); }

        /// <summary>错误消息</summary>
        public string ErrorMessage { get => _errorMessage; protected set { if (SetProperty(ref _errorMessage, value)) HasError = !string.IsNullOrWhiteSpace(value); } }

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        public bool HasErrors => _validationErrors.Any();
        public IEnumerable GetErrors(string? propertyName) => string.IsNullOrEmpty(propertyName) ? _validationErrors.SelectMany(x => x.Value) : _validationErrors.TryGetValue(propertyName, out var errors) ? errors : Enumerable.Empty<string>();

        /// <summary>用于XAML绑定的验证错误访问器</summary>
        public ValidationErrorsAccessor Errors { get; }

        /// <summary>用于XAML绑定的属性错误状态访问器</summary>
        public ValidationHasErrorsAccessor HasErrorsDictionary { get; }

        /// <summary>验证错误访问器 - 支持XAML索引器绑定</summary>
        public class ValidationErrorsAccessor
        {
            private readonly Dictionary<string, List<string>> _errors;
            public ValidationErrorsAccessor(Dictionary<string, List<string>> errors) => _errors = errors;
            public string this[string propertyName] => _errors.TryGetValue(propertyName, out var errors) && errors.Count > 0 ? errors[0] : string.Empty;
        }

        /// <summary>验证错误状态访问器 - 支持XAML索引器绑定</summary>
        public class ValidationHasErrorsAccessor
        {
            private readonly Dictionary<string, List<string>> _errors;
            public ValidationHasErrorsAccessor(Dictionary<string, List<string>> errors) => _errors = errors;
            public bool this[string propertyName] => _errors.ContainsKey(propertyName) && _errors[propertyName].Count > 0;
        }

        protected ViewModelBase(IEventAggregator eventAggregator, ILoggerFactory loggerFactory)
        {
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            Logger = loggerFactory.CreateLogger(GetType());
            Errors = new ValidationErrorsAccessor(_validationErrors);
            HasErrorsDictionary = new ValidationHasErrorsAccessor(_validationErrors);
            InitializeCommands();
            SubscribeToEvents();
        }

        protected virtual void InitializeCommands() { }
        protected virtual void SubscribeToEvents() { }
        protected virtual void OnLoadingStateChanged(bool isLoading) { }
        protected virtual void RefreshCommands() { }

        private void AutoClearStatusMessage(string message) => _ = Task.Delay(TimeSpan.FromSeconds(STATUS_MESSAGE_AUTO_CLEAR_DELAY_SECONDS)).ContinueWith(_ => RunOnUIThread(() => { if (StatusMessage == message) StatusMessage = string.Empty; }));

        /// <summary>安全执行异步操作</summary>
        protected async Task ExecuteSafelyAsync(Func<Task> operation, string? operationName = null, bool showProgress = true) => await ExecuteSafelyAsync(async () => { await operation().ConfigureAwait(false); return true; }, operationName, false, showProgress);

        /// <summary>安全执行有返回值的异步操作</summary>
        protected async Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> operation, string? operationName = null, T? defaultValue = default, bool showProgress = true)
        {
            try
            {
                SetupOperationStart(operationName, showProgress);
                var result = await operation().ConfigureAwait(false);
                HandleOperationCompletion(operationName, showProgress);
                return result;
            }
            catch (TaskCanceledException) { HandleOperationCancellation(operationName); return defaultValue; }
            catch (Exception ex) { HandleOperationFailure(ex, operationName); return defaultValue; }
            finally { IsBusy = false; }
        }

        private void SetupOperationStart(string? operationName, bool showProgress) { IsBusy = true; ClearError(); if (showProgress) StatusMessage = $"正在{operationName ?? "执行操作"}..."; }
        private void HandleOperationCompletion(string? operationName, bool showProgress) { if (showProgress) { StatusMessage = $"{operationName ?? "操作"}完成"; AutoClearStatusMessage(StatusMessage); } }
        private void HandleOperationCancellation(string? operationName) { StatusMessage = $"{operationName ?? "操作"}已取消"; Logger.LogInformation("{Operation}已取消", operationName ?? "操作"); }
        private void HandleOperationFailure(Exception ex, string? operationName) { StatusMessage = $"{operationName ?? "操作"}失败"; HandleError(ex, operationName); }

        #region SafeExecuteAsync - HTTP状态码感知的安全执行

        /// <summary>
        /// 安全执行异步操作（带HTTP状态码特殊处理）
        /// ERR-009: ViewModel安全执行模式 - 统一异常处理入口
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="action">要执行的异步操作</param>
        /// <param name="operationName">操作名称（用于日志和状态提示）</param>
        /// <param name="fallbackValue">异常时的回退值</param>
        /// <param name="onError">错误回调（可选）</param>
        /// <returns>操作结果或回退值</returns>
        protected async Task<T?> SafeExecuteAsync<T>(
            Func<Task<T>> action,
            string operationName,
            T? fallbackValue = default,
            Action<Exception>? onError = null)
        {
            try
            {
                IsBusy = true;
                ClearError();
                return await action().ConfigureAwait(false);
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorizedAsync(operationName);
                return fallbackValue;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                await HandleConflictAsync(operationName);
                return fallbackValue;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.GatewayTimeout ||
                                          ex.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                await HandleServiceUnavailableAsync(operationName);
                return fallbackValue;
            }
            catch (ApiException ex)
            {
                await HandleApiExceptionAsync(ex, operationName);
                onError?.Invoke(ex);
                return fallbackValue;
            }
            catch (TaskCanceledException)
            {
                HandleOperationCancellation(operationName);
                return fallbackValue;
            }
            catch (Exception ex)
            {
                HandleError(ex, operationName);
                onError?.Invoke(ex);
                return fallbackValue;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 安全执行无返回值的异步操作（带HTTP状态码特殊处理）
        /// </summary>
        protected async Task SafeExecuteAsync(
            Func<Task> action,
            string operationName,
            Action<Exception>? onError = null)
        {
            await SafeExecuteAsync(async () =>
            {
                await action().ConfigureAwait(false);
                return true;
            }, operationName, false, onError);
        }

        /// <summary>
        /// 处理API异常
        /// Phase 4.4: 添加CorrelationId追踪
        /// </summary>
        protected virtual async Task HandleApiExceptionAsync(ApiException ex, string operationName)
        {
            var correlationId = CorrelationIdContext.CurrentOrNew;
            var trackingCode = ClientErrorMessageMapper.GetShortTrackingCode();

            Logger.LogWarning(ex, "API请求失败: {Operation}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                operationName, ex.StatusCode, correlationId);

            var userMessage = ClientErrorMessageMapper.GetUserMessageFromStatusCode((int)ex.StatusCode);
            var messageWithTracking = $"{userMessage} (追踪码: {trackingCode})";

            ErrorMessage = messageWithTracking;
            StatusMessage = $"{operationName}失败";

            await ShowErrorNotificationAsync(messageWithTracking);
        }

        /// <summary>
        /// 处理401未授权响应 - 清除会话并导航到登录页
        /// ERR-010: 401 Unauthorized处理
        /// </summary>
        protected virtual async Task HandleUnauthorizedAsync(string operationName)
        {
            Logger.LogWarning("会话已过期或未授权: {Operation}", operationName);

            var loginCoordinator = GetService<ILoginCoordinator>();
            if (loginCoordinator != null)
            {
                await RunOnUIThreadAsync(async () =>
                {
                    await loginCoordinator.LogoutAsync();
                });
            }

            ErrorMessage = "登录已过期，请重新登录";
            StatusMessage = string.Empty;
        }

        /// <summary>
        /// 处理409冲突响应 - 提示数据已被修改
        /// ERR-010: 409 Conflict处理
        /// </summary>
        protected virtual async Task HandleConflictAsync(string operationName)
        {
            Logger.LogWarning("数据冲突: {Operation}", operationName);

            var notificationService = GetService<IUserNotificationService>();
            if (notificationService != null)
            {
                var shouldRefresh = await notificationService.ShowConfirmAsync(
                    "数据冲突",
                    "数据已被其他用户修改，是否刷新获取最新数据？");

                if (shouldRefresh)
                {
                    await OnConflictRefreshRequestedAsync();
                }
            }

            ErrorMessage = "数据已被修改，请刷新后重试";
            StatusMessage = string.Empty;
        }

        /// <summary>
        /// 处理504网关超时或503服务不可用
        /// ERR-010: 504 Gateway Timeout处理
        /// </summary>
        protected virtual async Task HandleServiceUnavailableAsync(string operationName)
        {
            Logger.LogWarning("服务暂时不可用: {Operation}", operationName);

            ErrorMessage = "服务暂时不可用，请稍后重试";
            StatusMessage = string.Empty;

            await ShowErrorNotificationAsync("服务暂时不可用，请稍后重试");
        }

        /// <summary>
        /// 当冲突后用户选择刷新时调用 - 子类可重写以执行数据刷新
        /// </summary>
        protected virtual Task OnConflictRefreshRequestedAsync() => Task.CompletedTask;

        /// <summary>
        /// 显示错误通知（如果IUserNotificationService可用）
        /// </summary>
        private async Task ShowErrorNotificationAsync(string message)
        {
            var notificationService = GetService<IUserNotificationService>();
            if (notificationService != null)
            {
                await notificationService.ShowErrorAsync(message);
            }
        }

        /// <summary>
        /// 获取服务实例 - 子类可重写以提供自定义服务解析
        /// </summary>
        protected virtual T? GetService<T>() where T : class => null;

        /// <summary>
        /// 在UI线程上异步执行操作
        /// </summary>
        protected Task RunOnUIThreadAsync(Func<Task> action)
        {
            if (Application.Current?.Dispatcher == null)
                return action();

            return Application.Current.Dispatcher.InvokeAsync(action).Task;
        }

        #endregion

        /// <summary>安全执行同步操作</summary>
        protected void ExecuteSafely(Action action, string? operationName = null)
        {
            try { IsBusy = true; ClearError(); action(); StatusMessage = $"{operationName ?? "操作"}完成"; }
            catch (Exception ex) { StatusMessage = $"{operationName ?? "操作"}失败"; HandleError(ex, operationName ?? "操作"); }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// 处理错误
        /// Phase 4.4: 添加CorrelationId追踪，日志包含追踪码
        /// </summary>
        protected virtual void HandleError(Exception ex, string? context = null)
        {
            var correlationId = CorrelationIdContext.CurrentOrNew;
            var trackingCode = ClientErrorMessageMapper.GetShortTrackingCode();

            // 日志包含完整CorrelationId
            Logger.LogError(ex, "错误发生在: {Context}, CorrelationId: {CorrelationId}", context ?? "未知操作", correlationId);

            // 用户消息包含短追踪码
            var baseMessage = GetUserFriendlyMessage(ex);
            ErrorMessage = $"{baseMessage} (追踪码: {trackingCode})";
            HasError = true;
        }

        /// <summary>获取友好的错误消息</summary>
        protected virtual string GetUserFriendlyMessage(Exception ex) => ex switch { System.ComponentModel.DataAnnotations.ValidationException => "输入数据验证失败", UnauthorizedAccessException => "权限不足", TimeoutException => "操作超时", TaskCanceledException => "操作已取消", _ => "操作失败，请重试" };

        protected void ClearError() { ErrorMessage = string.Empty; HasError = false; }

        protected void AddValidationError(string propertyName, string errorMessage)
        {
            if (!_validationErrors.TryGetValue(propertyName, out var errors)) { errors = new List<string>(); _validationErrors[propertyName] = errors; }
            if (!errors.Contains(errorMessage)) { errors.Add(errorMessage); OnErrorsChanged(propertyName); RaisePropertyChanged(nameof(HasErrors)); }
        }

        protected void ClearValidationErrors(string? propertyName = null)
        {
            if (propertyName == null) { var names = _validationErrors.Keys.ToList(); _validationErrors.Clear(); foreach (var name in names) OnErrorsChanged(name); }
            else if (_validationErrors.ContainsKey(propertyName)) { _validationErrors.Remove(propertyName); OnErrorsChanged(propertyName); }
            RaisePropertyChanged(nameof(HasErrors));
        }

        protected virtual void OnErrorsChanged(string propertyName) => ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

        protected void SetStatus(string message) { StatusMessage = message; Logger.LogDebug("状态更新: {Message}", message); }
        protected void ClearStatus() => StatusMessage = string.Empty;
        protected void RunOnUIThread(Action action) => Application.Current?.Dispatcher?.Invoke(action);
        protected void AddDisposable(IDisposable disposable) => _disposables.Add(disposable);

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        protected virtual void Dispose(bool disposing) { if (_disposed) return; if (disposing) { _disposables?.Dispose(); OnDisposing(); } _disposed = true; }
        protected virtual void OnDisposing() { }
    }
}
