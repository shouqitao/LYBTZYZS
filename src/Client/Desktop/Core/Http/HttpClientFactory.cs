using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace LYBT.Desktop.Core.Http
{
    /// <summary>
    /// HttpClient工厂 - 管理HTTP客户端生命周期
    /// </summary>
    public interface IHttpClientFactory
    {
        HttpClient CreateClient(string name = "default");
        void ConfigureClient(string name, Action<HttpClient> configure);
        void AddMessageHandler(string name, DelegatingHandler handler);
    }

    /// <summary>
    /// HttpClient工厂实现
    /// </summary>
    public class HttpClientFactory : IHttpClientFactory, IDisposable
    {
        private readonly ConcurrentDictionary<string, HttpClientConfiguration> _configurations = new();
        private readonly ConcurrentDictionary<string, HttpClient> _clients = new();
        private readonly ILogger<HttpClientFactory>? _logger;
        private readonly HttpMessageHandler _primaryHandler;

        public HttpClientFactory(ILogger<HttpClientFactory>? logger = null)
        {
            _logger = logger;
            _primaryHandler = CreatePrimaryHandler();
            
            // 配置默认客户端
            ConfigureDefaultClient();
        }

        /// <summary>
        /// 创建HTTP客户端
        /// </summary>
        public HttpClient CreateClient(string name = "default")
        {
            return _clients.GetOrAdd(name, key =>
            {
                var config = _configurations.GetOrDefault(key) ?? new HttpClientConfiguration();
                var handler = CreateHandlerPipeline(config);
                var client = new HttpClient(handler, false);
                
                // 应用配置
                config.ClientConfiguration?.Invoke(client);
                
                _logger?.LogDebug($"创建HttpClient: {name}");
                return client;
            });
        }

        /// <summary>
        /// 配置客户端
        /// </summary>
        public void ConfigureClient(string name, Action<HttpClient> configure)
        {
            var config = _configurations.GetOrAdd(name, _ => new HttpClientConfiguration());
            config.ClientConfiguration = configure;
            
            // 如果客户端已存在，重新配置
            if (_clients.TryGetValue(name, out var client))
            {
                configure(client);
            }
        }

        /// <summary>
        /// 添加消息处理器
        /// </summary>
        public void AddMessageHandler(string name, DelegatingHandler handler)
        {
            var config = _configurations.GetOrAdd(name, _ => new HttpClientConfiguration());
            config.AdditionalHandlers.Add(handler);
        }

        /// <summary>
        /// 创建处理器管道
        /// </summary>
        private HttpMessageHandler CreateHandlerPipeline(HttpClientConfiguration config)
        {
            HttpMessageHandler handler = _primaryHandler;
            
            // 添加默认处理器
            handler = new AuthenticationHandler(_logger) { InnerHandler = handler };
            handler = new LoggingHandler(_logger) { InnerHandler = handler };
            handler = new RetryPolicyHandler(_logger) { InnerHandler = handler };
            handler = new RequestIdHandler { InnerHandler = handler };
            
            // 添加自定义处理器
            foreach (var additionalHandler in config.AdditionalHandlers.AsEnumerable().Reverse())
            {
                additionalHandler.InnerHandler = handler;
                handler = additionalHandler;
            }
            
            return handler;
        }

        /// <summary>
        /// 创建主处理器
        /// </summary>
        private HttpMessageHandler CreatePrimaryHandler()
        {
            return new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = false,
                AllowAutoRedirect = false,
                MaxConnectionsPerServer = 10
            };
        }

        /// <summary>
        /// 配置默认客户端
        /// </summary>
        private void ConfigureDefaultClient()
        {
            ConfigureClient("default", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent", "LYBT-WPF-Client/1.0");
            });
        }

        public void Dispose()
        {
            foreach (var client in _clients.Values)
            {
                client?.Dispose();
            }
            _clients.Clear();
        }

        /// <summary>
        /// HTTP客户端配置
        /// </summary>
        private class HttpClientConfiguration
        {
            public Action<HttpClient>? ClientConfiguration { get; set; }
            public List<DelegatingHandler> AdditionalHandlers { get; } = new();
        }
    }

    /// <summary>
    /// 认证处理器
    /// </summary>
    public class AuthenticationHandler : DelegatingHandler
    {
        private readonly ILogger? _logger;
        private static string? _bearerToken;
        private static readonly SemaphoreSlim _tokenRefreshSemaphore = new(1, 1);

        public AuthenticationHandler(ILogger? logger)
        {
            _logger = logger;
        }

        public static void SetBearerToken(string? token)
        {
            _bearerToken = token;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // 添加认证头
            if (!string.IsNullOrEmpty(_bearerToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // 处理401响应
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger?.LogWarning("收到401响应，需要刷新令牌");
                
                // 这里可以触发令牌刷新逻辑
                await RefreshTokenAsync(cancellationToken);
                
                // 重试请求
                if (!string.IsNullOrEmpty(_bearerToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
                    response = await base.SendAsync(request, cancellationToken);
                }
            }

            return response;
        }

        private async Task RefreshTokenAsync(CancellationToken cancellationToken)
        {
            await _tokenRefreshSemaphore.WaitAsync(cancellationToken);
            try
            {
                // 实现令牌刷新逻辑
                _logger?.LogInformation("刷新认证令牌");
                await Task.Delay(100, cancellationToken); // 模拟刷新
            }
            finally
            {
                _tokenRefreshSemaphore.Release();
            }
        }
    }

    /// <summary>
    /// 日志处理器
    /// </summary>
    public class LoggingHandler : DelegatingHandler
    {
        private readonly ILogger? _logger;

        public LoggingHandler(ILogger? logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var requestId = request.Headers.TryGetValues("X-Request-Id", out var values) 
                ? values.FirstOrDefault() 
                : "unknown";

            _logger?.LogDebug($"[{requestId}] HTTP请求: {request.Method} {request.RequestUri}");
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                var response = await base.SendAsync(request, cancellationToken);
                sw.Stop();
                
                _logger?.LogDebug($"[{requestId}] HTTP响应: {(int)response.StatusCode} {response.StatusCode} ({sw.ElapsedMilliseconds}ms)");
                
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger?.LogWarning($"[{requestId}] 错误响应内容: {content}");
                }
                
                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger?.LogError(ex, $"[{requestId}] HTTP请求失败 ({sw.ElapsedMilliseconds}ms)");
                throw;
            }
        }
    }

    /// <summary>
    /// 重试策略处理器
    /// </summary>
    public class RetryPolicyHandler : DelegatingHandler
    {
        private readonly ILogger? _logger;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

        public RetryPolicyHandler(ILogger? logger)
        {
            _logger = logger;
            _retryPolicy = CreateRetryPolicy();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
                await base.SendAsync(request, cancellationToken));
        }

        private IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
        {
            // 重试策略
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.ServiceUnavailable)
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var requestId = context.TryGetValue("RequestId", out var id) 
                            ? id 
                            : "unknown";
                        _logger?.LogWarning($"[{requestId}] 重试 {retryCount} 次，等待 {timespan}");
                    });

            // 熔断策略
            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    5,
                    TimeSpan.FromSeconds(30),
                    onBreak: (result, duration) =>
                    {
                        _logger?.LogError($"熔断器打开，持续时间: {duration}");
                    },
                    onReset: () =>
                    {
                        _logger?.LogInformation("熔断器重置");
                    });

            // 超时策略
            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(10);

            // 组合策略
            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
        }
    }

    /// <summary>
    /// 请求ID处理器
    /// </summary>
    public class RequestIdHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // 添加请求ID
            if (!request.Headers.Contains("X-Request-Id"))
            {
                request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString());
            }
            
            // 添加时间戳
            request.Headers.Add("X-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            
            return base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// 扩展方法
    /// </summary>
    public static class ConcurrentDictionaryExtensions
    {
        public static TValue? GetOrDefault<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> dictionary,
            TKey key) where TKey : notnull where TValue : class
        {
            return dictionary.TryGetValue(key, out var value) ? value : null;
        }
    }
}