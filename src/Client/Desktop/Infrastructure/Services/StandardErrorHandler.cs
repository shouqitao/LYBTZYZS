using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// 统一标准错误处理器 - 企业级错误处理解决方案
/// 采用UltraThink架构标准，使用C# 12主构造函数和现代化特性
/// 提供统一的异常处理、错误日志记录、用户友好提示等企业级功能
/// 遵循实用主义原则，避免过度工程化，适配小型诊所部署环境
/// </summary>
/// <param name="logger">日志记录器，用于记录错误信息和异常堆栈</param>
/// <exception cref="ArgumentNullException">当 <paramref name="logger"/> 为 null 时抛出</exception>
public class StandardErrorHandler(ILogger<StandardErrorHandler> logger) : IStandardErrorHandler {
    private readonly ILogger<StandardErrorHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private static readonly object _lockObject = new();
    private static StandardErrorHandler? _instance;

    /// <summary>
    /// 获取单例实例
    /// 适合小型应用场景，提供全局一致的错误处理行为
    /// 使用双重检查锁定模式确保线程安全
    /// </summary>
    /// <value>全局单例错误处理器实例</value>
    /// <remarks>
    /// 在生产环境中建议使用依赖注入而不是单例模式
    /// </remarks>
    public static StandardErrorHandler Instance {
        get {
            if (_instance == null) {
                lock (_lockObject) {
                    _instance ??= new StandardErrorHandler(
                        Microsoft.Extensions.Logging.Abstractions.NullLogger<StandardErrorHandler>.Instance);
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 统一处理ServiceResult异常
    /// 将原始异常转换为用户友好的错误消息并返回ServiceResult
    /// </summary>
    /// <typeparam name="T">服务结果的数据类型</typeparam>
    /// <param name="exception">需要处理的异常对象</param>
    /// <param name="operationName">发生异常的操作名称</param>
    /// <returns>包含错误信息的ServiceResult</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="exception"/> 为 null 时抛出</exception>
    public ServiceResult<T> HandleServiceError<T>(Exception exception, string operationName = "操作") {
        ArgumentNullException.ThrowIfNull(exception, nameof(exception));

        LogError(exception, operationName);
        var friendlyMessage = GetFriendlyErrorMessage(exception);
        return ServiceResult<T>.Failure(friendlyMessage, exception);
    }

    /// <summary>
    /// 统一处理ServiceResult异常（无泛型版本）
    /// 用于不返回具体数据只返回成功失败状态的操作
    /// </summary>
    /// <param name="exception">需要处理的异常对象</param>
    /// <param name="operationName">发生异常的操作名称</param>
    /// <returns>包含错误信息的ServiceResult</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="exception"/> 为 null 时抛出</exception>
    public ServiceResult HandleServiceError(Exception exception, string operationName = "操作") {
        ArgumentNullException.ThrowIfNull(exception, nameof(exception));

        LogError(exception, operationName);
        var friendlyMessage = GetFriendlyErrorMessage(exception);
        return ServiceResult.Failure(friendlyMessage, exception);
    }

    /// <summary>
    /// 统一处理API异步操作异常
    /// 包装API调用，自动处理异常并返回ServiceResult格式的结果
    /// </summary>
    /// <typeparam name="T">API返回的数据类型</typeparam>
    /// <param name="apiCall">要执行的API调用委托</param>
    /// <param name="operationName">操作名称，用于日志和错误报告</param>
    /// <returns>包含API调用结果或错误信息的ServiceResult</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="apiCall"/> 为 null 时抛出</exception>
    public async Task<ServiceResult<T>> HandleApiErrorAsync<T>(
        Func<Task<T>> apiCall,
        string operationName = "API调用") {
        ArgumentNullException.ThrowIfNull(apiCall, nameof(apiCall));

        try {
            var result = await apiCall().ConfigureAwait(false);
            return ServiceResult<T>.Success(result);
        } catch (Exception ex) {
            return HandleServiceError<T>(ex, operationName);
        }
    }

    /// <summary>
    /// 统一处理API异步操作异常（无返回值版本）
    /// 用于处理只需要知道成功失败状态的API操作
    /// </summary>
    /// <param name="apiCall">要执行的API调用委托</param>
    /// <param name="operationName">操作名称，用于日志和错误报告</param>
    /// <returns>表示API调用成功或失败的ServiceResult</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="apiCall"/> 为 null 时抛出</exception>
    public async Task<ServiceResult> HandleApiErrorAsync(
        Func<Task> apiCall,
        string operationName = "API调用") {
        ArgumentNullException.ThrowIfNull(apiCall, nameof(apiCall));

        try {
            await apiCall().ConfigureAwait(false);
            return ServiceResult.Success();
        } catch (Exception ex) {
            return HandleServiceError(ex, operationName);
        }
    }

    /// <summary>
    /// 统一处理业务逻辑异常
    /// 用于处理业务规则验证失败、业务状态冲突等业务层面的错误
    /// </summary>
    /// <typeparam name="T">服务结果的数据类型</typeparam>
    /// <param name="errorMessage">业务错误描述信息</param>
    /// <param name="exception">可选的引起业务错误的异常对象</param>
    /// <returns>包含业务错误信息的ServiceResult</returns>
    /// <exception cref="ArgumentException">当错误消息为空时抛出</exception>
    public ServiceResult<T> HandleBusinessError<T>(string errorMessage, Exception? exception = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage, nameof(errorMessage));

        _logger.LogWarning(exception, "业务逻辑错误: {ErrorMessage}", errorMessage);
        return ServiceResult<T>.Failure(errorMessage, exception);
    }

    /// <summary>
    /// 统一处理数据验证异常
    /// 用于处理参数验证、数据格式校验等输入验证错误
    /// </summary>
    /// <typeparam name="T">服务结果的数据类型</typeparam>
    /// <param name="validationMessage">验证失败的具体描述信息</param>
    /// <returns>包含验证错误信息的ServiceResult</returns>
    /// <exception cref="ArgumentException">当验证消息为空时抛出</exception>
    public ServiceResult<T> HandleValidationError<T>(string validationMessage) {
        ArgumentException.ThrowIfNullOrWhiteSpace(validationMessage, nameof(validationMessage));

        _logger.LogWarning("数据验证失败: {ValidationMessage}", validationMessage);
        return ServiceResult<T>.Failure($"验证失败: {validationMessage}");
    }

    /// <summary>
    /// 统一处理通用异常
    /// 适用于ViewModel、Service层等需要统一异常处理的场景
    /// </summary>
    /// <param name="exception">需要处理的异常对象</param>
    /// <param name="operationName">发生异常的操作名称</param>
    /// <param name="showToUser">是否向用户显示错误消息</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="exception"/> 为 null 时抛出</exception>
    public void HandleGeneralError(Exception exception, string operationName = "操作", bool showToUser = true) {
        ArgumentNullException.ThrowIfNull(exception, nameof(exception));

        LogError(exception, operationName);

        if (showToUser) {
            var friendlyMessage = GetFriendlyErrorMessage(exception);
            ShowErrorToUser(friendlyMessage);
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// 记录错误日志
    /// 使用结构化日志记录错误信息和异常堆栈
    /// </summary>
    /// <param name="exception">异常对象</param>
    /// <param name="operationName">操作名称</param>
    private void LogError(Exception exception, string operationName) {
        _logger.LogError(exception,
            "{OperationName}操作失败: {ErrorMessage} | 异常类型: {ExceptionType} | 堆栈跟踪: {StackTrace}",
            operationName,
            exception.Message,
            exception.GetType().Name,
            exception.StackTrace);
    }

    /// <summary>
    /// 获取用户友好的错误消息
    /// 将技术异常信息转换为用户可理解的中文描述
    /// </summary>
    /// <param name="exception">异常对象</param>
    /// <returns>用户友好的中文错误消息</returns>
    private static string GetFriendlyErrorMessage(Exception exception) {
        return exception switch {
            // 参数验证错误
            ArgumentNullException => "参数不能为空，请检查输入信息",
            ArgumentException => "参数格式不正确，请检查输入内容",

            // 操作状态错误
            InvalidOperationException => "当前操作无效，请检查操作条件后重试",

            // 权限相关错误
            UnauthorizedAccessException => "您没有权限执行此操作，请联系管理员",

            // 网络和时间相关错误
            TimeoutException => "操作超时，请检查网络状态后重试",
            System.Net.Http.HttpRequestException => "网络连接失败，请检查网络设置和服务器状态",
            TaskCanceledException => "操作已取消或超时，请稍后重试",

            // 系统环境错误
            PlatformNotSupportedException => "当前操作系统不支持此功能",
            NotSupportedException => "当前系统环境不支持此操作",

            // 文件系统错误
            System.IO.FileNotFoundException => "找不到指定的文件，请检查文件路径",
            System.IO.DirectoryNotFoundException => "找不到指定的目录，请检查目录路径",
            System.IO.IOException => "文件操作失败，请检查文件权限和磁盘空间",

            // 数据库相关错误
            System.Data.Common.DbException => "数据库操作失败，请稍后重试或联系管理员",

            // 其他错误
            _ => $"操作失败: {exception.Message}。如果问题持续，请联系技术支持。"
        };
    }

    /// <summary>
    /// 向用户显示错误消息
    /// 使用简单的消息框实现，适合小型诊所环境
    /// 生产环境中可集成更专业的通知系统或Toast组件
    /// </summary>
    /// <param name="message">要显示给用户的错误消息</param>
    private static void ShowErrorToUser(string message) {
        try {
            // TODO: 生产环境中应集成到专业的通知系统或使用自定义对话框
            System.Windows.MessageBox.Show(
                message,
                "系统提示 - 凌隐宝堂中医诊所",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        } catch (Exception ex) {
            // 如果连消息框都无法显示，记录到系统日志
            System.Diagnostics.Debug.WriteLine(
                "ShowErrorToUser失败: {0}, 原始消息: {1}", ex.Message, message);

            // 生产环境中应该有更完善的fallback机制
            // 例如写入日志文件、发送到错误跟踪服务等
        }
    }

    #endregion 私有辅助方法
}

/// <summary>
/// 标准错误处理器接口
/// </summary>
public interface IStandardErrorHandler {

    /// <summary>
    /// 处理ServiceResult异常
    /// </summary>
    ServiceResult<T> HandleServiceError<T>(Exception exception, string operationName = "操作");

    /// <summary>
    /// 处理ServiceResult异常 (无泛型版本)
    /// </summary>
    ServiceResult HandleServiceError(Exception exception, string operationName = "操作");

    /// <summary>
    /// 处理API异常
    /// </summary>
    Task<ServiceResult<T>> HandleApiErrorAsync<T>(Func<Task<T>> apiCall, string operationName = "API调用");

    /// <summary>
    /// 处理API异常 (无返回值版本)
    /// </summary>
    Task<ServiceResult> HandleApiErrorAsync(Func<Task> apiCall, string operationName = "API调用");

    /// <summary>
    /// 处理业务异常
    /// </summary>
    ServiceResult<T> HandleBusinessError<T>(string errorMessage, Exception? exception = null);

    /// <summary>
    /// 处理验证异常
    /// </summary>
    ServiceResult<T> HandleValidationError<T>(string validationMessage);

    /// <summary>
    /// 处理通用异常
    /// </summary>
    void HandleGeneralError(Exception exception, string operationName = "操作", bool showToUser = true);
}
