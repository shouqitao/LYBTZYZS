using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Desktop.Core.Exceptions;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Common;
using Refit;
using ErrorCategory = LYBT.Shared.Models.Contracts.Common.ErrorCategory;
using ErrorSeverity = LYBT.Shared.Models.Contracts.Common.ErrorSeverity;
using SharedCommon = LYBT.Shared.Models.Contracts.Common;
using TimeoutException = System.TimeoutException;

namespace LYBT.Desktop.Services
{

    /// <summary>
    /// 统一错误处理服务实现
    /// </summary>
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ICustomDialogService? _customDialogService;

        // 错误消息映射表
        private readonly Dictionary<Type, Func<Exception, string>> _messageMapping;

        private readonly Dictionary<Type, ErrorCategory> _categoryMapping;
        private readonly Dictionary<Type, ErrorSeverity> _severityMapping;
        private readonly Dictionary<Type, string[]> _actionMapping;

        /// <inheritdoc/>
        public event EventHandler<SharedCommon.HandledError>? ErrorOccurred;

        /// <inheritdoc/>
        public event EventHandler<SharedCommon.HandledError>? CriticalErrorOccurred;

        /// <summary>
        /// 自定义对话框服务
        /// </summary>
        public ICustomDialogService? CustomDialogService => _customDialogService;

        public ErrorHandlingService(ICustomDialogService? customDialogService = null)
        {
            _customDialogService = customDialogService;

            _messageMapping = InitializeMessageMapping();
            _categoryMapping = InitializeCategoryMapping();
            _severityMapping = InitializeSeverityMapping();
            _actionMapping = InitializeActionMapping();
        }

        /// <inheritdoc/>
        public SharedCommon.HandledError HandleException(Exception exception, ErrorContext? context = null)
        {
            // 警告：同步异常处理可能导致死锁，建议使用 HandleExceptionAsync()
            try
            {
                return HandleExceptionAsync(exception, context).GetAwaiter().GetResult();
            }
            catch
            {
                // 异常处理本身出错时，返回基本错误信息
                return new SharedCommon.HandledError 
                { 
                    UserMessage = exception?.Message ?? "未知错误",
                    Severity = SharedCommon.ErrorSeverity.Error
                };
            }
        }

        /// <inheritdoc/>
        public async Task<SharedCommon.HandledError> HandleExceptionAsync(Exception exception, ErrorContext? context = null)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            var handledError = CreateHandledError(exception, context ?? new ErrorContext());

            // 触发错误事件
            OnErrorOccurred(handledError);

            // 如果是严重错误，触发严重错误事件
            if (handledError.Severity >= ErrorSeverity.Critical)
            {
                OnCriticalErrorOccurred(handledError);
            }

            // 异步记录日志
            await LogErrorAsync(handledError);

