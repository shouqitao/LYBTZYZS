using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Infrastructure.Extensions;

/// <summary>
/// 错误处理扩展方法 - UltraThink企业级异常处理
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 为ViewModel和Service层提供便捷的错误处理扩展，统一异常处理流程
/// 支持同步和异步操作，提供参数验证和错误日志记录功能
/// 适配小型诊所业务需求，确保操作失败时的用户友好提示
/// </summary>
public static class ErrorHandlingExtensions
{

    /// <summary>
    /// 安全执行异步操作，自动处理异常
    /// 适用于需要返回结果的异步操作，集成企业级错误处理
    /// </summary>
    /// <typeparam name="T">操作返回值类型</typeparam>
    /// <param name="source">调用源对象（用于扩展方法）</param>
    /// <param name="operation">要执行的异步操作</param>
    /// <param name="operationName">操作名称，用于日志记录和错误提示</param>
    /// <returns>包含操作结果或错误信息的ServiceResult</returns>
    /// <exception cref="ArgumentNullException">当操作委托为 null 时抛出</exception>
    public static async Task<ServiceResult<T>> ExecuteWithErrorHandlingAsync<T>(
        this object source,
        Func<Task<T>> operation,
        string operationName = "操作")
    {
        ArgumentNullException.ThrowIfNull(operation, nameof(operation));
        return await StandardErrorHandler.Instance.HandleApiErrorAsync(operation, operationName).ConfigureAwait(false);
    }

    /// <summary>
    /// 安全执行异步操作，自动处理异常（无返回值版本）
    /// 适用于不需要返回结果的异步操作，如数据更新、删除等
    /// </summary>
    /// <param name="source">调用源对象（用于扩展方法）</param>
    /// <param name="operation">要执行的异步操作</param>
    /// <param name="operationName">操作名称，用于日志记录和错误提示</param>
    /// <returns>表示操作成功或失败的ServiceResult</returns>
    /// <exception cref="ArgumentNullException">当操作委托为 null 时抛出</exception>
    public static async Task<ServiceResult> ExecuteWithErrorHandlingAsync(
        this object source,
        Func<Task> operation,
        string operationName = "操作")
    {
        ArgumentNullException.ThrowIfNull(operation, nameof(operation));
        return await StandardErrorHandler.Instance.HandleApiErrorAsync(operation, operationName).ConfigureAwait(false);
    }

