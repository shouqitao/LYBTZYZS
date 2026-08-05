// ---------------------------------------------------------------------------
// HttpClientApiClientExtensions — DI registration for HttpClientApiClient
// ---------------------------------------------------------------------------
// Registers IApiClient backed by HttpClientApiClient (LocalWebAPI mode)
// using Microsoft.Extensions.Http IHttpClientFactory typed client pattern.
//
// This is the counterpart to RefitApiClientExtensions (Remote mode).
// ---------------------------------------------------------------------------

using System.Net.Http;
using LYBT.Desktop.Contracts.ApiClient;
using Prism.Ioc;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// 在 Prism DryIoc 容器中注册 <see cref="HttpClientApiClient"/> 的扩展方法。
/// </summary>
public static class HttpClientApiClientExtensions
{
    /// <summary>
    /// Named HttpClient key used by <see cref="HttpClientApiClient"/> for LocalWebAPI requests.
    /// </summary>
    public const string HttpClientName = "LocalWebApi";

    /// <summary>
    /// 注册 <see cref="IApiClient"/> 为基于 <see cref="HttpClientApiClient"/> 的单例，
    /// 并通过 <c>IHttpClientFactory</c> 配置带指定基地址的命名 <see cref="HttpClient"/>。
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="HttpClient"/> is registered as a named client with <c>Microsoft.Extensions.Http</c>,
    /// enabling future handler pipeline configuration (logging, retry, auth) via
    /// <c>AddHttpClient().AddHttpMessageHandler(...)</c>.</para>
    /// <para>Caller is responsible for ensuring the <paramref name="port"/> is valid before calling this method.</para>
    /// </remarks>
    /// <param name="containerRegistry">The Prism DryIoc container registry.</param>
    /// <param name="port">
    /// The TCP port the local WebAPI is listening on (e.g. from <c>LocalWebApiHost.Port</c>).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="port"/> is outside the valid TCP port range.
    /// </exception>
    public static void AddHttpClientApiClient(
        this IContainerRegistry containerRegistry,
        int port)
    {
        ArgumentNullException.ThrowIfNull(containerRegistry);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535);

        var baseAddress = new Uri($"http://127.0.0.1:{port}");

        // Register named HttpClient via Microsoft.Extensions.Http
        // The typed client pattern uses IHttpClientFactory.CreateClient(name)
        containerRegistry.RegisterSingleton<IHttpClientFactory>(resolver =>
        {
            var factory = resolver.Resolve<IServiceProvider>()
                .GetService(typeof(IHttpClientFactory)) as IHttpClientFactory;
            if (factory is not null)
                return factory;

            // Fallback: create a minimal factory for the named client
            return new LocalWebApiHttpClientFactory(baseAddress);
        });

        // Register IApiClient → HttpClientApiClient (singleton)
        containerRegistry.RegisterSingleton<IApiClient>(resolver =>
        {
            var httpClientFactory = resolver.Resolve<IHttpClientFactory>();
            return new HttpClientApiClient(httpClientFactory);
        });
    }

    /// <summary>
    /// 注册 <see cref="IApiClient"/> 为基于 <see cref="HttpClientApiClient"/> 的单例，
    /// 使用容器中已有的 <see cref="IHttpClientFactory"/>。
    /// </summary>
    /// <remarks>
    /// Use this overload when <see cref="IHttpClientFactory"/> is already configured (e.g. via
    /// <c>AddHttpClient</c> in the Shell composition root) and you only need to wire up the API client.
    /// </remarks>
    /// <param name="containerRegistry">The Prism DryIoc container registry.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="containerRegistry"/> is null.
    /// </exception>
    public static void AddHttpClientApiClient(
        this IContainerRegistry containerRegistry)
    {
        ArgumentNullException.ThrowIfNull(containerRegistry);

        // Register IApiClient → HttpClientApiClient (singleton)
        // Assumes IHttpClientFactory is already registered in the container
        containerRegistry.RegisterSingleton<IApiClient>(resolver =>
        {
            var httpClientFactory = resolver.Resolve<IHttpClientFactory>();
            return new HttpClientApiClient(httpClientFactory);
        });
    }

    /// <summary>
    /// 适用于 LocalWebAPI 模式的最小 <see cref="IHttpClientFactory"/> 实现。
    /// 返回带配置基地址的共享 <see cref="HttpClient"/> 实例。
    /// </summary>
    /// <remarks>
    /// <para>This is a lightweight fallback when <c>Microsoft.Extensions.Http</c> is not available
    /// in the DI container. For production use, prefer the overload that delegates to
    /// <c>AddHttpClient</c> from <c>Microsoft.Extensions.DependencyInjection</c>.</para>
    /// <para>The returned <see cref="HttpClient"/> is long-lived and thread-safe; callers must not
    /// dispose it.</para>
    /// </remarks>
    internal sealed class LocalWebApiHttpClientFactory : IHttpClientFactory, IDisposable
    {
        private readonly HttpClient _httpClient;
        private bool _disposed;

        /// <summary>
        /// 初始化 <see cref="LocalWebApiHttpClientFactory"/> 的新实例。
        /// </summary>
        /// <param name="baseAddress">The base address for LocalWebAPI requests.</param>
        public LocalWebApiHttpClientFactory(Uri baseAddress)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = baseAddress,
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <inheritdoc />
        public HttpClient CreateClient(string name) => _httpClient;

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _httpClient.Dispose();
        }
    }
}
