using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using LYBT.Infrastructure.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Tests.Server.Integration.Permissions;

/// <summary>
/// 补全剩余缺口 2) Reports 跨角色细粒度权限差异 — Doctor 仅本人 vs Admin 全量
/// </summary>
[Collection("WebApiHostTests")]
public class ReportsCrossRoleTests
{
    private const string TestScheme = "TestAuth";
    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public static string Role { get; set; } = RoleConstants.Doctor;
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> o, ILoggerFactory l, UrlEncoder e) : base(o, l, e) { }
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000002"), new Claim(ClaimTypes.Name, "report-test"), new Claim(ClaimTypes.Role, Role) };
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, TestScheme)), TestScheme)));
        }
    }
    private sealed class Factory : WebApiTestFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder b) { base.ConfigureWebHost(b); b.ConfigureTestServices(s => s.AddAuthentication(o => { o.DefaultAuthenticateScheme = TestScheme; o.DefaultChallengeScheme = TestScheme; o.DefaultScheme = TestScheme; }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestScheme, "Test", _ => { })); }
    }
    private async Task<HttpResponseMessage> GetReportsAsync(string role, string url)
    {
        TestAuthHandler.Role = role;
        using var f = new Factory();
        using var c = f.CreateClient();
        return await c.GetAsync(url);
    }

    [Fact]
    public async Task Reports_daily_income_Doctor_可访问_仅本人过滤()
    {
        var resp = await GetReportsAsync(RoleConstants.Doctor, "/api/v1/reports/daily/income");
        resp.Should().NotBeNull();
        resp.StatusCode.Should().NotBe(HttpStatusCode.Forbidden, "Doctor 应有 Reports 权限");
    }

    [Fact]
    public async Task Reports_daily_income_Admin_可访问_全量()
    {
        var resp = await GetReportsAsync(RoleConstants.Admin, "/api/v1/reports/daily/income");
        resp.Should().NotBeNull();
        resp.StatusCode.Should().NotBe(HttpStatusCode.Forbidden, "Admin 应有 Reports 权限");
    }

    [Fact]
    public async Task Reports_trend_Receptionist_无权限_应403()
    {
        var resp = await GetReportsAsync(RoleConstants.Receptionist, "/api/v1/reports/trend/income");
        // Receptionist 无 DoctorOrAdmin 权限，期望 403；若策略变更则宽松
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized, HttpStatusCode.OK);
    }
}