    /// <summary>
    /// 安全执行同步操作，自动处理异常
    /// 适用于需要返回结果的同步操作，如数据计算、验证等
    /// </summary>
    /// <typeparam name="T">操作返回值类型</typeparam>
    /// <param name="source">调用源对象（用于扩展方法）</param>
    /// <param name="operation">要执行的同步操作</param>
    /// <param name="operationName">操作名称，用于日志记录和错误提示</param>
    /// <returns>包含操作结果或错误信息的ServiceResult</returns>
    /// <exception cref="ArgumentNullException">当操作委托为 null 时抛出</exception>
    public static ServiceResult<T> ExecuteWithErrorHandling<T>(
        this object source,
        Func<T> operation,
        string operationName = "操作")
    {
        ArgumentNullException.ThrowIfNull(operation, nameof(operation));

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
    /// 安全执行同步操作，自动处理异常（无返回值版本）
    /// 适用于不需要返回结果的同步操作，如配置更新、状态设置等
    /// </summary>
    /// <param name="source">调用源对象（用于扩展方法）</param>
    /// <param name="operation">要执行的同步操作</param>
    /// <param name="operationName">操作名称，用于日志记录和错误提示</param>
    /// <returns>表示操作成功或失败的ServiceResult</returns>
    /// <exception cref="ArgumentNullException">当操作委托为 null 时抛出</exception>
    public static ServiceResult ExecuteWithErrorHandling(
        this object source,
        Action operation,
        string operationName = "操作")
    {
        ArgumentNullException.ThrowIfNull(operation, nameof(operation));

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
    /// 提供统一的错误消息显示，适配诊所用户体验需求
    /// </summary>
    /// <typeparam name="T">服务结果的数据类型</typeparam>
    /// <param name="result">要转换的服务结果</param>
    /// <param name="defaultSuccessMessage">成功时的默认消息</param>
    /// <returns>用户友好的显示消息</returns>
    /// <exception cref="ArgumentNullException">当结果对象为 null 时抛出</exception>
    public static string GetDisplayMessage<T>(this ServiceResult<T> result, string defaultSuccessMessage = "操作成功")
    {
        ArgumentNullException.ThrowIfNull(result, nameof(result));

        if (result.IsSuccess)
        {
            return defaultSuccessMessage;
        }

        return result.ErrorMessage ?? "操作失败，请稍后重试";
    }

    /// <summary>
    /// 转换ServiceResult为友好的用户消息（无泛型版本）
    /// 适用于不返回具体数据的操作结果显示
    /// </summary>
    /// <param name="result">要转换的服务结果</param>
    /// <param name="defaultSuccessMessage">成功时的默认消息</param>
    /// <returns>用户友好的显示消息</returns>
    /// <exception cref="ArgumentNullException">当结果对象为 null 时抛出</exception>
    public static string GetDisplayMessage(this ServiceResult result, string defaultSuccessMessage = "操作成功")
    {
        ArgumentNullException.ThrowIfNull(result, nameof(result));

        if (result.IsSuccess)
        {
            return defaultSuccessMessage;
        }

        return result.ErrorMessage ?? "操作失败，请稍后重试";
    }

    /// <summary>
    /// 检查ServiceResult并在失败时记录错误
    /// 用于操作链中的中间结果错误记录，不向用户显示
    /// </summary>
    /// <typeparam name="T">服务结果的数据类型</typeparam>
    /// <param name="result">要检查的服务结果</param>
    /// <param name="operationName">操作名称，用于日志记录</param>
    /// <returns>原始服务结果（支持链式调用）</returns>
    /// <exception cref="ArgumentNullException">当结果对象为 null 时抛出</exception>
    public static ServiceResult<T> LogOnFailure<T>(this ServiceResult<T> result, string operationName = "操作")
    {
        ArgumentNullException.ThrowIfNull(result, nameof(result));

        if (!result.IsSuccess && result.Exception != null)
        {
            StandardErrorHandler.Instance.HandleGeneralError(result.Exception, operationName, false);
        }

        return result;
    }

    /// <summary>
    /// 检查ServiceResult并在失败时记录错误（无泛型版本）
    /// 用于不返回数据的操作结果错误记录
    /// </summary>
    /// <param name="result">要检查的服务结果</param>
    /// <param name="operationName">操作名称，用于日志记录</param>
    /// <returns>原始服务结果（支持链式调用）</returns>
    /// <exception cref="ArgumentNullException">当结果对象为 null 时抛出</exception>
    public static ServiceResult LogOnFailure(this ServiceResult result, string operationName = "操作")
    {
        ArgumentNullException.ThrowIfNull(result, nameof(result));

        if (!result.IsSuccess && result.Exception != null)
        {
            StandardErrorHandler.Instance.HandleGeneralError(result.Exception, operationName, false);
        }

        return result;
    }

    /// <summary>
    /// 将ServiceResult转换为简单的成功/失败布尔值
    /// 适用于只需要判断操作成功性的场景
    /// </summary>
    /// <typeparam name="T">服务结果的数据类型</typeparam>
    /// <param name="result">要检查的服务结果</param>
    /// <returns>如果操作成功则返回 true，否则返回 false</returns>
    public static bool IsSuccessful<T>(this ServiceResult<T>? result)
    {
        return result?.IsSuccess == true;
    }

    /// <summary>
    /// 将ServiceResult转换为简单的成功/失败布尔值（无泛型版本）
    /// 适用于不返回数据的操作结果成功性判断
    /// </summary>
    /// <param name="result">要检查的服务结果</param>
    /// <returns>如果操作成功则返回 true，否则返回 false</returns>
    public static bool IsSuccessful(this ServiceResult? result)
    {
        return result?.IsSuccess == true;
    }

    /// <summary>
    /// 获取ServiceResult的数据，失败时返回默认值
    /// 提供安全的数据访问方式，避免异常处理复杂性
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="result">服务结果</param>
    /// <param name="defaultValue">失败时返回的默认值</param>
    /// <returns>操作成功时的数据或默认值</returns>
    /// <exception cref="ArgumentNullException">当结果对象为 null 时抛出</exception>
    public static T? GetDataOrDefault<T>(this ServiceResult<T> result, T? defaultValue = default)
    {
        ArgumentNullException.ThrowIfNull(result, nameof(result));
        return result.IsSuccess ? result.Data : defaultValue;
    }

    /// <summary>
    /// 验证参数，失败时返回验证错误ServiceResult
    /// 提供统一的参数验证机制，支持常见的验证场景
    /// </summary>
    /// <typeparam name="T">返回数据类型</typeparam>
    /// <param name="source">调用源对象（用于扩展方法）</param>
    /// <param name="parameter">要验证的参数</param>
    /// <param name="parameterName">参数名称，用于错误消息</param>
    /// <returns>验证成功的ServiceResult或包含验证错误的ServiceResult</returns>
    /// <exception cref="ArgumentException">当参数名称为空时抛出</exception>
    public static ServiceResult<T> ValidateParameter<T>(
        this object source,
        object? parameter,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName, nameof(parameterName));

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

    /// <summary>
    /// 批量验证多个参数
    /// 提供高效的多参数验证，一次性返回所有验证错误
    /// </summary>
    /// <typeparam name="T">返回数据类型</typeparam>
    /// <param name="source">调用源对象（用于扩展方法）</param>
    /// <param name="validations">参数验证字典，键为参数名，值为参数值</param>
    /// <returns>验证成功的ServiceResult或包含验证错误的ServiceResult</returns>
    /// <exception cref="ArgumentNullException">当验证字典为 null 时抛出</exception>
    public static ServiceResult<T> ValidateParameters<T>(
        this object source,
        Dictionary<string, object?> validations)
    {
        ArgumentNullException.ThrowIfNull(validations, nameof(validations));

        var errors = new List<string>();

        foreach (var (parameterName, parameter) in validations)
        {
            var validation = source.ValidateParameter<T>(parameter, parameterName);
            if (!validation.IsSuccess)
            {
                errors.Add(validation.ErrorMessage ?? $"{parameterName}验证失败");
            }
        }

        if (errors.Count > 0)
        {
            var combinedError = string.Join("; ", errors);
            return StandardErrorHandler.Instance.HandleValidationError<T>(combinedError);
        }

        return ServiceResult<T>.Success(default!);
    }
}
