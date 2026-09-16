// ---------------------------------------------------------------------------
// NamedLocalApiClientFactory — 把具名 IHttpClientFactory 适配为「本地模式工厂」
// ---------------------------------------------------------------------------
// HttpClientApiClient 要求注入 IHttpClientFactory，并在每次请求时 CreateClient()。
// 本适配器把任意具名客户端（LocalApi）包装成该形状：忽略传入的 name，
// 每次返回新的 HttpClient 包装（BaseAddress/Timeout 由这里补齐），
// 底层 handler 由私有 IHttpClientFactory 池化管理 —— 调用方 using 释放包装不会破坏后续请求。
// ---------------------------------------------------------------------------

using System.Net.Http;
using LYBT.Shared.Configuration.Options.Client;

namespace LYBT.Desktop.Foundation.Http;

/// <summary>
/// 将具名 <see cref="IHttpClientFactory"/> 适配为本地模式所需的 <see cref="IHttpClientFactory"/>。
/// </summary>
public sealed class NamedLocalApiClientFactory : IHttpClientFactory
{
    private readonly IHttpClientFactory _inner;
    private readonly Uri _baseAddress;
    private readonly TimeSpan _timeout;

    /// <summary>
    /// 初始化 <see cref="NamedLocalApiClientFactory"/> 类的新实例。
    /// </summary>
    /// <param name="inner">底层具名工厂。</param>
    /// <param name="baseAddress">本地模式基地址。</param>
    /// <param name="options">API 客户端选项（提供超时）。</param>
    /// <exception cref="ArgumentNullException">任一参数为 null。</exception>
    public NamedLocalApiClientFactory(IHttpClientFactory inner, Uri baseAddress, ApiClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _baseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));
        _timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    }

    /// <inheritdoc />
    /// <remarks>忽略 <paramref name="name"/>：本地模式固定使用具名客户端 <see cref="DesktopHttpTransportFactory.LocalClientName"/>。</remarks>
    public HttpClient CreateClient(string name)
    {
        var client = _inner.CreateClient(DesktopHttpTransportFactory.LocalClientName);
        client.BaseAddress = _baseAddress;
        client.Timeout = _timeout;
        return client;
    }
}
