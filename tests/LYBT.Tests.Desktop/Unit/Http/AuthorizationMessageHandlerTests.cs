using FluentAssertions;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Net.Http;
using Xunit;

namespace LYBT.Tests.Desktop.Unit;

/// <summary>
/// TDD Batch 2 — AuthorizationMessageHandler（DelegatingHandler）单元测试。
/// 覆盖：有 Token 注入 Bearer header、无 Token 跳过、匿名端点（health/login/refresh）
/// 不查 Token、非匿名端点注入、Token 存储异常传播。
/// </summary>
public class AuthorizationMessageHandlerTests
{
    /// <summary>记录穿透 handler 链的最终请求（不访问真实网络）。</summary>
    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }

    private static (AuthorizationMessageHandler Handler, ITokenStorageService TokenStorage, CaptureHandler Inner) CreateSut(
        string? token = null)
    {
        var tokenStorage = Substitute.For<ITokenStorageService>();
        tokenStorage.GetTokenAsync().Returns(Task.FromResult<string?>(token));

        var logger = Substitute.For<ILogger<AuthorizationMessageHandler>>();
        var inner = new CaptureHandler();
        var handler = new AuthorizationMessageHandler(tokenStorage, logger)
        {
            InnerHandler = inner
        };
        return (handler, tokenStorage, inner);
    }

    private static async Task<HttpResponseMessage> SendGetAsync(AuthorizationMessageHandler handler, string url)
    {
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5300") };
        return await client.GetAsync(url);
    }

    #region Token 注入

    [Fact]
    public async Task HasToken_InjectsAuthorizationHeader()
    {
        const string token = "eyJhbGciOiJIUzI1NiJ9.payload.signature";
        var (handler, tokenStorage, inner) = CreateSut(token);

        await SendGetAsync(handler, "/api/v1/patients");

        inner.LastRequest!.Headers.Authorization.Should().NotBeNull();
        inner.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        inner.LastRequest.Headers.Authorization.Parameter.Should().Be(token);
        await tokenStorage.Received(1).GetTokenAsync();
    }

    [Fact]
    public async Task NoToken_SkipsAuthorizationHeader()
    {
        var (handler, tokenStorage, inner) = CreateSut(token: null);

        await SendGetAsync(handler, "/api/v1/patients");

        inner.LastRequest!.Headers.Authorization.Should().BeNull();
        await tokenStorage.Received(1).GetTokenAsync();
    }

    #endregion

    #region 匿名端点跳过

    [Fact]
    public async Task AnonymousEndpoint_Health_SkipsTokenCheck()
    {
        var (handler, tokenStorage, inner) = CreateSut();

        await SendGetAsync(handler, "/health");

        await tokenStorage.DidNotReceive().GetTokenAsync();
        inner.LastRequest!.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task AnonymousEndpoint_Login_SkipsTokenCheck()
    {
        var (handler, tokenStorage, inner) = CreateSut();

        await SendGetAsync(handler, "/api/v1/auth/login");

        await tokenStorage.DidNotReceive().GetTokenAsync();
        inner.LastRequest!.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task AnonymousEndpoint_Refresh_SkipsTokenCheck()
    {
        var (handler, tokenStorage, inner) = CreateSut();

        await SendGetAsync(handler, "/api/v1/auth/refresh");

        await tokenStorage.DidNotReceive().GetTokenAsync();
        inner.LastRequest!.Headers.Authorization.Should().BeNull();
    }

    #endregion

    #region 非匿名端点

    [Fact]
    public async Task NonAnonymousEndpoint_InjectsToken()
    {
        const string token = "jwt-token-123";
        var (handler, _, inner) = CreateSut(token);

        await SendGetAsync(handler, "/api/v1/patients");

        inner.LastRequest!.Headers.Authorization.Should().NotBeNull();
        inner.LastRequest.Headers.Authorization!.Parameter.Should().Be(token);
    }

    #endregion

    #region 异常传播

    [Fact]
    public async Task TokenStorageThrows_PropagatesException()
    {
        var tokenStorage = Substitute.For<ITokenStorageService>();
        tokenStorage.GetTokenAsync()
            .Returns(Task.FromException<string?>(new InvalidOperationException("storage failed")));

        var logger = Substitute.For<ILogger<AuthorizationMessageHandler>>();
        var handler = new AuthorizationMessageHandler(tokenStorage, logger)
        {
            InnerHandler = new CaptureHandler()
        };

        var act = () => SendGetAsync(handler, "/api/v1/patients");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("storage failed");
    }

    #endregion
}
