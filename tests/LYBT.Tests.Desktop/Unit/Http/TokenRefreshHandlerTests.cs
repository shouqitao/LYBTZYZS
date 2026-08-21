using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Prism.Events;
using System.Net;
using System.Net.Http;
using Xunit;

namespace LYBT.Tests.Desktop;

/// <summary>
/// TDD Batch 3 — TokenRefreshHandler（DelegatingHandler，535 行）单元测试。
/// 覆盖：过期检测（提前 5 分钟）、滑动过期（用户不活跃跳过）、SemaphoreSlim 防并发、
/// 失败重试（3 次指数退避）、RefreshToken 缺失、AutoLogin 降级（触发/成功/失败清凭据）、
/// Prism 事件发布。
/// _refreshHttpClient 在构造器内创建（new HttpClientHandler）无法注入——用进程内
/// Kestrel 迷你服务器捕获 /api/v1/auth/refresh 与 /api/v1/auth/auto-login 调用，
/// BaseUrl 由 IOptions&lt;ApiClientOptions&gt; 指向该服务器。
/// </summary>
public class TokenRefreshHandlerTests : IAsyncLifetime
{
    private readonly FakeAuthServer _server = new();
    private readonly ITokenStorageService _tokenStorage = Substitute.For<ITokenStorageService>();
    private readonly ICredentialVault _credentialVault = Substitute.For<ICredentialVault>();
    private readonly IUserActivityState _userActivity = Substitute.For<IUserActivityState>();
    private readonly IEventAggregator _eventAggregator = Substitute.For<IEventAggregator>();
    private readonly ILogger<TokenRefreshHandler> _logger = Substitute.For<ILogger<TokenRefreshHandler>>();

    private AuthEvents.TokenRefreshSucceededEvent? _succeededEvent;
    private AuthEvents.SessionExtendedEvent? _sessionEvent;
    private AuthEvents.TokenRefreshFailedEvent? _failedEvent;

    public Task InitializeAsync() => _server.InitializeAsync();
    public Task DisposeAsync() => _server.DisposeAsync();

    private static LoginResponse MakeLogin(DateTime expiresAt, string? autoLoginToken = null, string token = "old-access-token")
        => new()
        {
            Token = token,
            RefreshToken = "old-refresh-token",
            AutoLoginToken = autoLoginToken,
            ExpiresAt = expiresAt,
            User = new UserDetailDto { UserName = "user1" }
        };

    private static LoginResponse MakeFreshLogin(string? autoLoginToken = null)
        => MakeLogin(DateTime.UtcNow.AddMinutes(30), autoLoginToken, token: "new-access-token");

    /// <summary>记录穿透 handler 链的原始请求（不访问真实网络）。</summary>
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

    /// <summary>
    /// 进程内 Kestrel 迷你服务器：捕获 TokenRefreshHandler 内部 _refreshHttpClient
    /// 发出的 refresh / auto-login 调用，按测试注入的 handler 返回固定响应。
    /// </summary>
    private sealed class FakeAuthServer : IAsyncLifetime
    {
        private WebApplication? _app;

        public string BaseUrl { get; private set; } = string.Empty;
        public int RefreshCalls { get; private set; }
        public int AutoLoginCalls { get; private set; }
        public Func<HttpContext, Task>? OnRefresh { get; set; }
        public Func<HttpContext, Task>? OnAutoLogin { get; set; }

        public async Task InitializeAsync()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://127.0.0.1:0");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

            var builder = WebApplication.CreateBuilder(new[] { "--no-launch-settings" });
            builder.WebHost.ConfigureKestrel(o => o.Listen(System.Net.IPAddress.Loopback, 0));
            _app = builder.Build();

            _app.MapPost("/api/v1/auth/refresh", async ctx =>
            {
                RefreshCalls++;
                if (OnRefresh is not null)
                {
                    await OnRefresh(ctx);
                    return;
                }
                await WriteRefreshSuccess(ctx);
            });

            _app.MapPost("/api/v1/auth/auto-login", async ctx =>
            {
                AutoLoginCalls++;
                if (OnAutoLogin is not null)
                {
                    await OnAutoLogin(ctx);
                    return;
                }
                await WriteAutoLoginSuccess(ctx);
            });

            await _app.StartAsync();
            BaseUrl = _app.Urls.First();
        }

