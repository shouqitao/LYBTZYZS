using System;
using System.Net;
using System.Threading.Tasks;
using Refit;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Models;
using LYBT.Desktop.Core.Exceptions;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// API错误处理辅助类 - 增强版错误处理和重试机制
    /// </summary>
    public static class ApiErrorHandler
    {
        private static ILogger? _logger;
        
        /// <summary>
        /// 设置日志记录器
        /// </summary>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 处理Refit API响应，转换为前端ServiceResult格式（增强版）
        /// </summary>
        public static async Task<ServiceResult<T>> HandleApiResponseAsync<T>(
            Func<Task<Refit.ApiResponse<T>>> apiCall, 
            string? operationName = null,
            int maxRetries = 0)
        {
            var attempt = 0;
            Exception? lastException = null;

            while (attempt <= maxRetries)
            {
                try
                {
                    var response = await apiCall();

                    if (response.IsSuccessStatusCode)
                    {
                        if (attempt > 0)
                        {
                            _logger?.LogInformation("API调用在第{Attempt}次重试后成功: {Operation}", attempt, operationName ?? "未知操作");
                        }
                        return ServiceResult<T>.Success(response.Content!);
                    }
                    else
                    {
                        // 尝试从响应内容获取错误信息
                        var errorMessage = await ExtractErrorMessageAsync(response);
                        var errorInfo = new ApiErrorInfo
                        {
                            StatusCode = response.StatusCode,
                            ErrorMessage = errorMessage,
                            OperationName = operationName,
                            AttemptNumber = attempt + 1
                        };

                        LogApiError(errorInfo);

                        // 检查是否应该重试
                        if (ShouldRetry(response.StatusCode, attempt, maxRetries))
                        {
                            attempt++;
                            await Task.Delay(GetRetryDelay(attempt));
                            continue;
                        }

                        return ServiceResult<T>.Failure(errorMessage, CreateEnhancedException(errorInfo));
                    }
                }
                catch (Refit.ApiException ex)
                {
                    lastException = ex;
                    var errorInfo = new ApiErrorInfo
                    {
                        StatusCode = ex.StatusCode,
                        ErrorMessage = await GetApiExceptionMessageAsync(ex),
                        OperationName = operationName,
                        AttemptNumber = attempt + 1,
                        Exception = ex
                    };

                    LogApiError(errorInfo);

                    // 检查是否应该重试
                    if (ShouldRetry(ex.StatusCode, attempt, maxRetries))
                    {
                        attempt++;
                        await Task.Delay(GetRetryDelay(attempt));
                        continue;
                    }

                    return await HandleApiExceptionAsync<T>(ex, operationName);
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    lastException = ex;
                    var errorInfo = new ApiErrorInfo
                    {
                        StatusCode = HttpStatusCode.RequestTimeout,
                        ErrorMessage = "请求超时",
                        OperationName = operationName,
                        AttemptNumber = attempt + 1,
                        Exception = ex
                    };

                    LogApiError(errorInfo);

                    // 超时错误可以重试
                    if (attempt < maxRetries)
                    {
                        attempt++;
                        await Task.Delay(GetRetryDelay(attempt));
                        continue;
                    }

                    return ServiceResult<T>.Failure("请求超时，请稍后重试", ex);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    var errorInfo = new ApiErrorInfo
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        ErrorMessage = $"请求失败: {ex.Message}",
                        OperationName = operationName,
                        AttemptNumber = attempt + 1,
                        Exception = ex
                    };

                    LogApiError(errorInfo);

                    // 一般异常不重试
                    return ServiceResult<T>.Failure($"请求失败: {ex.Message}", ex);
                }
            }

            // 所有重试都失败了
            return ServiceResult<T>.Failure(
                $"操作失败，已重试{maxRetries}次: {lastException?.Message ?? "未知错误"}", 
                lastException);
        }

        /// <summary>
        /// 处理Refit API异常（增强版）
        /// </summary>
        private static async Task<ServiceResult<T>> HandleApiExceptionAsync<T>(Refit.ApiException ex, string? operationName = null)
        {
            var errorMessage = "请求失败";
            var statusCode = (int?)ex.StatusCode ?? 500;

            if (ex.HasContent)
            {
                try
                {
                    // 尝试解析为ProblemDetails
                    var problemDetails = await ex.GetContentAsAsync<LYBT.Desktop.Core.Models.ProblemDetails>();
                    if (problemDetails != null)
                    {
                        errorMessage = problemDetails.Detail ?? problemDetails.Title ?? ex.Message;
                    }
                }
                catch
                {
                    // 如果不是ProblemDetails格式，尝试获取原始内容
                    try
                    {
                        var content = await ex.GetContentAsAsync<string>();
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            errorMessage = content;
                        }
                    }
                    catch
                    {
                        errorMessage = ex.Message;
                    }
                }
            }
            else
            {
                // 根据HTTP状态码提供更友好的错误消息
                errorMessage = ex.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "未授权，请重新登录",
                    System.Net.HttpStatusCode.Forbidden => "无权限访问此资源",
                    System.Net.HttpStatusCode.NotFound => "请求的资源不存在",
                    System.Net.HttpStatusCode.BadRequest => "请求参数错误",
                    System.Net.HttpStatusCode.InternalServerError => "服务器内部错误",
                    System.Net.HttpStatusCode.ServiceUnavailable => "服务暂时不可用",
                    System.Net.HttpStatusCode.GatewayTimeout => "请求超时",
                    _ => ex.Message
                };
            }

            return ServiceResult<T>.Failure(errorMessage, ex);
        }

        /// <summary>
        /// 从响应中提取错误消息
        /// </summary>
        private static Task<string> ExtractErrorMessageAsync<T>(Refit.ApiResponse<T> response)
        {
            if (response.Error != null)
            {
                try
                {
                    // 尝试解析错误内容为ProblemDetails
                    var content = response.Error.Content;
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var problemDetails = System.Text.Json.JsonSerializer.Deserialize<LYBT.Desktop.Core.Models.ProblemDetails>(content, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (problemDetails != null)
                        {
                            return Task.FromResult(problemDetails.Detail ?? problemDetails.Title ?? "请求失败");
                        }
                    }
                }
                catch
                {
                    // 如果解析失败，返回原始错误消息
                }
            }

            // 根据状态码返回默认消息
            var errorMessage = response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => "未授权，请重新登录",
                System.Net.HttpStatusCode.Forbidden => "无权限访问此资源",
                System.Net.HttpStatusCode.NotFound => "请求的资源不存在",
                System.Net.HttpStatusCode.BadRequest => "请求参数错误",
                System.Net.HttpStatusCode.InternalServerError => "服务器内部错误",
                System.Net.HttpStatusCode.ServiceUnavailable => "服务暂时不可用",
                System.Net.HttpStatusCode.GatewayTimeout => "请求超时",
                _ => "请求失败"
            };

            return Task.FromResult(errorMessage);
        }

        /// <summary>
        /// 简化的错误处理方法，用于不需要返回数据的操作
        /// </summary>
        public static async Task<ServiceResult> HandleApiCallAsync(Func<Task<Refit.ApiResponse<object>>> apiCall)
        {
            var result = await HandleApiResponseAsync(apiCall);
            if (result.IsSuccess)
            {
                return ServiceResult.Success();
            }
            else
            {
                return ServiceResult.Failure(result.ErrorMessage ?? "操作失败", result.Exception);
            }
        }

        /// <summary>
        /// 简化的错误处理方法，用于不需要返回数据的操作（泛型版本）
        /// </summary>
        public static async Task<ServiceResult> HandleApiCallAsync<T>(Func<Task<Refit.ApiResponse<T>>> apiCall, string? operationName = null, int maxRetries = 0)
        {
            var result = await HandleApiResponseAsync(apiCall, operationName, maxRetries);
            if (result.IsSuccess)
            {
                return ServiceResult.Success();
            }
            else
            {
                return ServiceResult.Failure(result.ErrorMessage ?? "操作失败", result.Exception);
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 判断是否应该重试
        /// </summary>
        private static bool ShouldRetry(HttpStatusCode? statusCode, int currentAttempt, int maxRetries)
        {
            if (currentAttempt >= maxRetries)
                return false;

            return statusCode switch
            {
                HttpStatusCode.InternalServerError => true,      // 500
                HttpStatusCode.BadGateway => true,               // 502
                HttpStatusCode.ServiceUnavailable => true,       // 503
                HttpStatusCode.GatewayTimeout => true,           // 504
                HttpStatusCode.RequestTimeout => true,           // 408
                HttpStatusCode.TooManyRequests => true,          // 429
                _ => false
            };
        }

        /// <summary>
        /// 获取重试延迟时间（指数退避）
        /// </summary>
        private static TimeSpan GetRetryDelay(int attemptNumber)
        {
            var baseDelay = TimeSpan.FromMilliseconds(1000); // 1秒基础延迟
            var exponentialDelay = TimeSpan.FromMilliseconds(Math.Pow(2, attemptNumber) * 1000);
            var maxDelay = TimeSpan.FromSeconds(30); // 最大30秒

            return exponentialDelay > maxDelay ? maxDelay : exponentialDelay;
        }

        /// <summary>
        /// 记录API错误
        /// </summary>
        private static void LogApiError(ApiErrorInfo errorInfo)
        {
            if (_logger == null) return;

            var logLevel = GetLogLevel(errorInfo.StatusCode);
            _logger.Log(logLevel, 
                "API错误 - 操作: {Operation}, 状态码: {StatusCode}, 尝试: {Attempt}, 错误: {Error}",
                errorInfo.OperationName ?? "未知",
                errorInfo.StatusCode,
                errorInfo.AttemptNumber,
                errorInfo.ErrorMessage);

            if (errorInfo.Exception != null)
            {
                _logger.LogDebug(errorInfo.Exception, "API异常详情");
            }
        }

        /// <summary>
        /// 根据状态码获取日志级别
        /// </summary>
        private static LogLevel GetLogLevel(HttpStatusCode? statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.BadRequest => LogLevel.Warning,           // 400
                HttpStatusCode.Unauthorized => LogLevel.Warning,         // 401
                HttpStatusCode.Forbidden => LogLevel.Warning,            // 403
                HttpStatusCode.NotFound => LogLevel.Information,         // 404
                HttpStatusCode.InternalServerError => LogLevel.Error,    // 500
                HttpStatusCode.BadGateway => LogLevel.Error,             // 502
                HttpStatusCode.ServiceUnavailable => LogLevel.Warning,   // 503
                HttpStatusCode.GatewayTimeout => LogLevel.Warning,       // 504
                _ => LogLevel.Warning
            };
        }

        /// <summary>
        /// 从API异常获取错误消息
        /// </summary>
        private static async Task<string> GetApiExceptionMessageAsync(ApiException ex)
        {
            var errorMessage = "请求失败";

            if (ex.HasContent)
            {
                try
                {
                    // 尝试解析为ProblemDetails
                    var problemDetails = await ex.GetContentAsAsync<LYBT.Desktop.Core.Models.ProblemDetails>();
                    if (problemDetails != null)
                    {
                        errorMessage = problemDetails.Detail ?? problemDetails.Title ?? ex.Message;
                    }
                }
                catch
                {
                    // 如果不是ProblemDetails格式，尝试获取原始内容
                    try
                    {
                        var content = await ex.GetContentAsAsync<string>();
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            errorMessage = content;
                        }
                    }
                    catch
                    {
                        errorMessage = ex.Message;
                    }
                }
            }
            else
            {
                errorMessage = GetFriendlyErrorMessage(ex.StatusCode);
            }

            return errorMessage;
        }

        /// <summary>
        /// 获取友好的错误消息
        /// </summary>
        private static string GetFriendlyErrorMessage(HttpStatusCode? statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.Unauthorized => "未授权，请重新登录",
                HttpStatusCode.Forbidden => "无权限访问此资源",
                HttpStatusCode.NotFound => "请求的资源不存在",
                HttpStatusCode.BadRequest => "请求参数错误",
                HttpStatusCode.InternalServerError => "服务器内部错误",
                HttpStatusCode.ServiceUnavailable => "服务暂时不可用",
                HttpStatusCode.GatewayTimeout => "请求超时",
                HttpStatusCode.TooManyRequests => "请求过于频繁，请稍后重试",
                _ => "请求失败"
            };
        }

        /// <summary>
        /// 创建增强的异常信息
        /// </summary>
        private static Exception CreateEnhancedException(ApiErrorInfo errorInfo)
        {
            var message = $"API调用失败 - {errorInfo.ErrorMessage} (状态码: {errorInfo.StatusCode}, 操作: {errorInfo.OperationName ?? "未知"})";
            return new ApiCallException(message, errorInfo.Exception)
            {
                StatusCode = errorInfo.StatusCode,
                OperationName = errorInfo.OperationName,
                AttemptNumber = errorInfo.AttemptNumber
            };
        }

        #endregion
    }

    /// <summary>
    /// API错误信息
    /// </summary>
    public class ApiErrorInfo
    {
        public HttpStatusCode? StatusCode { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string? OperationName { get; set; }
        public int AttemptNumber { get; set; }
        public Exception? Exception { get; set; }
    }
}