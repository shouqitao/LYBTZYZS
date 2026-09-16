// ---------------------------------------------------------------------------
// LocalModeTransportTests — 本地模式 IApiClient 传输层契约
// ---------------------------------------------------------------------------
// 走真实生产组合（DesktopHttpTransportFactory + NamedLocalApiClientFactory），锁定两条契约：
//   1) 本地请求携带 Bearer Token —— 本地端点受 [Authorize(Policy=...)] 保护
//   2) CreateClient 返回的 HttpClient 可被调用方释放而不破坏后续请求
//      （HttpApiClientBase.SendAsync 对每次调用 `using var client`）
// ---------------------------------------------------------------------------

using System.Net.Http;
using LYBT.Desktop.Foundation.Caching;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Desktop.Integration.LocalApi;

[Collection("LocalApi")]
public class LocalModeTransportTests : LocalWebApiTestBase, IDisposable
{
    private ServiceProvider? _transportProvider;

    private sealed class StubTokenStorage : ITokenStorageService
    {
        public string? Token { get; set; }

        public Task<string?> GetTokenAsync() => Task.FromResult(Token);

        public Task SaveAuthenticationAsync(LoginResponse loginResponse, bool rememberMe) => Task.CompletedTask;

        public Task<string?> GetRefreshTokenAsync() => Task.FromResult<string?>(null);

        public Task<LoginResponse?> GetLoginResponseAsync() => Task.FromResult<LoginResponse?>(null);

        public Task ClearAuthenticationAsync() => Task.CompletedTask;

        public Task<bool> IsTokenExpiredAsync() => Task.FromResult(false);

        public string? GetToken() => Token;

        public LoginResponse? GetLoginResponse() => null;

        public void ClearAuthentication() => Token = null;
    }

    /// <summary>本地链所需的处理器提供者桩（授权 + 缓存；本地链不含 TokenRefresh/Logging）。</summary>
    private sealed class StubHandlerProvider : IDesktopHttpHandlerProvider, IDisposable
    {
        private readonly ITokenStorageService _tokenStorage;
        private readonly IMemoryCache _cache;
        private readonly DesktopCacheKeyRegistry _registry;

        public StubHandlerProvider(ITokenStorageService tokenStorage)
        {
            _tokenStorage = tokenStorage;
            _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
            _registry = new DesktopCacheKeyRegistry();
        }

        public DelegatingHandler CreateAuthorizationHandler() =>
            new AuthorizationMessageHandler(_tokenStorage, NullLogger<AuthorizationMessageHandler>.Instance);

        public DelegatingHandler CreateTokenRefreshHandler() =>
            throw new NotSupportedException("本地链不含 TokenRefresh");

        public DelegatingHandler CreateLoggingHandler() =>
            throw new NotSupportedException("本地链不含 Logging");

        public DelegatingHandler CreateCachingHandler() =>
            new CachingHttpMessageHandler(
                _cache, _registry, _tokenStorage, NullLogger<CachingHttpMessageHandler>.Instance);

        public void Dispose() => _cache.Dispose();
    }

    private HttpClientApiClient CreateApiClient(StubTokenStorage tokenStorage)
    {
        var options = new ApiClientOptions { TimeoutSeconds = 30 };
        var provider = DesktopHttpTransportFactory.Build(options, new StubHandlerProvider(tokenStorage));
        _transportProvider = provider;

        var factory = new NamedLocalApiClientFactory(
            provider.GetRequiredService<IHttpClientFactory>(),
            Client.BaseAddress!,
            options);

        return new HttpClientApiClient(factory, NullLogger.Instance);
    }

    [Fact]
    public async Task Local_transport_reaches_authorized_endpoint_with_bearer_token()
    {
        var tokenStorage = new StubTokenStorage { Token = await GetAdminTokenAsync() };
        var api = CreateApiClient(tokenStorage);

        var response = await api.Patients.GetPatientsAsync(1, 20, null);

        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Local_transport_survives_repeated_calls_with_disposing_callers()
    {
        var tokenStorage = new StubTokenStorage { Token = await GetAdminTokenAsync() };
        var api = CreateApiClient(tokenStorage);

        var first = await api.Patients.GetPatientsAsync(1, 20, null);
        var second = await api.Patients.GetPatientsAsync(2, 20, null);

        first.Success.Should().BeTrue();
        second.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Local_transport_without_token_is_rejected()
    {
        var api = CreateApiClient(new StubTokenStorage());

        var act = async () => await api.Patients.GetPatientsAsync(1, 20, null);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    public void Dispose()
    {
        _transportProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}
