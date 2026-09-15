// ---------------------------------------------------------------------------
// LocalApiHttpClientFactory — IHttpClientFactory for LocalWebAPI mode
// ---------------------------------------------------------------------------
// Local 模式（内嵌 LocalWebAPI，localhost 回环）的传输层工厂。
//
// 与 Remote 模式一致，本地端点同样受 [Authorize(Policy=...)] 保护，请求必须携带
// Bearer Token —— handler 链为 HttpClientHandler → AuthorizationMessageHandler。
//
// CreateClient 每次返回新的 HttpClient 包装同一个共享 handler（disposeHandler: false）：
// 调用方 using 释放包装不会破坏后续请求，符合 IHttpClientFactory 约定。
//
// 不含 TokenRefreshHandler：本地令牌续期由 Shell 侧 TokenLifecycleService 显式驱动
// （POST /api/v1/auth/refresh），而 TokenRefreshHandler 固定打向 ApiClientOptions.BaseUrl
// （远程地址），挂到本地链上会在本地会话里误发远程刷新请求。
// ---------------------------------------------------------------------------

using System.Net.Http;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// Local（内嵌 LocalWebAPI）模式的 <see cref="IHttpClientFactory"/> 实现。
/// </summary>
/// <remarks>
/// <para>生命周期：handler 链由本实例持有并在 <see cref="Dispose"/> 时释放；
/// <see cref="CreateClient"/> 返回的 <see cref="HttpClient"/> 是轻量包装（不拥有 handler）。</para>
/// </remarks>
public sealed class LocalApiHttpClientFactory : IHttpClientFactory, IDisposable
{
    private readonly Uri _baseAddress;
    private readonly TimeSpan _timeout;
    private readonly HttpMessageHandler _handler;
    private bool _disposed;

    /// <summary>
    /// 初始化 <see cref="LocalApiHttpClientFactory"/> 类的新实例。
    /// </summary>
    /// <param name="baseAddress">LocalWebAPI 基地址（回环地址）。</param>
    /// <param name="timeout">单次请求超时。</param>
    /// <param name="tokenStorage">令牌存储——供 <see cref="AuthorizationMessageHandler"/> 注入 Bearer Token。</param>
    /// <param name="logger">授权处理器日志器。</param>
    /// <exception cref="ArgumentNullException">任一参数为 null。</exception>
    /// <exception cref="ArgumentException"><paramref name="baseAddress"/> 非绝对地址。</exception>
    public LocalApiHttpClientFactory(
        Uri baseAddress,
        TimeSpan timeout,
        ITokenStorageService tokenStorage,
        ILogger<AuthorizationMessageHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);
        ArgumentNullException.ThrowIfNull(tokenStorage);
        ArgumentNullException.ThrowIfNull(logger);

        _baseAddress = baseAddress;
        _timeout = timeout;
        _handler = new AuthorizationMessageHandler(tokenStorage, logger)
        {
            InnerHandler = new HttpClientHandler(),
        };
    }

    /// <inheritdoc />
    public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false)
    {
        BaseAddress = _baseAddress,
        Timeout = _timeout,
    };

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _handler.Dispose();
    }
}
