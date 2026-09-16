// ---------------------------------------------------------------------------
// DryIocHttpHandlerProvider — 用 DryIoc 容器装配传输层自定义 handler
// ---------------------------------------------------------------------------
// DesktopHttpTransportFactory（Foundation，不依赖 DryIoc）通过本提供者获得自定义 handler；
// 本类位于 Shell 层，可访问 Prism.DryIoc 的 IContainer。
//
// 两阶段初始化：工厂需要 handler 提供者才能构建，而 TokenRefreshHandler 需要工厂产出的
// IHttpClientFactory —— 故先创建提供者、构建工厂、再 AttachFactory 回填。
// ---------------------------------------------------------------------------

using System.Net.Http;
using DryIoc;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Caching;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Logging.Correlation;
using LYBT.Shared.Logging.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Desktop.Shell.Extensions;

/// <summary>
/// 基于 DryIoc 容器的 <see cref="IDesktopHttpHandlerProvider"/> 实现。
/// </summary>
internal sealed class DryIocHttpHandlerProvider : IDesktopHttpHandlerProvider
{
    private readonly IContainer _container;
    private readonly ApiClientOptions _options;
    private IHttpClientFactory? _factory;

    /// <summary>
    /// 初始化 <see cref="DryIocHttpHandlerProvider"/> 类的新实例。
    /// </summary>
    /// <param name="container">DryIoc 容器。</param>
    /// <param name="options">API 客户端选项。</param>
    public DryIocHttpHandlerProvider(IContainer container, ApiClientOptions options)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// 回填由工厂产出的 <see cref="IHttpClientFactory"/>（供 <c>TokenRefreshHandler</c> 发起刷新请求）。
    /// </summary>
    /// <param name="factory">私有传输工厂。</param>
    public void AttachFactory(IHttpClientFactory factory) =>
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    /// <inheritdoc />
    public DelegatingHandler CreateAuthorizationHandler() =>
        new AuthorizationMessageHandler(
            _container.Resolve<ITokenStorageService>(),
            _container.Resolve<ILogger<AuthorizationMessageHandler>>());

    /// <inheritdoc />
    public DelegatingHandler CreateTokenRefreshHandler() =>
        new TokenRefreshHandler(
            _container.Resolve<ITokenStorageService>(),
            _container.Resolve<ICredentialVault>(),
            _factory ?? throw new InvalidOperationException(
                "DryIocHttpHandlerProvider.AttachFactory 未调用：TokenRefreshHandler 需要私有 IHttpClientFactory。"),
            Options.Create(_options),
            _container.Resolve<ILogger<TokenRefreshHandler>>(),
            userActivityState: null,
            eventAggregator: null,
            connectionSettings: _container.Resolve<IConnectionSettingsService>());

    /// <inheritdoc />
    public DelegatingHandler CreateLoggingHandler() =>
        new LoggingHttpHandler(
            _container.Resolve<ILogger<LoggingHttpHandler>>(),
            _container.Resolve<ICorrelationIdProvider>());

    /// <inheritdoc />
    public DelegatingHandler CreateCachingHandler() =>
        new CachingHttpMessageHandler(
            _container.Resolve<IMemoryCache>(),
            _container.Resolve<DesktopCacheKeyRegistry>(),
            _container.Resolve<ITokenStorageService>(),
            _container.Resolve<ILogger<CachingHttpMessageHandler>>());
}
