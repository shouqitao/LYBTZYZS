using System.Net;
using System.Net.Http;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace LYBT.Desktop.Infrastructure;

/// <summary>
/// 企业级 HttpClient 工厂类 - 统一创建和配置HttpClient实例
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 提供统一的HTTP客户端创建、配置和企业级重试策略
/// 解决依赖注入中的重复代码和性能问题，适配小型诊所部署环境
/// </summary>
public static class HttpClientFactory {

    // 共享配置常量 - 企业级超时配置
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// 创建基础 HttpClient（无认证）
    /// 配置企业级重试策略和超时处理，适合内网部署环境
    /// </summary>
    /// <param name="baseUrl">API基础地址，必须是有效的绝对URL</param>
    /// <param name="timeout">超时时间，默认60秒，适合小型诊所网络环境</param>
    /// <returns>配置好的 HttpClient 实例</returns>
    /// <exception cref="ArgumentException">当基地址格式无效时抛出</exception>
    /// <exception cref="UriFormatException">当URL格式错误时抛出</exception>
    public static HttpClient CreateBasicClient(string baseUrl, TimeSpan? timeout = null) {
        var handler = CreateHttpClientHandler();
        var client = CreateWithRetryPolicy(handler);

        if (!string.IsNullOrWhiteSpace(baseUrl)) {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)) {
                throw new ArgumentException($"基地址格式无效: {baseUrl}", nameof(baseUrl));
            }

