using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Exceptions;

namespace LYBT.Shared.Models.Extensions;

/// <summary>
/// ServiceResult 扩展方法 - UltraThink统一异常体系集成
/// </summary>
public static class ServiceResultExtensions
{
    /// <summary>
    /// 从异常创建失败结果
    /// </summary>
    public static ServiceResult<T> FromException<T>(AppException exception)
    {
        return ServiceResult<T>.Failure(
            exception.UserMessage ?? exception.Message,
            exception
        );
    }

    /// <summary>
    /// 从异常创建失败结果（无数据）
    /// </summary>
    public static ServiceResult FromException(AppException exception)
    {
        return ServiceResult.Failure(
            exception.UserMessage ?? exception.Message,
            exception
        );
    }

    /// <summary>
    /// 转换为异常（如果失败）
    /// </summary>
    public static AppException? ToException<T>(this ServiceResult<T> result)
    {
        if (result.IsSuccess)
        {
            return null;
        }

        return result.Exception switch
        {
            AppException appEx => appEx,
            _ => new AppException(result.ErrorMessage ?? "操作失败", result.Exception!)
        };
    }

    /// <summary>
    /// 转换为异常（如果失败）
    /// </summary>
    public static AppException? ToException(this ServiceResult result)
    {
        if (result.IsSuccess)
        {
            return null;
        }

        return result.Exception switch
        {
            AppException appEx => appEx,
            _ => new AppException(result.ErrorMessage ?? "操作失败", result.Exception!)
        };
    }

    /// <summary>
    /// 如果失败则抛出异常
    /// </summary>
    public static T ThrowIfFailed<T>(this ServiceResult<T> result)
    {
        if (result.IsSuccess && result.Data != null)
        {
            return result.Data;
        }

        var exception = result.ToException();
        throw exception ?? new AppException(result.ErrorMessage ?? "操作失败");
    }

    /// <summary>
    /// 如果失败则抛出异常
    /// </summary>
    public static void ThrowIfFailed(this ServiceResult result)
    {
        if (result.IsSuccess)
        {
            return;
        }

        var exception = result.ToException();
        throw exception ?? new AppException(result.ErrorMessage ?? "操作失败");
    }

    /// <summary>
    /// 检查是否为特定类型的异常
    /// </summary>
    public static bool IsException<TException>(this ServiceResult result) where TException : AppException
    {
        return result.Exception is TException;
    }

    /// <summary>
    /// 检查是否为特定类型的异常
    /// </summary>
    public static bool IsException<TException, T>(this ServiceResult<T> result) where TException : AppException
    {
        return result.Exception is TException;
    }

    /// <summary>
    /// 获取特定类型的异常
    /// </summary>
    public static TException? GetException<TException>(this ServiceResult result) where TException : AppException
    {
        return result.Exception as TException;
    }

    /// <summary>
    /// 获取特定类型的异常
    /// </summary>
    public static TException? GetException<TException, T>(this ServiceResult<T> result) where TException : AppException
    {
        return result.Exception as TException;
    }
}