        public async Task DisposeAsync()
        {
            if (_app is not null)
                await _app.DisposeAsync();
        }

        public static Task WriteRefreshSuccess(HttpContext ctx)
            => ctx.Response.WriteAsJsonAsync(new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = MakeFreshLogin()
            });

        public static Task WriteAutoLoginSuccess(HttpContext ctx)
            => ctx.Response.WriteAsJsonAsync(new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = MakeFreshLogin(autoLoginToken: "new-auto-token")
            });

        public static async Task WriteError(HttpContext ctx, HttpStatusCode statusCode, string body)
        {
            ctx.Response.StatusCode = (int)statusCode;
            await ctx.Response.WriteAsync(body);
        }
    }

    private TokenRefreshHandler CreateHandler(
        LoginResponse? login,
        string? refreshToken = "refresh-token-1",
        bool userActive = true)
    {
        _tokenStorage.GetLoginResponseAsync().Returns(_ => Task.FromResult<LoginResponse?>(login));
        _tokenStorage.GetRefreshTokenAsync().Returns(Task.FromResult<string?>(refreshToken));
        _tokenStorage.SaveAuthenticationAsync(Arg.Any<LoginResponse>(), Arg.Any<bool>())
            .Returns(Task.CompletedTask);

        _userActivity.IsUserActive.Returns(userActive);

        _credentialVault.GetAutoLoginTokenAsync(Arg.Any<string>())
            .Returns(Task.FromResult<string?>("auto-login-token"));
        _credentialVault.ClearCredentialsAsync(Arg.Any<string>()).Returns(true);
        _credentialVault.SaveAutoLoginTokenAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        _succeededEvent = Substitute.For<AuthEvents.TokenRefreshSucceededEvent>();
        _sessionEvent = Substitute.For<AuthEvents.SessionExtendedEvent>();
        _failedEvent = Substitute.For<AuthEvents.TokenRefreshFailedEvent>();
        _eventAggregator.GetEvent<AuthEvents.TokenRefreshSucceededEvent>().Returns(_succeededEvent);
        _eventAggregator.GetEvent<AuthEvents.SessionExtendedEvent>().Returns(_sessionEvent);
        _eventAggregator.GetEvent<AuthEvents.TokenRefreshFailedEvent>().Returns(_failedEvent);

        var handler = new TokenRefreshHandler(
            _tokenStorage,
            _credentialVault,
            Options.Create(new ApiClientOptions { BaseUrl = _server.BaseUrl, IgnoreSslErrors = true }),
            _logger,
            _userActivity,
            _eventAggregator)
        {
            InnerHandler = new CaptureHandler()
        };
        return handler;
    }

    private static async Task<HttpResponseMessage> SendGetAsync(TokenRefreshHandler handler, string url)
    {
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5300") };
        return await client.GetAsync(url);
    }

    #region 过期检测

    [Fact]
    public async Task TokenNotExpiring_PassesThrough()
    {
        var handler = CreateHandler(MakeLogin(DateTime.UtcNow.AddMinutes(60)));

        await SendGetAsync(handler, "/api/v1/patients");

        _server.RefreshCalls.Should().Be(0);
        await _tokenStorage.DidNotReceive().GetRefreshTokenAsync();
    }

    [Fact]
    public async Task TokenExpiringSoon_TriggersRefresh()
    {
        var handler = CreateHandler(MakeLogin(DateTime.UtcNow.AddMinutes(2)));

        await SendGetAsync(handler, "/api/v1/patients");

        _server.RefreshCalls.Should().Be(1);
        await _tokenStorage.Received(1).GetRefreshTokenAsync();
    }

    #endregion

    #region 刷新成功

    [Fact]
    public async Task RefreshSuccess_SavesNewToken()
    {
        var handler = CreateHandler(MakeLogin(DateTime.UtcNow.AddMinutes(2)));

        await SendGetAsync(handler, "/api/v1/patients");

        await _tokenStorage.Received(1).SaveAuthenticationAsync(
            Arg.Is<LoginResponse>(l => l.Token == "new-access-token"), Arg.Any<bool>());
        _userActivity.Received(1).ResetActivity();
    }

    #endregion

    #region 滑动过期

    [Fact]
    public async Task UserInactive_SkipsRefresh()
    {
        var handler = CreateHandler(MakeLogin(DateTime.UtcNow.AddMinutes(2)), userActive: false);

        await SendGetAsync(handler, "/api/v1/patients");

        _server.RefreshCalls.Should().Be(0);
        await _tokenStorage.DidNotReceive().GetRefreshTokenAsync();
    }

    #endregion

    #region 并发

    [Fact]
    public async Task ConcurrentRequests_NoDoubleRefresh()
    {
        // 状态化存储：刷新成功后 GetLoginResponseAsync 返回新登录（后续请求跳过刷新）
        LoginResponse? current = MakeLogin(DateTime.UtcNow.AddMinutes(2));
        _tokenStorage.GetLoginResponseAsync().Returns(_ => Task.FromResult<LoginResponse?>(current));
        _tokenStorage.GetRefreshTokenAsync().Returns(Task.FromResult<string?>("refresh-token-1"));
        _tokenStorage.SaveAuthenticationAsync(Arg.Any<LoginResponse>(), Arg.Any<bool>())
            .Returns(Task.CompletedTask)
            .AndDoes(ci => current = ci.Arg<LoginResponse>());

        _userActivity.IsUserActive.Returns(true);
        _succeededEvent = Substitute.For<AuthEvents.TokenRefreshSucceededEvent>();
        _sessionEvent = Substitute.For<AuthEvents.SessionExtendedEvent>();
        _failedEvent = Substitute.For<AuthEvents.TokenRefreshFailedEvent>();
        _eventAggregator.GetEvent<AuthEvents.TokenRefreshSucceededEvent>().Returns(_succeededEvent);
        _eventAggregator.GetEvent<AuthEvents.SessionExtendedEvent>().Returns(_sessionEvent);
        _eventAggregator.GetEvent<AuthEvents.TokenRefreshFailedEvent>().Returns(_failedEvent);

        var handler = new TokenRefreshHandler(
            _tokenStorage, _credentialVault,
            Options.Create(new ApiClientOptions { BaseUrl = _server.BaseUrl, IgnoreSslErrors = true }),
            _logger, _userActivity, _eventAggregator)
        {
            InnerHandler = new CaptureHandler()
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5300") };
        await Task.WhenAll(
            client.GetAsync("/api/v1/patients/1"),
            client.GetAsync("/api/v1/patients/2"));

        _server.RefreshCalls.Should().Be(1);
    }

    #endregion

    #region 重试

    [Fact]
    public async Task RefreshFails_Retries3Times()
    {
        _server.OnRefresh = ctx => FakeAuthServer.WriteError(ctx, HttpStatusCode.InternalServerError, "server boom");
        var handler = CreateHandler(MakeLogin(DateTime.UtcNow.AddMinutes(2)));

        var result = await handler.RefreshTokenAsync();

        _server.RefreshCalls.Should().Be(3);
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be(TokenRefreshFailureReason.ServerError);
    }

    #endregion

    #region RefreshToken 缺失

    [Fact]
    public async Task NoRefreshToken_ReturnsNotLoggedIn()
    {
        var handler = CreateHandler(MakeLogin(DateTime.UtcNow.AddMinutes(2)), refreshToken: null);

        var result = await handler.RefreshTokenAsync();

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be(TokenRefreshFailureReason.NotLoggedIn);
        _server.RefreshCalls.Should().Be(0);
    }

    #endregion

    #region AutoLogin 降级

    [Fact]
    public async Task AutoLogin_TriggersOnExpiredRefresh()
    {
        // refresh 401 + "expired" → RefreshTokenExpired（不可重试）→ AutoLogin 降级触发
        _server.OnRefresh = ctx => FakeAuthServer.WriteError(ctx, HttpStatusCode.Unauthorized, "refresh token expired");
        var handler = CreateHandler(MakeLogin(DateTime.UtcNow.AddMinutes(2)));

        var result = await handler.RefreshTokenAsync();

        result.Success.Should().BeTrue();
        _server.RefreshCalls.Should().Be(1);
        _server.AutoLoginCalls.Should().Be(1);
        await _credentialVault.Received(1).GetAutoLoginTokenAsync("user1");
    }

    [Fact]
    public async Task AutoLogin_Success_ReturnsNewToken()
    {
        _server.OnRefresh = ctx => FakeAuthServer.WriteError(ctx, HttpStatusCode.Unauthorized, "refresh token expired");
        var handler = CreateHandler(MakeLogin(DateTime.UtcNow.AddMinutes(2)));

        var result = await handler.RefreshTokenAsync();

        result.Success.Should().BeTrue();
        await _tokenStorage.Received(1).SaveAuthenticationAsync(
            Arg.Is<LoginResponse>(l => l.AutoLoginToken == "new-auto-token"), true);
        await _credentialVault.Received(1).SaveAutoLoginTokenAsync("user1", "new-auto-token");
    }

    [Fact]
    public async Task AutoLogin_Failure_ClearsCredentials()
    {
        _server.OnRefresh = ctx => FakeAuthServer.WriteError(ctx, HttpStatusCode.Unauthorized, "refresh token expired");
        _server.OnAutoLogin = ctx => FakeAuthServer.WriteError(ctx, HttpStatusCode.Unauthorized, "auto-login token invalid");
        var handler = CreateHandler(MakeLogin(DateTime.UtcNow.AddMinutes(2)));

        var result = await handler.RefreshTokenAsync();

        // 降级失败时返回原始刷新失败（RefreshTokenExpired，requiresReLogin）——
        // 清除凭据副作用已在 TryAutoLoginFallbackAsync 内完成（401 → ClearCredentialsAsync）
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be(TokenRefreshFailureReason.RefreshTokenExpired);
        await _credentialVault.Received(1).ClearCredentialsAsync("user1");
    }

    [Fact]
    public async Task AutoLogin_SkipsOnRevokedRefresh_WhenNoStoredAutoToken()
    {
        // T5.1: RefreshTokenRevoked 亦属可降级分支，但无存储 AutoLoginToken 时应跳过降级直接失败
        _server.OnRefresh = ctx => FakeAuthServer.WriteError(ctx, HttpStatusCode.Unauthorized, "refresh token revoked");
        var handler = CreateHandler(MakeLogin(DateTime.UtcNow.AddMinutes(2)));
        _credentialVault.GetAutoLoginTokenAsync("user1").Returns(Task.FromResult<string?>(null));

        var result = await handler.RefreshTokenAsync();

        result.Success.Should().BeFalse();
        _server.AutoLoginCalls.Should().Be(0);
        await _credentialVault.DidNotReceive().SaveAutoLoginTokenAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task AutoLogin_SkipsOnUserDisabled()
    {
        // T5.1: UserDisabled 不降级（IsAutoLoginEligible=false）——直接返回失败且不调 AutoLogin 端点
        _server.OnRefresh = ctx => FakeAuthServer.WriteError(ctx, HttpStatusCode.Forbidden, "account disabled");
        var handler = CreateHandler(MakeLogin(DateTime.UtcNow.AddMinutes(2)));

        var result = await handler.RefreshTokenAsync();

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be(TokenRefreshFailureReason.UserDisabled);
        _server.AutoLoginCalls.Should().Be(0);
    }

    [Fact]
    public async Task AutoLogin_SkipsOnNetworkError_AfterRetriesExhausted()
    {
        // T5.1: NetworkError/ServerError 属可重试错误，3 次重试耗尽后仍失败则不降级（仅过期/撤销/无效降级）
        _server.OnRefresh = ctx => throw new HttpRequestException("network down");
        var handler = CreateHandler(MakeLogin(DateTime.UtcNow.AddMinutes(2)));

        var result = await handler.RefreshTokenAsync();

        result.Success.Should().BeFalse();
        result.FailureReason.Should().BeOneOf(TokenRefreshFailureReason.NetworkError, TokenRefreshFailureReason.ServerError);
        _server.RefreshCalls.Should().Be(3);
        _server.AutoLoginCalls.Should().Be(0);
    }

    #endregion

    #region 事件发布

    [Fact]
    public async Task PublishesEvents_OnSuccess()
    {
        var handler = CreateHandler(MakeLogin(DateTime.UtcNow.AddMinutes(2)));

        await SendGetAsync(handler, "/api/v1/patients");

        _succeededEvent!.Received(1).Publish(Arg.Any<TokenRefreshSucceededPayload>());
        _sessionEvent!.Received(1).Publish(Arg.Any<SessionExtendedPayload>());
    }

    #endregion
}
