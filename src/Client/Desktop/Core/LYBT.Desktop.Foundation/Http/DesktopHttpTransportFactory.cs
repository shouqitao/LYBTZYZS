// ---------------------------------------------------------------------------
// DesktopHttpTransportFactory — 桌面私有 IHttpClientFactory（池化 + 弹性）
// ---------------------------------------------------------------------------
// 设计 05（docs/compose/specs/architecture-optimization-2026-09-16/design-05-httpclient-resilience.md）
//
// 关键约束：桌面主容器是 Prism.DryIoc（IContainerRegistry），不能直接 `services.AddHttpClient(...)`
// 注册进主容器。故此处构建一个**仅供传输层使用**的私有 ServiceCollection，由其产出
// IHttpClientFactory 实例，再由 Shell 层注册进 DryIoc。
//
// 具名客户端与 handler 链（首个 AddHttpMessageHandler 为最外层）：
//   RemoteApi    : Logging → Caching → Authorization → TokenRefresh → SocketsHttpHandler
//   LocalApi     : Caching → Authorization → SocketsHttpHandler（不含 TokenRefresh，见 05-dual-mode）
//   RefreshToken : SocketsHttpHandler（刷新请求自身不再套认证/刷新，避免递归）
//
// 弹性策略：仅对**幂等方法**（GET/HEAD/OPTIONS/PUT/DELETE）重试瞬时故障与 429；
// POST 不重试 —— 避免「服务端已成功但响应丢失」导致重复创建。
// ---------------------------------------------------------------------------

using System.Net;
using System.Net.Http;
using LYBT.Shared.Configuration.Options.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// 传输层自定义 <see cref="DelegatingHandler"/> 的提供者（由 Shell 层用 DryIoc 容器实现，
/// 使 Foundation 不依赖 Prism/DryIoc）。
/// </summary>
public interface IDesktopHttpHandlerProvider
{
    /// <summary>创建授权头处理器（注入 Bearer Token）。</summary>
    /// <returns>未设置 <see cref="DelegatingHandler.InnerHandler"/> 的处理器实例。</returns>
    DelegatingHandler CreateAuthorizationHandler();

    /// <summary>创建令牌刷新处理器（远程链专用）。</summary>
    /// <returns>未设置 <c>InnerHandler</c> 的处理器实例。</returns>
    DelegatingHandler CreateTokenRefreshHandler();

    /// <summary>创建请求/响应日志处理器。</summary>
    /// <returns>未设置 <c>InnerHandler</c> 的处理器实例。</returns>
    DelegatingHandler CreateLoggingHandler();

    /// <summary>创建 GET 响应缓存处理器。</summary>
    /// <returns>未设置 <c>InnerHandler</c> 的处理器实例。</returns>
    DelegatingHandler CreateCachingHandler();
}

/// <summary>
/// 桌面传输层工厂 —— 构建私有 <see cref="IHttpClientFactory"/>。
/// </summary>
public static class DesktopHttpTransportFactory
{
    /// <summary>远程具名客户端。</summary>
    public const string RemoteClientName = "RemoteApi";

    /// <summary>本地具名客户端。</summary>
    public const string LocalClientName = "LocalApi";

    /// <summary>令牌刷新具名客户端。</summary>
    public const string RefreshClientName = "RefreshToken";

    /// <summary>handler 链重建周期（DNS/连接池刷新）。</summary>
    private static readonly TimeSpan HandlerLifetime = TimeSpan.FromMinutes(2);

    /// <summary>刷新客户端固定超时（沿用既有 30s 约定）。</summary>
    private static readonly TimeSpan RefreshTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 构建私有服务容器并返回其 <see cref="IServiceProvider"/>。
    /// 调用方负责在退出时释放（<see cref="IDisposable.Dispose"/>）。
    /// </summary>
    /// <param name="options">API 客户端选项（超时 / 忽略 SSL）。</param>
    /// <param name="handlers">自定义处理器提供者。</param>
    /// <returns>已构建的服务提供者（含 <see cref="IHttpClientFactory"/>）。</returns>
    /// <exception cref="ArgumentNullException">任一参数为 null。</exception>
    public static ServiceProvider Build(ApiClientOptions options, IDesktopHttpHandlerProvider handlers)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(handlers);

        var services = new ServiceCollection();
        services.AddLogging();

        // 远程：完整链（日志 → 缓存 → 授权 → 刷新）
        services
            .AddHttpClient(RemoteClientName, client => client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds))
            .ConfigurePrimaryHttpMessageHandler(() => CreatePrimaryHandler(options))
            .SetHandlerLifetime(HandlerLifetime)
            .AddHttpMessageHandler(_ => handlers.CreateLoggingHandler())
            .AddHttpMessageHandler(_ => handlers.CreateCachingHandler())
            .AddHttpMessageHandler(_ => handlers.CreateAuthorizationHandler())
            .AddHttpMessageHandler(_ => handlers.CreateTokenRefreshHandler())
            .AddPolicyHandler((_, request) => CreatePolicyFor(request.Method));

        // 本地：无 TokenRefresh（本地续期由 Shell TokenLifecycleService 显式驱动）
        services
            .AddHttpClient(LocalClientName, client => client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds))
            .ConfigurePrimaryHttpMessageHandler(() => CreatePrimaryHandler(options))
            .SetHandlerLifetime(HandlerLifetime)
            .AddHttpMessageHandler(_ => handlers.CreateCachingHandler())
            .AddHttpMessageHandler(_ => handlers.CreateAuthorizationHandler())
            .AddPolicyHandler((_, request) => CreatePolicyFor(request.Method));

        // 刷新：裸链（刷新请求自身不再套授权/刷新）
        services
            .AddHttpClient(RefreshClientName, client => client.Timeout = RefreshTimeout)
            .ConfigurePrimaryHttpMessageHandler(() => CreatePrimaryHandler(options))
            .SetHandlerLifetime(HandlerLifetime);

        return services.BuildServiceProvider();
    }

    private static HttpMessageHandler CreatePrimaryHandler(ApiClientOptions options)
    {
        var handler = new SocketsHttpHandler
        {
            // 连接池中连接的存活上限：服务端/DNS 变更后自动重建
            PooledConnectionLifetime = HandlerLifetime,
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
        };

        if (options.IgnoreSslErrors)
            handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;

        return handler;
    }

    /// <summary>
    /// 幂等方法才重试瞬时故障（5xx/408/429/连接失败）；非幂等（POST/PATCH）返回空策略。
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> CreatePolicyFor(HttpMethod method)
    {
        if (!IsIdempotent(method))
            return Policy.NoOpAsync<HttpResponseMessage>();

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)));
    }

    private static bool IsIdempotent(HttpMethod method) =>
        method == HttpMethod.Get
        || method == HttpMethod.Head
        || method == HttpMethod.Options
        || method == HttpMethod.Put
        || method == HttpMethod.Delete;
}
