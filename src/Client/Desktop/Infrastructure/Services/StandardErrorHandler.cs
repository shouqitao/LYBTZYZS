using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;

namespace LYBT.Desktop.Infrastructure.Services;

/// <summary>
/// 统一标准错误处理器 - UltraThink简化版
/// 职责：提供统一的错误处理入口，简化复杂的错误处理架构
/// 原则：实用主义优于过度工程化
/// </summary>
public class StandardErrorHandler : IStandardErrorHandler
{
    private readonly ILogger<StandardErrorHandler> _logger;
    private static readonly object _lockObject = new();
    private static StandardErrorHandler? _instance;

    public StandardErrorHandler(ILogger<StandardErrorHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取单例实例 (简化版本，适合小型应用)
    /// </summary>
    public static StandardErrorHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lockObject)
                {
                    _instance ??= new StandardErrorHandler(
                        Microsoft.Extensions.Logging.Abstractions.NullLogger<StandardErrorHandler>.Instance);
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 统一处理ServiceResult异常
    /// </summary>
    public ServiceResult<T> HandleServiceError<T>(Exception exception, string operationName = "操作")
    {
        LogError(exception, operationName);
        var friendlyMessage = GetFriendlyErrorMessage(exception);
        return ServiceResult<T>.Failure(friendlyMessage, exception);
    }

    /// <summary>
    /// 统一处理ServiceResult异常 (无泛型版本)
    /// </summary>
    public ServiceResult HandleServiceError(Exception exception, string operationName = "操作")
    {
        LogError(exception, operationName);
        var friendlyMessage = GetFriendlyErrorMessage(exception);
        return ServiceResult.Failure(friendlyMessage, exception);
    }

    /// <summary>
    /// 统一处理API异常
    /// </summary>
    public async Task<ServiceResult<T>> HandleApiErrorAsync<T>(
        Func<Task<T>> apiCall, 
        string operationName = "API调用")
    {
        try
        {
            var result = await apiCall();
            return ServiceResult<T>.Success(result);
        }
        catch (Exception ex)
        {
            return HandleServiceError<T>(ex, operationName);
        }
    }

    /// <summary>
    /// 统一处理API异常 (无返回值版本)
    /// </summary>
    public async Task<ServiceResult> HandleApiErrorAsync(
        Func<Task> apiCall, 
        string operationName = "API调用")
    {
        try
        {
            await apiCall();
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            return HandleServiceError(ex, operationName);
        }
    }

    /// <summary>
    /// 统一处理业务异常
    /// </summary>
    public ServiceResult<T> HandleBusinessError<T>(string errorMessage, Exception? exception = null)
    {
        _logger.LogWarning(exception, "业务异常: {ErrorMessage}", errorMessage);
        return ServiceResult<T>.Failure(errorMessage, exception);
    }

    /// <summary>
    /// 统一处理验证异常
    /// </summary>
    public ServiceResult<T> HandleValidationError<T>(string validationMessage)
    {
        _logger.LogWarning("验证失败: {ValidationMessage}", validationMessage);
        return ServiceResult<T>.Failure($"验证失败: {validationMessage}");
    }

    /// <summary>
    /// 统一处理通用异常 (适用于ViewModel和Service层)
    /// </summary>
    public void HandleGeneralError(Exception exception, string operationName = "操作", bool showToUser = true)
    {
        LogError(exception, operationName);
        
        if (showToUser)
        {
            var friendlyMessage = GetFriendlyErrorMessage(exception);
            // TODO: 集成到用户通知系统
            ShowErrorToUser(friendlyMessage);
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// 记录错误日志
    /// </summary>
    private void LogError(Exception exception, string operationName)
    {
        _logger.LogError(exception, "{OperationName}失败: {ErrorMessage}", 
            operationName, exception.Message);
    }

    /// <summary>
    /// 获取用户友好的错误消息
    /// </summary>
    private static string GetFriendlyErrorMessage(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => "参数不能为空",
            ArgumentException => "参数格式不正确",
            InvalidOperationException => "当前操作无效，请检查操作条件",
            UnauthorizedAccessException => "您没有权限执行此操作",
            TimeoutException => "操作超时，请稍后重试",
            System.Net.Http.HttpRequestException => "网络连接失败，请检查网络设置",
            TaskCanceledException => "操作已取消或超时",
            NotSupportedException => "当前环境不支持此操作",
            System.IO.FileNotFoundException => "找不到指定的文件",
            System.IO.DirectoryNotFoundException => "找不到指定的目录",
            _ => $"操作失败: {exception.Message}"
        };
    }

    /// <summary>
    /// 向用户显示错误 (简化版本)
    /// </summary>
    private static void ShowErrorToUser(string message)
    {
        try
        {
            // 简单的消息框实现 (生产环境应该集成到专业的通知系统)
            System.Windows.MessageBox.Show(
                message, 
                "操作失败", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Error);
        }
        catch
        {
            // 如果连消息框都无法显示，则静默失败
            // 生产环境应该有更完善的fallback机制
        }
    }

    #endregion
}

/// <summary>
/// 标准错误处理器接口
/// </summary>
public interface IStandardErrorHandler
{
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