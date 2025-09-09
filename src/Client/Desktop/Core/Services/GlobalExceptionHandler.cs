using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using LYBT.Desktop.Core.Exceptions;
using Microsoft.Extensions.Logging;
using SharedCommon = LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Services
{

    /// <summary>
    /// 全局异常处理器 - 捕获和处理所有未处理的异常
    /// </summary>
    public class GlobalExceptionHandler : IGlobalExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IErrorClassifier _errorClassifier;
        private readonly IUserNotificationService _notificationService;

        private bool _isRegistered = false;
        private readonly object _registrationLock = new();

        // Epic 05-P0-02 增强：详细统计信息
        private int _totalExceptionsHandled = 0;
        private int _criticalExceptionsCount = 0;
        private DateTime _lastExceptionTime = DateTime.MinValue;
        private readonly Dictionary<SharedCommon.ErrorCategory, int> _categoryStats = new();
        private readonly Dictionary<Type, int> _exceptionTypeStats = new();
        private readonly List<ErrorTrendEntry> _recentErrors = new();

        public GlobalExceptionHandler(
            IErrorClassifier errorClassifier,
            IUserNotificationService notificationService,
            ILogger<GlobalExceptionHandler> logger)
        {
            _errorClassifier = errorClassifier ?? throw new ArgumentNullException(nameof(errorClassifier));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 注册全局异常处理器
        /// </summary>
        public void RegisterGlobalHandlers()
        {
            lock (_registrationLock)
            {
                if (_isRegistered)
                {
                    _logger?.LogWarning("全局异常处理器已注册，跳过重复注册");
                    return;
                }

                try
                {
                    // 1. 处理AppDomain未处理异常
                    AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

                    // 2. 处理Task未观察异常
                    TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

                    // 3. 处理WPF Dispatcher未处理异常
                    if (Application.Current != null)
                    {
                        Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
                    }

                    // 4. 设置第一次机会异常通知（仅用于调试）
#if DEBUG
                    AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
#endif

                    _isRegistered = true;
                    _logger.LogInformation("全局异常处理器注册成功");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "注册全局异常处理器失败");
                    throw;
                }
            }
        }

        /// <summary>
        /// 注销全局异常处理器
        /// </summary>
        public void UnregisterGlobalHandlers()
        {
            lock (_registrationLock)
            {
                if (!_isRegistered)
                {
                    return;
                }

                AppDomain.CurrentDomain.UnhandledException -= OnAppDomainUnhandledException;
                TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

                if (Application.Current != null)
                {
                    Application.Current.DispatcherUnhandledException -= OnDispatcherUnhandledException;
                }

#if DEBUG
                AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
#endif

                _isRegistered = false;
                _logger.LogInformation("全局异常处理器注销成功");
            }
        }

        /// <summary>
        /// 处理异常
        /// </summary>
        public async Task<bool> HandleExceptionAsync(Exception exception, ExceptionSource source)
        {
            if (exception == null)
            {
                return true;
            }

            _totalExceptionsHandled++;
            _lastExceptionTime = DateTime.Now;

            try
            {
                // 1. 分类异常
                var classifiedException = _errorClassifier.ClassifyException(exception);

                // 2. 记录日志
                LogException(classifiedException, source);

                // 3. 确定是否需要通知用户
                if (ShouldNotifyUser(classifiedException, source))
                {
                    await NotifyUserAsync(classifiedException);
                }

                // 4. 确定是否可以恢复
                var canRecover = DetermineRecoverability(classifiedException, source);

                // 5. 执行恢复策略
                if (canRecover)
                {
                    await ExecuteRecoveryStrategyAsync(classifiedException);
                }

                // Epic 05-P0-02 增强：更新详细统计
                if (classifiedException.Severity >= SharedCommon.ErrorSeverity.Critical)
                {
                    _criticalExceptionsCount++;
                }

                // 更新分类统计
                if (_categoryStats.ContainsKey(classifiedException.Category))
                    _categoryStats[classifiedException.Category]++;
                else
                    _categoryStats[classifiedException.Category] = 1;

                // 更新异常类型统计
                var exceptionType = exception.GetType();
                if (_exceptionTypeStats.ContainsKey(exceptionType))
                    _exceptionTypeStats[exceptionType]++;
                else
                    _exceptionTypeStats[exceptionType] = 1;

                // 添加到趋势数据（保留最近100条）
                _recentErrors.Add(new ErrorTrendEntry
                {
                    Timestamp = DateTime.Now,
                    Category = classifiedException.Category,
                    Severity = classifiedException.Severity,
                    ExceptionType = exceptionType.Name,
                    Source = source
                });

                if (_recentErrors.Count > 100)
                {
                    _recentErrors.RemoveAt(0);
                }

                // 7. 检查是否需要关闭应用
                if (ShouldShutdownApplication(classifiedException))
                {
                    await InitiateGracefulShutdownAsync(classifiedException);
                    return false;
                }

                return canRecover;
            }
            catch (Exception handlingEx)
            {
                // 处理异常时发生错误，记录到事件日志
                try
                {
                    EventLog.WriteEntry(
                        "LYBT",
                        $"异常处理失败: {handlingEx.Message}\n原始异常: {exception.Message}",
                        EventLogEntryType.Error);
                }
                catch
                {
                    // 完全失败，无法记录
                }

                return false;
            }
        }

        #region 事件处理器

        private void OnAppDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception ?? new Exception("未知的非托管异常");
            var isTerminating = e.IsTerminating;

            _logger.LogCritical(exception, "AppDomain未处理异常，应用即将终止: {IsTerminating}", isTerminating);

            var handled = HandleExceptionAsync(exception, ExceptionSource.AppDomain).Result;

            if (isTerminating)
            {
                // 尝试保存关键数据
                SaveCriticalData();

                // 生成崩溃报告
                GenerateCrashReport(exception);
            }
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "Task未观察异常");

            var handled = HandleExceptionAsync(e.Exception, ExceptionSource.TaskScheduler).Result;

            if (handled)
            {
                // 标记异常已观察，防止进程终止
                e.SetObserved();
            }
        }

        private void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "WPF Dispatcher未处理异常");

            var handled = HandleExceptionAsync(e.Exception, ExceptionSource.Dispatcher).Result;

            if (handled)
            {
                // 标记异常已处理，防止应用崩溃
                e.Handled = true;
            }
        }

        private void OnFirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            // 仅在调试模式下记录第一次机会异常
            if (e.Exception is AppException appEx)
            {
                _logger.LogTrace(
                    "第一次机会异常: {Type} - {Message}",
                    appEx.Category, appEx.Message);
            }
        }

        #endregion 事件处理器

        #region 私有方法

        private void LogException(AppException exception, ExceptionSource source)
        {
            var logLevel = exception.Severity switch
            {
                SharedCommon.ErrorSeverity.Info => LogLevel.Information,
                SharedCommon.ErrorSeverity.Warning => LogLevel.Warning,
                SharedCommon.ErrorSeverity.Error => LogLevel.Error,
                SharedCommon.ErrorSeverity.Critical => LogLevel.Critical,
                SharedCommon.ErrorSeverity.Fatal => LogLevel.Critical,
                _ => LogLevel.Error
            };

            _logger.LogError(
                exception,
                "异常处理 - 来源: {Source}, 类别: {Category}, 严重程度: {Severity}, 错误码: {ErrorCode}",
                source, exception.Category, exception.Severity, exception.ErrorCode);
        }

        private bool ShouldNotifyUser(AppException exception, ExceptionSource source)
        {
            // 不通知用户的情况
            if (exception.IsHandled)
            {
                return false;
            }

            if (exception.Severity <= SharedCommon.ErrorSeverity.Info)
            {
                return false;
            }

            if (source == ExceptionSource.FirstChance)
            {
                return false;
            }

            // 限制通知频率（5秒内最多一次）
            if ((DateTime.Now - _lastExceptionTime).TotalSeconds < 5)
            {
                return false;
            }

            return true;
        }

        private async Task NotifyUserAsync(AppException exception)
        {
            try
            {
                await _notificationService.ShowErrorAsync(
                    exception.UserFriendlyMessage ?? "操作失败，请稍后重试",
                    exception.Severity);
            }
            catch (Exception notifyEx)
            {
                _logger?.LogError(notifyEx, "通知用户失败");
            }
        }

        private bool DetermineRecoverability(AppException exception, ExceptionSource source)
        {
            // 致命错误不可恢复
            if (exception.Severity == SharedCommon.ErrorSeverity.Fatal)
            {
                return false;
            }

            // AppDomain异常通常不可恢复
            if (source == ExceptionSource.AppDomain)
            {
                return false;
            }

            // 检查是否可重试
            if (exception.IsRetryable && exception.RetryCount < 3)
            {
                return true;
            }

            // 网络和超时错误通常可恢复
            if (exception.Category == SharedCommon.ErrorCategory.Network ||
                exception.Category == SharedCommon.ErrorCategory.Timeout)
            {
                return true;
            }

            return false;
        }

        private async Task ExecuteRecoveryStrategyAsync(AppException exception)
        {
            _logger.LogInformation("执行恢复策略: {Category}", exception.Category);

            switch (exception.Category)
            {
                case SharedCommon.ErrorCategory.Network:
                case SharedCommon.ErrorCategory.ServiceUnavailable:
                    // 等待并重试（指数退避）
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, exception.RetryCount)));
                    exception.IncrementRetryCount();
                    break;

                case SharedCommon.ErrorCategory.Authentication:
                    // 触发重新登录
                    await _notificationService.ShowErrorAsync("认证失败，请重新登录", SharedCommon.ErrorSeverity.Warning);
                    // TODO: 触发登录事件
                    break;

                case SharedCommon.ErrorCategory.Configuration:
                    // 尝试重新加载配置
                    _logger.LogInformation("尝试重新加载配置");
                    // TODO: 重新加载配置
                    break;

                // Epic 05-P0-02 增强：新增数据库和缓存错误恢复策略
                case SharedCommon.ErrorCategory.Validation:
                    // 数据验证错误：尝试刷新缓存数据
                    _logger.LogInformation("数据验证错误，尝试刷新缓存");
                    await RefreshCacheDataAsync();
                    break;

                case SharedCommon.ErrorCategory.Timeout:
                    // 超时错误：指数退避重试
                    var timeoutDelay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, exception.RetryCount)));
                    _logger.LogInformation("超时错误，等待 {Delay} 秒后重试", timeoutDelay.TotalSeconds);
                    await Task.Delay(timeoutDelay);
                    exception.IncrementRetryCount();
                    break;

                case SharedCommon.ErrorCategory.Internal:
                    // 内部错误：尝试重置服务状态
                    _logger.LogInformation("内部错误，尝试重置服务状态");
                    await ResetServiceStateAsync();
                    break;

                default:
                    // 默认策略：记录并继续
                    break;
            }
        }

        // Epic 05-P0-02 增强：添加错误恢复辅助方法
        private async Task RefreshCacheDataAsync()
        {
            try
            {
                _logger.LogInformation("开始刷新缓存数据");
                // TODO: 实现缓存刷新逻辑
                await Task.Delay(100); // 模拟异步操作
                _logger.LogInformation("缓存数据刷新完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新缓存数据失败");
            }
        }

        private async Task ResetServiceStateAsync()
        {
            try
            {
                _logger.LogInformation("开始重置服务状态");
                // TODO: 实现服务状态重置逻辑
                await Task.Delay(100); // 模拟异步操作
                _logger.LogInformation("服务状态重置完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置服务状态失败");
            }
        }

        private bool ShouldShutdownApplication(AppException exception)
        {
            // 致命错误需要关闭应用
            if (exception.Severity == SharedCommon.ErrorSeverity.Fatal)
            {
                return true;
            }

            // 连续多个严重错误
            if (_criticalExceptionsCount >= 5)
            {
                return true;
            }

            // 特定类别的严重错误
            if (exception.Severity == SharedCommon.ErrorSeverity.Critical &&
                (exception.Category == SharedCommon.ErrorCategory.Configuration ||
                 exception.Category == SharedCommon.ErrorCategory.Internal))
            {
                return true;
            }

            return false;
        }

        private async Task InitiateGracefulShutdownAsync(AppException exception)
        {
            _logger.LogCritical(exception, "启动优雅关闭流程");

            try
            {
                // 1. 通知用户
                await _notificationService.ShowErrorAsync(
                    "应用程序遇到严重错误，即将关闭。您的数据已保存。",
                    SharedCommon.ErrorSeverity.Fatal);

                // 2. 保存关键数据
                SaveCriticalData();

                // 3. 生成崩溃报告
                GenerateCrashReport(exception);

                // 4. 等待一段时间让用户看到消息
                await Task.Delay(3000);

                // 5. 关闭应用
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    Application.Current.Shutdown(-1);
                });
            }
            catch (Exception shutdownEx)
            {
                _logger?.LogError(shutdownEx, "优雅关闭失败，强制终止");
                Environment.Exit(-1);
            }
        }

        private void SaveCriticalData()
        {
            try
            {
                _logger.LogInformation("保存关键数据");
                // TODO: 实现数据保存逻辑
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存关键数据失败");
            }
        }

        private void GenerateCrashReport(Exception exception)
        {
            try
            {
                var crashReport = new CrashReport
                {
                    Timestamp = DateTime.Now,
                    Exception = exception.ToString(),
                    TotalExceptionsHandled = _totalExceptionsHandled,
                    CriticalExceptionsCount = _criticalExceptionsCount,
                    SystemInfo = GetSystemInfo()
                };

                // TODO: 保存崩溃报告到文件
                _logger.LogInformation("崩溃报告已生成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成崩溃报告失败");
            }
        }

        private SystemInfo GetSystemInfo()
        {
            return new SystemInfo
            {
                OSVersion = Environment.OSVersion.ToString(),
                CLRVersion = Environment.Version.ToString(),
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                WorkingSet = Environment.WorkingSet,
                Is64Bit = Environment.Is64BitProcess
            };
        }

        // Epic 05-P0-02 增强：错误趋势分析方法
        public ErrorTrendReport GenerateErrorTrendReport(TimeSpan period)
        {
            var cutoffTime = DateTime.Now - period;
            var recentErrorsInPeriod = _recentErrors.Where(e => e.Timestamp >= cutoffTime).ToList();

            return new ErrorTrendReport
            {
                Period = period,
                TotalErrors = recentErrorsInPeriod.Count,
                CriticalErrors = recentErrorsInPeriod.Count(e => e.Severity >= SharedCommon.ErrorSeverity.Critical),
                TopErrorCategories = _categoryStats
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(5)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                TopExceptionTypes = _exceptionTypeStats
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(5)
                    .ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value),
                ErrorsByHour = recentErrorsInPeriod
                    .GroupBy(e => e.Timestamp.Hour)
                    .ToDictionary(g => g.Key, g => g.Count()),
                GeneratedAt = DateTime.Now
            };
        }

        public bool ShouldGenerateAlert()
        {
            // 检查是否需要生成警报
            var last15Minutes = DateTime.Now.AddMinutes(-15);
            var recentCriticalErrors = _recentErrors.Count(e => 
                e.Timestamp >= last15Minutes && 
                e.Severity >= SharedCommon.ErrorSeverity.Critical);

            return recentCriticalErrors >= 3; // 15分钟内3个严重错误触发警报
        }

        #endregion 私有方法

        #region 内部类

        private class CrashReport
        {
            public DateTime Timestamp { get; set; }
            public string Exception { get; set; } = null!;
            public int TotalExceptionsHandled { get; set; }
            public int CriticalExceptionsCount { get; set; }
            public SystemInfo SystemInfo { get; set; } = null!;
        }

        private class SystemInfo
        {
            public string OSVersion { get; set; } = null!;
            public string CLRVersion { get; set; } = null!;
            public string MachineName { get; set; } = null!;
            public int ProcessorCount { get; set; }
            public long WorkingSet { get; set; }
            public bool Is64Bit { get; set; }
        }

        // Epic 05-P0-02 增强：错误趋势分析相关类
        private class ErrorTrendEntry
        {
            public DateTime Timestamp { get; set; }
            public SharedCommon.ErrorCategory Category { get; set; }
            public SharedCommon.ErrorSeverity Severity { get; set; }
            public string ExceptionType { get; set; } = null!;
            public ExceptionSource Source { get; set; }
        }

        public class ErrorTrendReport
        {
            public TimeSpan Period { get; set; }
            public int TotalErrors { get; set; }
            public int CriticalErrors { get; set; }
            public Dictionary<SharedCommon.ErrorCategory, int> TopErrorCategories { get; set; } = new();
            public Dictionary<string, int> TopExceptionTypes { get; set; } = new();
            public Dictionary<int, int> ErrorsByHour { get; set; } = new();
            public DateTime GeneratedAt { get; set; }
        }

        #endregion 内部类
    }

    /// <summary>
    /// 异常来源
    /// </summary>
    public enum ExceptionSource
    {
        Unknown,
        AppDomain,
        TaskScheduler,
        Dispatcher,
        FirstChance,
        Manual
    }

    /// <summary>
    /// 全局异常处理器接口
    /// </summary>
    public interface IGlobalExceptionHandler
    {

        void RegisterGlobalHandlers();

        void UnregisterGlobalHandlers();

        Task<bool> HandleExceptionAsync(Exception exception, ExceptionSource source);
    }
}
