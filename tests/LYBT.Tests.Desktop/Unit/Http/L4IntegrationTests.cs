using FluentAssertions;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Logging.Correlation;
using LYBT.Shared.Logging.Http;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Net;
using System.Net.Http;
using Xunit;

namespace LYBT.Tests.Desktop;

/// <summary>
/// TDD Batch 5 — L4 层集成验证。
/// 用例 1：Remote 模式 Handler 链组装顺序（Logging → Authorization → TokenRefresh → HttpClientHandler）
/// 按 UnifiedApiClientExtensions.remoteHttpClientFactory 的组装方式复现，做类型级顺序检查；
/// 用例 2：端到端——请求穿透整条链，Authorization 注入生效、最终到达终端 handler。
/// 用例 3/4（Batch 1-4 全量回归 + Desktop Shell 构建 0/0）为验证命令，见任务书。
/// </summary>
public class L4IntegrationTests
{
    /// <summary>终端 handler（对应 HttpClientHandler）：记录穿透整条链的最终请求。</summary>
    private sealed class TerminalHandler : HttpMessageHandler
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

    /// <summary>
    /// 按 UnifiedApiClientExtensions.remoteHttpClientFactory 的组装方式构建完整链：
    /// HttpClient → LoggingHttpHandler → AuthorizationMessageHandler → TokenRefreshHandler → TerminalHandler
    /// </summary>
    private static (HttpClient Client, LoggingHttpHandler ChainHead, TerminalHandler Terminal) BuildRemoteChain(
        string? token = null,
        LoginResponse? login = null)
    {
        var terminal = new TerminalHandler();

        var tokenStorage = Substitute.For<ITokenStorageService>();
        tokenStorage.GetTokenAsync().Returns(Task.FromResult<string?>(token));
        tokenStorage.GetLoginResponseAsync().Returns(Task.FromResult<LoginResponse?>(login));
        tokenStorage.GetRefreshTokenAsync().Returns(Task.FromResult<string?>("refresh-token"));

        var credentialVault = Substitute.For<ICredentialVault>();
        var apiOptions = Options.Create(new ApiClientOptions { BaseUrl = "http://localhost:5300" });

        // 与 DI 注册一致：TokenRefresh →(InnerHandler)→ HttpClientHandler
        var tokenRefreshHandler = new TokenRefreshHandler(
            tokenStorage, credentialVault, httpClientFactory: null, apiOptions,
            Substitute.For<ILogger<TokenRefreshHandler>>());
        tokenRefreshHandler.InnerHandler = terminal;

        // Authorization → TokenRefresh
        var authHandler = new AuthorizationMessageHandler(
            tokenStorage, Substitute.For<ILogger<AuthorizationMessageHandler>>());
        authHandler.InnerHandler = tokenRefreshHandler;

        // Logging → Authorization
        var correlationProvider = Substitute.For<ICorrelationIdProvider>();
        correlationProvider.GetCorrelationId().Returns("corr-123");
        var loggingHandler = new LoggingHttpHandler(
            Substitute.For<ILogger<LoggingHttpHandler>>(), correlationProvider);
        loggingHandler.InnerHandler = authHandler;

        return (
            new HttpClient(loggingHandler) { BaseAddress = new Uri("http://localhost:5300") },
            loggingHandler,
            terminal);
    }

    [Fact]
    public void HandlerChain_CorrectOrder()
    {
        var (client, chainHead, _) = BuildRemoteChain(token: "test-jwt");
        using var _ = client;

        // 逐层验证组装顺序：Logging → Authorization → TokenRefresh → Terminal(HttpClientHandler 替身)
        var authHandler = chainHead.InnerHandler.Should().BeOfType<AuthorizationMessageHandler>().Subject;
        var tokenRefreshHandler = authHandler.InnerHandler.Should().BeOfType<TokenRefreshHandler>().Subject;
        tokenRefreshHandler.InnerHandler.Should().BeOfType<TerminalHandler>();
    }

    [Fact]
    public async Task HandlerChain_EndToEnd_RequestFlowsThroughAllHandlers()
    {
        // 登录信息未过期 → TokenRefreshHandler 直通（不触发刷新），
        // AuthorizationMessageHandler 注入 Bearer token，最终到达终端 handler。
        var (client, _, terminal) = BuildRemoteChain(
            token: "test-jwt",
            login: new LoginResponse
            {
                Token = "test-jwt",
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserDetailDto { UserName = "user1" }
            });

        using (client)
        {
            var response = await client.GetAsync("/api/v1/patients");

            response.IsSuccessStatusCode.Should().BeTrue();
            terminal.LastRequest.Should().NotBeNull();
            terminal.LastRequest!.Headers.Authorization.Should().NotBeNull();
            terminal.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
            terminal.LastRequest.Headers.Authorization.Parameter.Should().Be("test-jwt");
        }
    }
}