            client.BaseAddress = uri;
        }

        client.Timeout = timeout ?? DefaultTimeout;
        ConfigureStandardHeaders(client);
        return client;
    }

    /// <summary>
    /// 创建带认证的 HttpClient
    /// 集成JWT Bearer Token认证处理器，支持企业级安全访问
    /// </summary>
    /// <param name="authHandler">认证处理器，用于JWT令牌管理</param>
    /// <param name="baseUrl">API基础地址，必须是有效的绝对URL</param>
    /// <param name="timeout">超时时间，默认60秒</param>
    /// <returns>配置好的带认证 HttpClient 实例</returns>
    /// <exception cref="ArgumentNullException">当认证处理器为 null 时抛出</exception>
    /// <exception cref="ArgumentException">当基地址格式无效时抛出</exception>
    public static HttpClient CreateAuthenticatedClient(DelegatingHandler authHandler, string baseUrl, TimeSpan? timeout = null) {
        ArgumentNullException.ThrowIfNull(authHandler, nameof(authHandler));

        var innerHandler = CreateHttpClientHandler();
        authHandler.InnerHandler = innerHandler;

        var client = CreateWithRetryPolicy(authHandler);

        if (!string.IsNullOrWhiteSpace(baseUrl)) {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)) {
                throw new ArgumentException($"基地址格式无效: {baseUrl}", nameof(baseUrl));
            }

            client.BaseAddress = uri;
        }

        client.Timeout = timeout ?? DefaultTimeout;
        ConfigureStandardHeaders(client);
        return client;
    }

    /// <summary>
    /// 配置标准的HTTP请求头
    /// 设置统一的客户端标识和内容类型，便于服务器端识别和调试
    /// </summary>
    /// <param name="client">要配置的HttpClient实例</param>
    /// <exception cref="ArgumentNullException">当客户端为 null 时抛出</exception>
    private static void ConfigureStandardHeaders(HttpClient client) {
        ArgumentNullException.ThrowIfNull(client, nameof(client));

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("User-Agent", "LYBT-Desktop-Client/2.1.0");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("X-Client-Version", "2.1.0");
        client.DefaultRequestHeaders.Add("X-Client-Type", "Desktop");
        client.DefaultRequestHeaders.Add("X-Client-Platform", Environment.OSVersion.ToString());
    }

    /// <summary>
    /// 创建标准的 HttpClientHandler
    /// 开发环境忽略SSL证书验证，生产环境使用默认设置
    /// 适配小型诊所内网部署和开发调试需求
    /// </summary>
    /// <returns>配置好的 HttpClientHandler 实例</returns>
    private static HttpClientHandler CreateHttpClientHandler() {
#if DEBUG
        // 开发环境忽略SSL证书验证，便于本地调试
        return new HttpClientHandler {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
#else
        // 生产环境使用默认SSL验证设置
        return new HttpClientHandler();
#endif
    }

    /// <summary>
    /// 创建带有企业级重试策略的 HttpClient
    /// 集成Polly重试和超时策略，提升网络调用的可靠性
    /// </summary>
    /// <param name="innerHandler">内部消息处理器</param>
    /// <returns>配置了重试策略的HttpClient实例</returns>
    /// <exception cref="ArgumentNullException">当内部处理器为 null 时抛出</exception>
    public static HttpClient CreateWithRetryPolicy(HttpMessageHandler innerHandler) {
        ArgumentNullException.ThrowIfNull(innerHandler, nameof(innerHandler));

        var retryPolicy = GetRetryPolicy();
        var timeoutPolicy = GetTimeoutPolicy();
        var combinedPolicy = Policy.WrapAsync(retryPolicy, timeoutPolicy);

        var policyHandler = new PolicyHttpMessageHandler(combinedPolicy) {
            InnerHandler = innerHandler
        };

        return new HttpClient(policyHandler);
    }

    /// <summary>
    /// 获取企业级重试策略
    /// 适合小型诊所网络环境，避免临时网络问题影响业务操作
    /// </summary>
    /// <returns>配置好的重试策略</returns>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // 处理 HttpRequestException, 5XX 和 408
            .OrResult(msg => !msg.IsSuccessStatusCode && msg.StatusCode != HttpStatusCode.Unauthorized) // 不重试 401 认证失败
            .WaitAndRetryAsync(
                3, // 重试 3 次，适合小型网络环境
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 指数退避：2, 4, 8 秒
                onRetry: (outcome, timespan, retryCount, context) => {
                    var request = outcome.Result?.RequestMessage;
                    System.Diagnostics.Debug.WriteLine(
                        "[Polly] 重试 {RetryCount}/3: {Method} {RequestUri} 等待 {DelaySeconds} 秒后重试",
                        retryCount, request?.Method, request?.RequestUri, timespan.TotalSeconds);
                });
    }

    /// <summary>
    /// 获取超时策略
    /// 防止长时间等待影响用户体验，适合诊所业务操作需求
    /// </summary>
    /// <returns>配置好的超时策略</returns>
    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy() {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            60, // 60 秒超时，适合诊所内网环境
            TimeoutStrategy.Pessimistic,
            onTimeoutAsync: (context, timespan, task) => {
                System.Diagnostics.Debug.WriteLine("[Polly] 请求超时: {TimeoutSeconds} 秒", timespan.TotalSeconds);
                return Task.CompletedTask;
            });
    }
}

/// <summary>
/// Polly 策略处理器 - 企业级HTTP消息处理
/// 采用UltraThink架构标准，使用C# 12主构造函数和现代化特性
/// 集成重试、超时、熔断等企业级弹性策略
/// </summary>
/// <param name="policy">要应用的Polly策略</param>
/// <exception cref="ArgumentNullException">当策略为 null 时抛出</exception>
public class PolicyHttpMessageHandler(IAsyncPolicy<HttpResponseMessage> policy) : DelegatingHandler {
    private readonly IAsyncPolicy<HttpResponseMessage> _policy = policy ?? throw new ArgumentNullException(nameof(policy));

    /// <summary>
    /// 发送HTTP请求并应用策略
    /// 自动处理重试、超时等弹性策略，提升网络调用可靠性
    /// </summary>
    /// <param name="request">HTTP请求消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>HTTP响应消息</returns>
    /// <exception cref="ArgumentNullException">当请求消息为 null 时抛出</exception>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        return await _policy.ExecuteAsync(async (ct) =>
            await base.SendAsync(request, ct).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    }
}
