using System.Net;
using System.Net.Http;
using Polly;
using Polly.Extensions.Http;

namespace LYBT.Desktop.Core.Extensions {

    /// <summary>
    /// Polly策略扩展方法
    /// </summary>
    public static class PollyExtensions {

        /// <summary>
        /// 创建标准的重试策略
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> CreateStandardRetryPolicy(int retryCount = 3) {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => !IsSuccessStatusCode(msg.StatusCode))
                .WaitAndRetryAsync(
                    retryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryNumber, context) => {
                        var statusCode = outcome.Result?.StatusCode;
                        Console.WriteLine($"重试 {retryNumber}/{retryCount}，状态码: {statusCode}，等待 {timespan.TotalSeconds} 秒");
                    });
        }

        /// <summary>
        /// 创建标准的熔断策略
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> CreateStandardCircuitBreakerPolicy(
            int handledEventsAllowedBeforeBreaking = 3,
            int durationOfBreakInSeconds = 30) {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking,
                    TimeSpan.FromSeconds(durationOfBreakInSeconds),
                    onBreak: (result, duration) => {
                        Console.WriteLine($"熔断器开启，持续 {duration.TotalSeconds} 秒");
                    },
                    onReset: () => {
                        Console.WriteLine("熔断器重置");
                    },
                    onHalfOpen: () => {
                        Console.WriteLine("熔断器半开状态");
                    });
        }

        /// <summary>
        /// 创建标准的超时策略
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> CreateStandardTimeoutPolicy(int timeoutSeconds = 30) {
            return Policy
                .TimeoutAsync<HttpResponseMessage>(
                    timeoutSeconds,
                    onTimeoutAsync: async (context, timespan, task) => {
                        Console.WriteLine($"请求超时，已等待 {timespan.TotalSeconds} 秒");
                        await System.Threading.Tasks.Task.CompletedTask;
                    });
        }

        /// <summary>
        /// 创建组合策略（超时 -> 重试 -> 熔断）
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> CreateCombinedPolicy(
            int retryCount = 3,
            int timeoutSeconds = 30,
            int circuitBreakerEvents = 3,
            int circuitBreakerDurationSeconds = 30) {
            var timeoutPolicy = CreateStandardTimeoutPolicy(timeoutSeconds);
            var retryPolicy = CreateStandardRetryPolicy(retryCount);
            var circuitBreakerPolicy = CreateStandardCircuitBreakerPolicy(circuitBreakerEvents, circuitBreakerDurationSeconds);

            // 组合策略：超时 -> 重试 -> 熔断
            return Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy);
        }

        /// <summary>
        /// 判断是否为成功状态码（包括业务逻辑）
        /// </summary>
        private static bool IsSuccessStatusCode(HttpStatusCode statusCode) {
            // 2xx 范围内的状态码被认为是成功的
            return (int)statusCode >= 200 && (int)statusCode < 300;
        }

        /// <summary>
        /// 创建带日志的重试策略
        /// </summary>
        public static IAsyncPolicy<T> CreateRetryPolicyWithLogging<T>(
            int retryCount,
            Action<string> logAction,
            Func<T, bool>? resultPredicate = null) {
            if (resultPredicate != null) {
                return Policy
                    .HandleResult(resultPredicate)
                    .WaitAndRetryAsync(
                        retryCount,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, timespan, retryNumber, context) => {
                            var message = outcome.Exception != null
                                ? $"重试 {retryNumber}/{retryCount}，异常: {outcome.Exception.Message}"
                                : $"重试 {retryNumber}/{retryCount}，结果不满足条件";
                            logAction(message);
                        });
            } else {
                return Policy<T>
                    .Handle<Exception>()
                    .WaitAndRetryAsync(
                        retryCount,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, timespan, retryNumber, context) => {
                            var message = outcome.Exception != null
                                ? $"重试 {retryNumber}/{retryCount}，异常: {outcome.Exception.Message}"
                                : $"重试 {retryNumber}/{retryCount}";
                            logAction(message);
                        });
            }
        }
    }
}
