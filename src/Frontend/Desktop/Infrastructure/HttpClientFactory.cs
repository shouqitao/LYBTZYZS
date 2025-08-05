using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace LYBT.WPF.Client.Infrastructure
{
    /// <summary>
    /// HttpClient 工厂类，配置 Polly 重试策略
    /// </summary>
    public static class HttpClientFactory
    {
        /// <summary>
        /// 创建带有重试策略的 HttpClient
        /// </summary>
        public static HttpClient CreateWithRetryPolicy(HttpMessageHandler innerHandler)
        {
            var retryPolicy = GetRetryPolicy();
            var timeoutPolicy = GetTimeoutPolicy();
            var combinedPolicy = Policy.WrapAsync(retryPolicy, timeoutPolicy);

            var policyHandler = new PolicyHttpMessageHandler(combinedPolicy)
            {
                InnerHandler = innerHandler
            };

            return new HttpClient(policyHandler);
        }

        /// <summary>
        /// 获取重试策略
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError() // 处理 HttpRequestException, 5XX 和 408
                .OrResult(msg => !msg.IsSuccessStatusCode && msg.StatusCode != HttpStatusCode.Unauthorized) // 不重试 401
                .WaitAndRetryAsync(
                    3, // 重试 3 次
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 指数退避：2, 4, 8 秒
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var request = outcome.Result?.RequestMessage;
                        System.Diagnostics.Debug.WriteLine(
                            $"[Polly] 重试 {retryCount}/3: {request?.Method} {request?.RequestUri} " +
                            $"等待 {timespan.TotalSeconds} 秒后重试");
                    });
        }

        /// <summary>
        /// 获取超时策略
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(
                60, // 60 秒超时
                TimeoutStrategy.Pessimistic,
                onTimeoutAsync: (context, timespan, task) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[Polly] 请求超时: {timespan.TotalSeconds} 秒");
                    return Task.CompletedTask;
                });
        }
    }

    /// <summary>
    /// Polly 策略处理器
    /// </summary>
    public class PolicyHttpMessageHandler : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;

        public PolicyHttpMessageHandler(IAsyncPolicy<HttpResponseMessage> policy)
        {
            _policy = policy;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return await _policy.ExecuteAsync(async (ct) =>
                await base.SendAsync(request, ct), cancellationToken);
        }
    }
}