using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace LYBT.WPF.Client.Infrastructure
{
    /// <summary>
    /// 企业级 HttpClient 工厂类 - 统一创建和配置HttpClient实例
    /// 解决依赖注入中的重复代码和性能问题
    /// </summary>
    public static class HttpClientFactory
    {
        // 共享配置常量
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
        private static readonly int DefaultRetryCount = 3;
        
        /// <summary>
        /// 创建基础 HttpClient（无认证）
        /// </summary>
        /// <param name="baseUrl">API基础地址</param>
        /// <param name="timeout">超时时间，默认60秒</param>
        /// <returns>配置好的 HttpClient</returns>
        public static HttpClient CreateBasicClient(string baseUrl, TimeSpan? timeout = null)
        {
            var handler = CreateHttpClientHandler();
            var client = CreateWithRetryPolicy(handler);
            
            if (!string.IsNullOrEmpty(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl);
            }
            
            client.Timeout = timeout ?? DefaultTimeout;
            return client;
        }

        /// <summary>
        /// 创建带认证的 HttpClient - 需要在调用方设置认证处理器
        /// </summary>
        /// <param name="authHandler">认证处理器</param>
        /// <param name="baseUrl">API基础地址</param>
        /// <param name="timeout">超时时间，默认60秒</param>
        /// <returns>配置好的带认证 HttpClient</returns>
        public static HttpClient CreateAuthenticatedClient(DelegatingHandler authHandler, string baseUrl, TimeSpan? timeout = null)
        {
            if (authHandler == null)
                throw new ArgumentNullException(nameof(authHandler));

            var innerHandler = CreateHttpClientHandler();
            authHandler.InnerHandler = innerHandler;
            
            var client = CreateWithRetryPolicy(authHandler);
            
            if (!string.IsNullOrEmpty(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl);
            }
            
            client.Timeout = timeout ?? DefaultTimeout;
            return client;
        }

        /// <summary>
        /// 创建标准的 HttpClientHandler
        /// 开发环境忽略SSL证书验证，生产环境使用默认设置
        /// </summary>
        /// <returns>配置好的 HttpClientHandler</returns>
        private static HttpClientHandler CreateHttpClientHandler()
        {
#if DEBUG
            // 开发环境忽略SSL证书验证
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
#else
            // 生产环境使用默认设置
            return new HttpClientHandler();
#endif
        }

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