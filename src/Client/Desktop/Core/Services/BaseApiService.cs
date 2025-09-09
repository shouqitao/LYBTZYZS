using System.Net;
using System.Net.Http;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace LYBT.Desktop.Core.Services
{

    /// <summary>
    /// API服务基类 - 提供重试、超时、熔断等策略
    /// </summary>
    public abstract class BaseApiService
    {
        protected readonly ILogger? Logger;
        protected readonly IAsyncPolicy<HttpResponseMessage> RetryPolicy;
        protected readonly IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy;
        protected readonly IAsyncPolicy<HttpResponseMessage> TimeoutPolicy;
        protected readonly IAsyncPolicy<HttpResponseMessage> CombinedPolicy;

        private const int DefaultRetryCount = 3;
        private const int DefaultTimeoutSeconds = 30;
        private const int CircuitBreakerHandledEventsAllowedBeforeBreaking = 3;
        private const int CircuitBreakerDurationOfBreakSeconds = 30;

        protected BaseApiService(ILogger? logger = null)
        {
            Logger = logger;

            // 配置重试策略（指数退避）
            RetryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => !msg.IsSuccessStatusCode && msg.StatusCode != HttpStatusCode.Unauthorized)
                .WaitAndRetryAsync(
                    DefaultRetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var statusCode = outcome.Result?.StatusCode;
                        Logger?.LogWarning(
                            "API调用失败，正在重试 {RetryCount}/{MaxRetries}。状态码: {StatusCode}。等待 {TimeSpan} 秒",
                            retryCount, DefaultRetryCount, statusCode, timespan.TotalSeconds);
                    });

            // 配置熔断策略
            CircuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    CircuitBreakerHandledEventsAllowedBeforeBreaking,
                    TimeSpan.FromSeconds(CircuitBreakerDurationOfBreakSeconds),
                    onBreak: (result, timespan) =>
                    {
                        Logger?.LogError("熔断器开启，将在 {TimeSpan} 秒后重试", timespan.TotalSeconds);
                    },
                    onReset: () =>
                    {
                        Logger?.LogInformation("熔断器重置，服务恢复");
                    });

            // 配置超时策略
            TimeoutPolicy = Policy
                .TimeoutAsync<HttpResponseMessage>(
                    DefaultTimeoutSeconds,
                    TimeoutStrategy.Pessimistic,
                    onTimeoutAsync: async (context, timespan, task) =>
                    {
                        Logger?.LogWarning("API调用超时，已等待 {TimeSpan} 秒", timespan.TotalSeconds);
                        await Task.CompletedTask;
                    });

            // 组合策略：超时 -> 重试 -> 熔断
            CombinedPolicy = Policy.WrapAsync(TimeoutPolicy, RetryPolicy, CircuitBreakerPolicy);
        }

        /// <summary>
        /// 执行API调用，应用所有策略
        /// </summary>
        protected async Task<T> ExecuteWithPoliciesAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 如果是HttpResponseMessage类型，使用HTTP策略
                if (typeof(T) == typeof(HttpResponseMessage))
                {
                    var httpOperation = operation as Func<CancellationToken, Task<HttpResponseMessage>>;
                    if (httpOperation != null)
                    {
                        var result = await CombinedPolicy.ExecuteAsync(httpOperation, cancellationToken);
                        return (T)(object)result;
                    }
                }

                // 对于非HTTP响应，直接执行
                return await operation(cancellationToken);
            }
            catch (BrokenCircuitException ex)
            {
                Logger?.LogError(ex, "服务不可用（熔断器开启）");
                throw new ServiceUnavailableException("服务暂时不可用，请稍后重试", ex);
            }
            catch (TimeoutRejectedException ex)
            {
                Logger?.LogError(ex, "请求超时");
                throw new ServiceTimeoutException("请求超时，请检查网络连接", ex);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "API调用失败");
                throw;
            }
        }

        /// <summary>
        /// 处理API响应
        /// </summary>
        protected async Task<ServiceResult<T>> HandleApiResponseAsync<T>(HttpResponseMessage response)
        {
            try
            {
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = System.Text.Json.JsonSerializer.Deserialize<T>(content, GetJsonOptions());
                    return ServiceResult<T>.Success(data!);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = ParseErrorMessage(errorContent, response.StatusCode);

                Logger?.LogWarning("API返回错误: {StatusCode} - {Error}", response.StatusCode, errorMessage);
                return ServiceResult<T>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "处理API响应失败");
                return ServiceResult<T>.Failure("处理服务器响应失败", ex);
            }
        }

        /// <summary>
        /// 解析错误消息
        /// </summary>
        private string ParseErrorMessage(string content, HttpStatusCode statusCode)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return GetDefaultErrorMessage(statusCode);
            }

            try
            {
                // 尝试解析JSON错误响应
                using var document = System.Text.Json.JsonDocument.Parse(content);
                if (document.RootElement.TryGetProperty("message", out var messageProp))
                {
                    return messageProp.GetString() ?? GetDefaultErrorMessage(statusCode);
                }

                if (document.RootElement.TryGetProperty("error", out var errorProp))
                {
                    return errorProp.GetString() ?? GetDefaultErrorMessage(statusCode);
                }
            }
            catch
            {
                // 如果不是JSON，返回原始内容或默认消息
                if (content.Length < 200)
                {
                    return content;
                }
            }

            return GetDefaultErrorMessage(statusCode);
        }

        /// <summary>
        /// 获取默认错误消息
        /// </summary>
        private string GetDefaultErrorMessage(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.Unauthorized => "未授权，请重新登录",
                HttpStatusCode.Forbidden => "没有权限访问该资源",
                HttpStatusCode.NotFound => "请求的资源不存在",
                HttpStatusCode.InternalServerError => "服务器内部错误",
                HttpStatusCode.ServiceUnavailable => "服务暂时不可用",
                HttpStatusCode.BadRequest => "请求参数错误",
                _ => $"请求失败 ({(int)statusCode})"
            };
        }

        /// <summary>
        /// 获取JSON序列化选项
        /// </summary>
        protected virtual System.Text.Json.JsonSerializerOptions GetJsonOptions()
        {
            return new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };
        }
    }

    /// <summary>
    /// 服务不可用异常
    /// </summary>
    public class ServiceUnavailableException : Exception
    {

        public ServiceUnavailableException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// 服务超时异常
    /// </summary>
    public class ServiceTimeoutException : Exception
    {

        public ServiceTimeoutException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
