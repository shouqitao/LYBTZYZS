using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace LYBT.Desktop.Services.Http
{
    /// <summary>
    /// 重试策略扩展 - Polly集成
    /// </summary>
    public static class RetryPolicyExtensions
    {
        /// <summary>
        /// 创建HTTP重试策略
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> CreateHttpRetryPolicy(
            ILogger? logger = null,
            int retryCount = 3,
            TimeSpan? baseDelay = null)
        {
            var delay = baseDelay ?? TimeSpan.FromSeconds(1);

            return Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && ShouldRetry(r.StatusCode))
                .WaitAndRetryAsync(
                    retryCount: retryCount,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1) * delay.TotalSeconds),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var statusCode = outcome.Result?.StatusCode.ToString() ?? "N/A";
                        var exception = outcome.Exception?.Message ?? "无异常";
                        logger?.LogWarning(
                            "API调用重试 {RetryCount}/{MaxRetries}, 状态码: {StatusCode}, 异常: {Exception}, 延迟: {Delay}ms",
                            retryCount, 3, statusCode, exception, timespan.TotalMilliseconds);
                    });
        }

        /// <summary>
        /// 创建超时策略
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> CreateTimeoutPolicy(
            TimeSpan timeout,
            ILogger? logger = null)
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(timeout);
        }

        /// <summary>
        /// 创建熔断器策略
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy(
            ILogger? logger = null,
            int failureThreshold = 5,
            TimeSpan durationOfBreak = default)
        {
            if (durationOfBreak == default)
                durationOfBreak = TimeSpan.FromMinutes(1);

            return Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && ShouldRetry(r.StatusCode))
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: failureThreshold,
                    durationOfBreak: durationOfBreak,
                    onBreak: (exception, duration) =>
                    {
                        logger?.LogError("API熔断器开启, 持续时间: {Duration}ms, 异常: {Exception}",
                            duration.TotalMilliseconds, exception.Exception?.Message ?? "无异常");
                    },
                    onReset: () =>
                    {
                        logger?.LogInformation("API熔断器重置");
                    },
                    onHalfOpen: () =>
                    {
                        logger?.LogInformation("API熔断器半开状态");
                    });
        }

        /// <summary>
        /// 创建组合策略 (重试 + 超时 + 熔断)
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> CreateCompositePolicy(
            ILogger? logger = null,
            int retryCount = 3,
            TimeSpan? baseDelay = null,
            TimeSpan? timeout = null,
            int circuitBreakerThreshold = 5,
            TimeSpan? circuitBreakerDuration = null)
        {
            var timeoutPolicy = CreateTimeoutPolicy(timeout ?? TimeSpan.FromSeconds(30), logger);
            var retryPolicy = CreateHttpRetryPolicy(logger, retryCount, baseDelay);
            var circuitBreakerPolicy = CreateCircuitBreakerPolicy(logger, circuitBreakerThreshold, circuitBreakerDuration ?? TimeSpan.FromMinutes(1));

            // 执行顺序: 重试 -> 熔断器 -> 超时
            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
        }

        /// <summary>
        /// 判断是否应该重试
        /// </summary>
        private static bool ShouldRetry(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.InternalServerError => true,
                HttpStatusCode.BadGateway => true,
                HttpStatusCode.ServiceUnavailable => true,
                HttpStatusCode.GatewayTimeout => true,
                HttpStatusCode.RequestTimeout => true,
                HttpStatusCode.TooManyRequests => true,
                _ => false
            };
        }
    }

    /// <summary>
    /// 重试策略配置选项
    /// </summary>
    public class RetryPolicyOptions
    {
        /// <summary>
        /// 重试次数，默认3次
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// 基础延迟时间，默认1秒
        /// </summary>
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// 超时时间，默认30秒
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// 熔断器失败阈值，默认5次
        /// </summary>
        public int CircuitBreakerThreshold { get; set; } = 5;

        /// <summary>
        /// 熔断器开启时间，默认1分钟
        /// </summary>
        public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// 是否启用重试，默认启用
        /// </summary>
        public bool EnableRetry { get; set; } = true;

        /// <summary>
        /// 是否启用熔断器，默认启用
        /// </summary>
        public bool EnableCircuitBreaker { get; set; } = true;

        /// <summary>
        /// 是否启用超时，默认启用
        /// </summary>
        public bool EnableTimeout { get; set; } = true;
    }
}