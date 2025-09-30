using System;
using System.Threading.Tasks;
using LYBT.Desktop.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services.ErrorHandling
{
    /// <summary>
    /// 错误处理服务实现 - UltraThink架构
    /// </summary>
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly ICommonDialogService? _dialogService;

        public ErrorHandlingService(ILogger<ErrorHandlingService> logger, ICommonDialogService? dialogService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dialogService = dialogService;
        }

        /// <inheritdoc/>
        public async Task HandleExceptionAsync(Exception exception, string? context = null)
        {
            _logger.LogError(exception, "异常处理: {Context}", context ?? "未知上下文");
            
            var errorMessage = GetUserFriendlyMessage(exception);
            if (_dialogService != null)
            {
                await _dialogService.ShowErrorAsync(errorMessage, "系统错误");
            }
        }

        /// <inheritdoc/>
        public async Task ShowErrorAsync(string message, string? title = null)
        {
            _logger.LogError("错误消息: {Message}", message);
            
            if (_dialogService != null)
            {
                await _dialogService.ShowErrorAsync(message, title ?? "错误");
            }
        }

        /// <inheritdoc/>
        public async Task ShowSuccessAsync(string message, string? title = null)
        {
            _logger.LogInformation("成功消息: {Message}", message);
            
            if (_dialogService != null)
            {
                await _dialogService.ShowInfoAsync(message, title ?? "成功");
            }
        }

        /// <inheritdoc/>
        public async Task ShowWarningAsync(string message, string? title = null)
        {
            _logger.LogWarning("警告消息: {Message}", message);
            
            if (_dialogService != null)
            {
                await _dialogService.ShowWarningAsync(message, title ?? "警告");
            }
        }

        /// <inheritdoc/>
        public async Task ShowInfoAsync(string message, string? title = null)
        {
            _logger.LogInformation("信息消息: {Message}", message);
            
            if (_dialogService != null)
            {
                await _dialogService.ShowInfoAsync(message, title ?? "信息");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ShowConfirmAsync(string message, string? title = null)
        {
            _logger.LogInformation("确认对话框: {Message}", message);
            
            if (_dialogService != null)
            {
                return await _dialogService.ShowConfirmAsync(message, title ?? "确认");
            }
            
            return false;
        }

        /// <inheritdoc/>
        public void RegisterGlobalExceptionHandlers()
        {
            try
            {
                _logger.LogInformation("注册全局异常处理器");

                // 注册应用程序域未处理异常
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                // 注册任务调度器未观察到的任务异常
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

                _logger.LogInformation("全局异常处理器注册完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册全局异常处理器失败");
            }
        }

        /// <summary>
        /// 处理应用程序域未处理异常
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception exception)
                {
                    _logger.LogCritical(exception, "应用程序域未处理异常");
                    _ = Task.Run(async () => await HandleExceptionAsync(exception, "AppDomain未处理异常"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "处理应用程序域未处理异常时发生错误");
            }
        }

        /// <summary>
        /// 处理未观察到的任务异常
        /// </summary>
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                _logger.LogError(e.Exception, "未观察到的任务异常");
                _ = Task.Run(async () => await HandleExceptionAsync(e.Exception, "TaskScheduler未观察异常"));
                e.SetObserved(); // 标记异常已被观察，防止应用程序崩溃
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "处理未观察任务异常时发生错误");
            }
        }

        private static string GetUserFriendlyMessage(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => "参数不能为空",
                ArgumentException => "参数值无效",
                InvalidOperationException => "当前操作无效",
                UnauthorizedAccessException => "访问被拒绝",
                TimeoutException => "操作超时",
                _ => "发生了未知错误，请稍后重试"
            };
        }
    }
}