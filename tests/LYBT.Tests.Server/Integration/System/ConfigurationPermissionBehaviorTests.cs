using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using LYBT.Infrastructure.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace LYBT.Tests.Server.Integration.System;

/// <summary>
/// Configuration 权限隔离行为测试（CONFIG-PERM-FIX: Admin 不应访问系统配置——
/// 真机 bug 的自动化回归：testadmin GET /api/v1/configuration 曾 200（应 403））。
/// TestAuthHandler 注入角色 claim——零 DB 依赖（WebApiTestFactory 禁 DB 初始化），
/// 真实走授权中间件验证 403/200 行为。
/// </summary>
[Collection("WebApiHostTests")]
public class ConfigurationPermissionBehaviorTests
{
    private const string TestAuthScheme = "TestAuth";

    [Fact]
    public async Task Admin_AccessConfiguration_Returns403()
    {
        TestAuthHandler.Role = RoleConstants.Admin;
        using var factory = new PermissionTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/configuration");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "配置管理 sysadmin 专属——Admin 业务管理员访问系统配置应 403（真机 bug 回归守卫）");
    }

    [Fact]
    public async Task SuperAdmin_AccessConfiguration_Returns200()
    {
        TestAuthHandler.Role = RoleConstants.SuperAdmin;
        using var factory = new PermissionTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/configuration");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "sysadmin 应能正常读取系统配置（US-SHELL-018 角色: sysadmin）");
    }

    /// <summary>注入指定角色 claim 的认证 handler（WebApplicationFactory 测试认证官方模式）</summary>
    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public static string Role { get; set; } = RoleConstants.SuperAdmin;

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000001"),
                new Claim(ClaimTypes.Name, "permission-test"),
                new Claim(ClaimTypes.Role, Role)
            };
            var identity = new ClaimsIdentity(claims, TestAuthScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, TestAuthScheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    /// <summary>继承既有 WebApiTestFactory（测试库连接串 + 禁 DB 初始化），覆盖认证为 TestAuth</summary>
    private sealed class PermissionTestFactory : WebApiTestFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthScheme;
                    options.DefaultChallengeScheme = TestAuthScheme;
                    options.DefaultScheme = TestAuthScheme;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthScheme, "Test Auth", _ => { });
            });
        }
    }
}