            return handledError;
        }

        /// <inheritdoc/>
        public async Task ShowErrorAsync(SharedCommon.HandledError handledError, bool showDialog = true)
        {
            if (handledError == null)
            {
                return;
            }

            if (showDialog && handledError.RequiresUserAcknowledgment)
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        // UltraThink优化：使用统一的ICustomDialogService，移除MessageBox依赖
                        if (_customDialogService != null)
                        {
                            switch (handledError.Severity)
                            {
                                case ErrorSeverity.Info:
                                    await _customDialogService.ShowInformationAsync(handledError.UserMessage, "提示");
                                    break;

                                case ErrorSeverity.Warning:
                                    await _customDialogService.ShowWarningAsync(handledError.UserMessage, "警告");
                                    break;

                                case ErrorSeverity.Error:
                                case ErrorSeverity.Critical:
                                case ErrorSeverity.Fatal:
                                    await ShowDetailedErrorAsync(handledError);
                                    break;
                            }
                        }
                        else
                        {
                            // 降级处理：如果没有对话框服务，使用系统诊断输出
                            Debug.WriteLine($"错误 ({handledError.Severity}): {handledError.UserMessage}");
                            Console.WriteLine($"错误 ({handledError.Severity}): {handledError.UserMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // 确保错误显示不会失败 - 最后手段才使用MessageBox
                        Debug.WriteLine($"显示错误对话框失败: {ex.Message}");
                        try
                        {
                            // 仅在极端情况下使用MessageBox
                            MessageBox.Show(handledError.UserMessage, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        catch
                        {
                            // 连MessageBox都失败了，只能输出到调试信息
                            Debug.WriteLine($"致命错误: {handledError.UserMessage}");
                        }
                    }
                });
            }
        }

        /// <inheritdoc/>
        public async Task LogErrorAsync(SharedCommon.HandledError handledError)
        {
            try
            {
                // 构建日志消息
                var logMessage = $"[{handledError.Severity}] {handledError.Category}: {handledError.UserMessage}";
                if (!string.IsNullOrEmpty(handledError.TechnicalDetails))
                {
                    logMessage += $"\nTechnical Details: {handledError.TechnicalDetails}";
                }

                // 输出到调试窗口
                Debug.WriteLine($"[ErrorHandling] {logMessage}");

                // 这里可以扩展为写入文件、发送到远程日志服务等
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"记录错误日志失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public string GetUserFriendlyMessage(Exception exception, string? defaultMessage = null)
        {
            if (exception == null)
            {
                return defaultMessage ?? "发生未知错误";
            }

            // 检查自定义异常类型
            return exception switch
            {
                BusinessException businessEx => businessEx.UserFriendlyMessage,
                ValidationException validationEx => validationEx.UserFriendlyMessage,
                NetworkException networkEx => networkEx.UserFriendlyMessage,
                AuthenticationException authEx => authEx.UserFriendlyMessage,
                _ => GetMappedMessage(exception) ?? defaultMessage ?? "发生未知错误"
            };
        }

        /// <inheritdoc/>
        public bool CanRetry(Exception exception)
        {
            return exception switch
            {
                NetworkException networkEx => true, // 网络异常通常可以重试
                BusinessException => false,
                ValidationException => false,
                AuthenticationException => false,
                ApiException apiEx => IsRetryableStatusCode(apiEx.StatusCode),
                HttpRequestException => true,
                TaskCanceledException => true,
                TimeoutException => true,
                _ => false
            };
        }

        /// <inheritdoc/>
        public ErrorCategory GetErrorCategory(Exception exception)
        {
            if (exception == null)
            {
                return ErrorCategory.Unknown;
            }

            // 检查自定义异常类型
            var category = exception switch
            {
                NetworkException => ErrorCategory.Network,
                AuthenticationException => ErrorCategory.Authentication,
                ValidationException => ErrorCategory.Validation,
                BusinessException => ErrorCategory.Business,
                ApiException => ErrorCategory.Network,
                HttpRequestException => ErrorCategory.Network,
                UnauthorizedAccessException => ErrorCategory.Authentication,
                ArgumentException => ErrorCategory.Validation,
                InvalidOperationException => ErrorCategory.Internal,
                NotSupportedException => ErrorCategory.Internal,
                OutOfMemoryException => ErrorCategory.Internal,
                _ => _categoryMapping.TryGetValue(exception.GetType(), out var mapped) ? mapped : ErrorCategory.Unknown
            };

            return category;
        }

        /// <inheritdoc/>
        public ErrorSeverity GetErrorSeverity(Exception exception)
        {
            if (exception == null)
            {
                return ErrorSeverity.Error;
            }

            return exception switch
            {
                BusinessException businessEx => ConvertSeverity(businessEx.Severity),
                NetworkException networkEx => ConvertSeverity(networkEx.Severity),
                AuthenticationException authEx => ConvertSeverity(authEx.Severity),
                ValidationException => ErrorSeverity.Warning,
                OutOfMemoryException => ErrorSeverity.Fatal,
                StackOverflowException => ErrorSeverity.Fatal,
                AccessViolationException => ErrorSeverity.Fatal,
                _ => _severityMapping.TryGetValue(exception.GetType(), out var severity) ? severity : ErrorSeverity.Error
            };
        }

        private ErrorSeverity ConvertSeverity(LYBT.Shared.Models.Contracts.Common.ErrorSeverity severity)
        {
            return severity switch
            {
                LYBT.Shared.Models.Contracts.Common.ErrorSeverity.Info => ErrorSeverity.Info,
                LYBT.Shared.Models.Contracts.Common.ErrorSeverity.Warning => ErrorSeverity.Warning,
                LYBT.Shared.Models.Contracts.Common.ErrorSeverity.Error => ErrorSeverity.Error,
                LYBT.Shared.Models.Contracts.Common.ErrorSeverity.Critical => ErrorSeverity.Critical,
                LYBT.Shared.Models.Contracts.Common.ErrorSeverity.Fatal => ErrorSeverity.Fatal,
                _ => ErrorSeverity.Error
            };
        }

        /// <inheritdoc/>
        public string[] GetSuggestedActions(Exception exception)
        {
            if (exception == null)
            {
                return new[] { "联系技术支持" };
            }

            // 检查映射表
            if (_actionMapping.TryGetValue(exception.GetType(), out var actions))
            {
                return actions;
            }

            // 基于异常类型返回建议
            return exception switch
            {
                NetworkException => new[] { "检查网络连接", "稍后重试", "联系网络管理员" },
                AuthenticationException => new[] { "重新登录", "检查用户名和密码", "联系管理员" },
                ValidationException => new[] { "检查输入数据", "修正错误信息" },
                BusinessException => new[] { "检查操作条件", "联系业务人员" },
                ApiException apiEx => GetApiExceptionActions(apiEx),
                HttpRequestException => new[] { "检查网络连接", "稍后重试" },
                TimeoutException => new[] { "检查网络连接", "增加超时时间", "稍后重试" },
                UnauthorizedAccessException => new[] { "检查权限设置", "联系管理员" },
                ArgumentException => new[] { "检查参数值", "联系开发人员" },
                _ => new[] { "稍后重试", "重启应用程序", "联系技术支持" }
            };
        }

        /// <inheritdoc/>
        public async Task<bool> ExecuteSafelyAsync(Func<Task> operation, ErrorContext? context = null, bool showErrorDialog = true)
        {
            try
            {
                await operation();
                return true;
            }
            catch (Exception ex)
            {
                var handledError = await HandleExceptionAsync(ex, context);
                if (showErrorDialog)
                {
                    await ShowErrorAsync(handledError);
                }

                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> operation, ErrorContext? context = null, bool showErrorDialog = true)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                var handledError = await HandleExceptionAsync(ex, context);
                if (showErrorDialog)
                {
                    await ShowErrorAsync(handledError);
                }

                return default;
            }
        }

        /// <inheritdoc/>
        public void RegisterGlobalExceptionHandlers()
        {
            // 注册WPF全局异常处理器
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

            // 注册Task未处理异常处理器
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // 注册应用程序域异常处理器
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        }

        #region 私有方法

        private SharedCommon.HandledError CreateHandledError(Exception exception, ErrorContext context)
        {
            var category = GetErrorCategory(exception);
            var severity = GetErrorSeverity(exception);
            var userMessage = GetUserFriendlyMessage(exception);
            var canRetry = CanRetry(exception);
            var suggestedActions = GetSuggestedActions(exception);

            var handledError = new SharedCommon.HandledError
            {
                Category = category,
                Severity = severity,
                UserMessage = userMessage,
                // 安全加固：脱敏技术细节，移除敏感路径和堆栈信息
                TechnicalDetails = SanitizeTechnicalDetails(exception),
                Exception = exception,
                CanRetry = canRetry,
                RequiresUserAcknowledgment = severity >= ErrorSeverity.Warning
            };

            foreach (var action in suggestedActions)
            {
                handledError.SuggestedActions.Add(action);
            }

            return handledError;
        }

        private string? GetMappedMessage(Exception exception)
        {
            var exceptionType = exception.GetType();
            if (_messageMapping.TryGetValue(exceptionType, out var messageFunc))
            {
                return messageFunc(exception);
            }

            return null;
        }

        private async Task ShowDetailedErrorAsync(SharedCommon.HandledError handledError)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    // 构建详细错误信息
                    var message = handledError.UserMessage;

                    // 如果有建议操作，添加到消息中
                    if (handledError.SuggestedActions.Count > 0)
                    {
                        message += "\n\n建议操作：";
                        for (int i = 0; i < handledError.SuggestedActions.Count; i++)
                        {
                            message += $"\n{i + 1}. {handledError.SuggestedActions[i]}";
                        }
                    }

                    // 如果可以重试，添加重试提示
                    if (handledError.CanRetry)
                    {
                        message += "\n\n此操作支持重试。";
                    }

                    // 添加错误ID和时间信息
                    message += $"\n\n错误ID: {handledError.Id}";
                    message += $"\n时间: {handledError.OccurredAt:yyyy-MM-dd HH:mm:ss}";

                    // UltraThink优化：使用统一的ICustomDialogService
                    if (_customDialogService != null)
                    {
                        await _customDialogService.ShowErrorAsync(message, $"错误 - {handledError.Category}");
                    }
                    else
                    {
                        // 降级处理：输出到调试信息
                        Debug.WriteLine($"详细错误信息: {message}");
                        Console.WriteLine($"详细错误信息: {message}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"显示详细错误对话框失败: {ex.Message}");
                    try
                    {
                        // 极端情况下使用MessageBox
                        MessageBox.Show(handledError.UserMessage, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch
                    {
                        Debug.WriteLine($"致命错误: {handledError.UserMessage}");
                    }
                }
            });
        }

        private string[] GetApiExceptionActions(ApiException apiException)
        {
            return apiException.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new[] { "重新登录", "检查认证状态" },
                HttpStatusCode.Forbidden => new[] { "联系管理员获取权限" },
                HttpStatusCode.NotFound => new[] { "检查请求地址", "联系技术支持" },
                HttpStatusCode.BadRequest => new[] { "检查请求参数", "联系开发人员" },
                HttpStatusCode.InternalServerError => new[] { "稍后重试", "联系技术支持" },
                HttpStatusCode.ServiceUnavailable => new[] { "稍后重试", "检查服务状态" },
                HttpStatusCode.GatewayTimeout => new[] { "检查网络连接", "稍后重试" },
                _ => new[] { "稍后重试", "联系技术支持" }
            };
        }

        private bool IsRetryableStatusCode(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.InternalServerError => true,
                HttpStatusCode.BadGateway => true,
                HttpStatusCode.ServiceUnavailable => true,
                HttpStatusCode.GatewayTimeout => true,
                HttpStatusCode.TooManyRequests => true,
                HttpStatusCode.RequestTimeout => true,
                _ => false
            };
        }

        private void OnErrorOccurred(SharedCommon.HandledError handledError)
        {
            try
            {
                ErrorOccurred?.Invoke(this, handledError);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"错误事件处理失败: {ex.Message}");
            }
        }

        private void OnCriticalErrorOccurred(SharedCommon.HandledError handledError)
        {
            try
            {
                CriticalErrorOccurred?.Invoke(this, handledError);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"严重错误事件处理失败: {ex.Message}");
            }
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            // Fire-and-forget pattern with proper exception isolation
            _ = Task.Run(async () =>
            {
                try
                {
                    var context = new ErrorContext
                    {
                        OperationName = "UI线程异常",
                        ModuleName = "WPF",
                        ViewName = "Global"
                    };

                    var handledError = await HandleExceptionAsync(e.Exception, context);
                    await ShowErrorAsync(handledError);
                }
                catch (Exception ex)
                {
                    // 最后一道防线：确保异常处理器本身不会崩溃
                    Debug.WriteLine($"异常处理器失败: {ex.Message}");

                    // UltraThink优化：首先尝试使用ICustomDialogService
                    try
                    {
                        if (_customDialogService != null)
                        {
                            await _customDialogService.ShowErrorAsync(
                                $"系统发生严重错误：{e.Exception.Message}",
                                "系统错误");
                        }
                        else
                        {
                            // 最后手段：使用MessageBox
                            MessageBox.Show($"系统发生严重错误：{e.Exception.Message}", "系统错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch
                    {
                        // 静默失败，避免无限递归
                        Debug.WriteLine($"致命全局异常: {e.Exception.Message}");
                    }
                }
            });
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();

            // Fire-and-forget pattern with proper exception isolation
            _ = Task.Run(async () =>
            {
                try
                {
                    var context = new ErrorContext
                    {
                        OperationName = "后台任务异常",
                        ModuleName = "Task",
                        ViewName = "Background"
                    };

                    foreach (var exception in e.Exception.InnerExceptions)
                    {
                        try
                        {
                            var handledError = await HandleExceptionAsync(exception, context);
                            await ShowErrorAsync(handledError);
                        }
                        catch (Exception handlingEx)
                        {
                            Debug.WriteLine($"处理后台任务异常失败: {handlingEx.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 最后一道防线：确保异常处理器本身不会崩溃
                    Debug.WriteLine($"后台任务异常处理器失败: {ex.Message}");
                }
            });
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Fire-and-forget pattern with proper exception isolation
            _ = Task.Run(async () =>
            {
                try
                {
                    if (e.ExceptionObject is Exception exception)
                    {
                        var context = new ErrorContext
                        {
                            OperationName = "应用程序域异常",
                            ModuleName = "AppDomain",
                            ViewName = "Global"
                        };

                        var handledError = await HandleExceptionAsync(exception, context);
                        await ShowErrorAsync(handledError);
                    }
                }
                catch (Exception ex)
                {
                    // 最后一道防线：确保异常处理器本身不会崩溃
                    Debug.WriteLine($"应用程序域异常处理器失败: {ex.Message}");

                    // UltraThink优化：首先尝试使用ICustomDialogService
                    try
                    {
                        if (e.ExceptionObject is Exception originalEx)
                        {
                            if (_customDialogService != null)
                            {
                                await _customDialogService.ShowErrorAsync(
                                    $"系统发生致命错误：{originalEx.Message}",
                                    "致命错误");
                            }
                            else
                            {
                                // 最后手段：使用MessageBox
                                MessageBox.Show($"系统发生致命错误：{originalEx.Message}", "致命错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    catch
                    {
                        // 静默失败，避免无限递归
                        Debug.WriteLine($"致命域异常: {e.ExceptionObject}");
                    }
                }
            });
        }

        /// <summary>
        /// 脱敏技术细节，移除可能包含的敏感信息
        /// </summary>
        private string SanitizeTechnicalDetails(Exception exception)
        {
            if (exception == null)
            {
                return "Unknown error";
            }

            try
            {
                var details = exception.ToString();

                // 移除完整路径，只保留文件名
                details = System.Text.RegularExpressions.Regex.Replace(
                    details,
                    @"[A-Z]:\\[^:\r\n]*\\([^\\:\r\n]+\.[a-z]+)",
                    "$1");

                // 移除用户目录路径
                details = System.Text.RegularExpressions.Regex.Replace(
                    details,
                    @"C:\\Users\\[^\\]+\\",
                    @"C:\Users\[REDACTED]\");

                // 移除可能的密码或令牌信息
                details = System.Text.RegularExpressions.Regex.Replace(
                    details,
                    @"(password|token|key|secret|authorization|bearer)[\s]*[:=][\s]*[^\s\r\n]+",
                    "$1=[REDACTED]",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // 移除连接字符串中的敏感信息
                details = System.Text.RegularExpressions.Regex.Replace(
                    details,
                    @"(Data Source|Initial Catalog|User ID|Password|Integrated Security)=[^;]+",
                    "$1=[REDACTED]",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // 限制堆栈跟踪深度（只保留前10行）
                var lines = details.Split('\n');
                if (lines.Length > 10)
                {
                    details = string.Join("\n", lines.Take(10)) + "\n... [堆栈跟踪已截断]";
                }

                return details;
            }
            catch
            {
                // 如果脱敏失败，返回基本信息
                return $"{exception.GetType().Name}: {exception.Message}";
            }
        }

        #endregion 私有方法

        #region 初始化映射表

        private Dictionary<Type, Func<Exception, string>> InitializeMessageMapping()
        {
            return new Dictionary<Type, Func<Exception, string>>
            {
                { typeof(HttpRequestException), ex => "网络连接失败，请检查网络设置" },
                { typeof(TaskCanceledException), ex => "操作已取消或超时" },
                { typeof(TimeoutException), ex => "操作超时，请稍后重试" },
                { typeof(UnauthorizedAccessException), ex => "权限不足，无法执行此操作" },
                { typeof(ArgumentNullException), ex => "参数错误：必需的参数为空" },
                { typeof(ArgumentException), ex => "参数错误：" + ex.Message },
                { typeof(InvalidOperationException), ex => "操作无效：" + ex.Message },
                { typeof(NotSupportedException), ex => "不支持的操作" },
                { typeof(OutOfMemoryException), ex => "内存不足，请关闭其他程序后重试" },
                { typeof(StackOverflowException), ex => "程序错误：堆栈溢出" },
                { typeof(DivideByZeroException), ex => "数学错误：除零操作" },
                { typeof(FormatException), ex => "数据格式错误" },
                { typeof(OverflowException), ex => "数值溢出错误" }
            };
        }

        private Dictionary<Type, ErrorCategory> InitializeCategoryMapping()
        {
            return new Dictionary<Type, ErrorCategory>
            {
                { typeof(HttpRequestException), ErrorCategory.Network },
                { typeof(TaskCanceledException), ErrorCategory.Internal },
                { typeof(TimeoutException), ErrorCategory.Network },
                { typeof(UnauthorizedAccessException), ErrorCategory.Authentication },
                { typeof(ArgumentException), ErrorCategory.Validation },
                { typeof(ArgumentNullException), ErrorCategory.Validation },
                { typeof(InvalidOperationException), ErrorCategory.Internal },
                { typeof(NotSupportedException), ErrorCategory.Internal },
                { typeof(OutOfMemoryException), ErrorCategory.Internal },
                { typeof(StackOverflowException), ErrorCategory.Internal },
                { typeof(FormatException), ErrorCategory.Validation },
                { typeof(OverflowException), ErrorCategory.Validation }
            };
        }

        private Dictionary<Type, ErrorSeverity> InitializeSeverityMapping()
        {
            return new Dictionary<Type, ErrorSeverity>
            {
                { typeof(HttpRequestException), ErrorSeverity.Error },
                { typeof(TaskCanceledException), ErrorSeverity.Warning },
                { typeof(TimeoutException), ErrorSeverity.Warning },
                { typeof(UnauthorizedAccessException), ErrorSeverity.Error },
                { typeof(ArgumentException), ErrorSeverity.Error },
                { typeof(ArgumentNullException), ErrorSeverity.Error },
                { typeof(InvalidOperationException), ErrorSeverity.Error },
                { typeof(NotSupportedException), ErrorSeverity.Error },
                { typeof(OutOfMemoryException), ErrorSeverity.Fatal },
                { typeof(StackOverflowException), ErrorSeverity.Fatal },
                { typeof(FormatException), ErrorSeverity.Error },
                { typeof(OverflowException), ErrorSeverity.Error }
            };
        }

        private Dictionary<Type, string[]> InitializeActionMapping()
        {
            return new Dictionary<Type, string[]>
            {
                { typeof(HttpRequestException), new[] { "检查网络连接", "稍后重试", "联系网络管理员" } },
                { typeof(TaskCanceledException), new[] { "重新执行操作", "检查操作超时设置" } },
                { typeof(TimeoutException), new[] { "检查网络连接", "增加超时时间", "稍后重试" } },
                { typeof(UnauthorizedAccessException), new[] { "检查权限设置", "重新登录", "联系管理员" } },
                { typeof(ArgumentException), new[] { "检查输入参数", "联系开发人员" } },
                { typeof(ArgumentNullException), new[] { "检查必需参数", "联系开发人员" } },
                { typeof(InvalidOperationException), new[] { "检查操作条件", "重启应用程序", "联系技术支持" } },
                { typeof(NotSupportedException), new[] { "使用其他功能", "更新应用程序", "联系技术支持" } },
                { typeof(OutOfMemoryException), new[] { "关闭其他程序", "重启应用程序", "增加系统内存" } },
                { typeof(StackOverflowException), new[] { "重启应用程序", "联系技术支持" } },
                { typeof(FormatException), new[] { "检查数据格式", "修正输入数据" } },
                { typeof(OverflowException), new[] { "检查数值范围", "修正输入数据" } }
            };
        }

        #endregion 初始化映射表
    }
}
