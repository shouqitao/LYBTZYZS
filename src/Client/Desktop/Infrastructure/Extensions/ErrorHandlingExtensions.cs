using System;
using System.Threading.Tasks;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;

namespace LYBT.Desktop.Infrastructure.Extensions;

/// <summary>
/// 错误处理扩展方法 - UltraThink简化版
/// 职责：为ViewModel和Service层提供便捷的错误处理扩展
/// </summary>
public static class ErrorHandlingExtensions
{
    /// <summary>
    /// 安全执行异步操作，自动处理异常
    /// </summary>
    public static async Task<ServiceResult<T>> ExecuteWithErrorHandlingAsync<T>(
        this object source,
        Func<Task<T>> operation,
        string operationName = "操作")
    {
        return await StandardErrorHandler.Instance.HandleApiErrorAsync(operation, operationName);
    }

    /// <summary>
    /// 安全执行异步操作，自动处理异常 (无返回值版本)
    /// </summary>
    public static async Task<ServiceResult> ExecuteWithErrorHandlingAsync(
        this object source,
        Func<Task> operation,
        string operationName = "操作")
    {
        return await StandardErrorHandler.Instance.HandleApiErrorAsync(operation, operationName);
    }

    /// <summary>
    /// 安全执行同步操作，自动处理异常
    /// </summary>
    public static ServiceResult<T> ExecuteWithErrorHandling<T>(
        this object source,
        Func<T> operation,
        string operationName = "操作")
    {
        try
        {
            var result = operation();
            return ServiceResult<T>.Success(result);
        }
        catch (Exception ex)
        {
            return StandardErrorHandler.Instance.HandleServiceError<T>(ex, operationName);
        }
    }

    /// <summary>
    /// 安全执行同步操作，自动处理异常 (无返回值版本)
    /// </summary>
    public static ServiceResult ExecuteWithErrorHandling(
        this object source,
        Action operation,
        string operationName = "操作")
    {
        try
        {
            operation();
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            return StandardErrorHandler.Instance.HandleServiceError(ex, operationName);
        }
    }

    /// <summary>
    /// 转换ServiceResult为友好的用户消息
    /// </summary>
    public static string GetDisplayMessage<T>(this ServiceResult<T> result, string defaultSuccessMessage = "操作成功")
    {
        if (result.IsSuccess)
        {
            return defaultSuccessMessage;
        }
        
        return result.ErrorMessage ?? "操作失败，请稍后重试";
    }

    /// <summary>
    /// 转换ServiceResult为友好的用户消息 (无泛型版本)
    /// </summary>
    public static string GetDisplayMessage(this ServiceResult result, string defaultSuccessMessage = "操作成功")
    {
        if (result.IsSuccess)
        {
            return defaultSuccessMessage;
        }
        
        return result.ErrorMessage ?? "操作失败，请稍后重试";
    }

    /// <summary>
    /// 检查ServiceResult并在失败时记录错误
    /// </summary>
    public static ServiceResult<T> LogOnFailure<T>(this ServiceResult<T> result, string operationName = "操作")
    {
        if (!result.IsSuccess && result.Exception != null)
        {
            StandardErrorHandler.Instance.HandleGeneralError(result.Exception, operationName, false);
        }
        
        return result;
    }

    /// <summary>
    /// 检查ServiceResult并在失败时记录错误 (无泛型版本)
    /// </summary>
    public static ServiceResult LogOnFailure(this ServiceResult result, string operationName = "操作")
    {
        if (!result.IsSuccess && result.Exception != null)
        {
            StandardErrorHandler.Instance.HandleGeneralError(result.Exception, operationName, false);
        }
        
        return result;
    }

    /// <summary>
    /// 将ServiceResult转换为简单的成功/失败布尔值
    /// </summary>
    public static bool IsSuccessful<T>(this ServiceResult<T> result)
    {
        return result?.IsSuccess == true;
    }

    /// <summary>
    /// 将ServiceResult转换为简单的成功/失败布尔值 (无泛型版本)
    /// </summary>
    public static bool IsSuccessful(this ServiceResult result)
    {
        return result?.IsSuccess == true;
    }

    /// <summary>
    /// 获取ServiceResult的数据，失败时返回默认值
    /// </summary>
    public static T? GetDataOrDefault<T>(this ServiceResult<T> result, T? defaultValue = default)
    {
        return result.IsSuccess ? result.Data : defaultValue;
    }

    /// <summary>
    /// 验证参数，失败时返回验证错误ServiceResult
    /// </summary>
    public static ServiceResult<T> ValidateParameter<T>(
        this object source,
        object? parameter,
        string parameterName)
    {
        if (parameter == null)
        {
            return StandardErrorHandler.Instance.HandleValidationError<T>($"{parameterName}不能为空");
        }

        if (parameter is string str && string.IsNullOrWhiteSpace(str))
        {
            return StandardErrorHandler.Instance.HandleValidationError<T>($"{parameterName}不能为空白");
        }

        if (parameter is Guid guid && guid == Guid.Empty)
        {
            return StandardErrorHandler.Instance.HandleValidationError<T>($"{parameterName}不能为空ID");
        }

        return ServiceResult<T>.Success(default!);
    }
}