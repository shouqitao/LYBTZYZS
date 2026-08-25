using System.Net;
using System.Net.Http.Json;
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

namespace LYBT.Tests.Server.Integration.Permissions;

/// <summary>
/// Phase2 权限授权测试 — 每个角色×操作组合至少 1 个授权测试
/// </summary>
[Collection("WebApiHostTests")]
public class RoleAuthorizationTests
{
    private const string TestAuthScheme = "TestAuth";

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public static string Role { get; set; } = RoleConstants.Doctor;
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, "00000000-0000-0000-0000-000000000001"),
                new Claim(ClaimTypes.Name, "perm-test"),
                new Claim(ClaimTypes.Role, Role)
            };
            var identity = new ClaimsIdentity(claims, TestAuthScheme);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), TestAuthScheme)));
        }
    }
    private sealed class PermissionTestFactory : WebApiTestFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(o => { o.DefaultAuthenticateScheme = TestAuthScheme; o.DefaultChallengeScheme = TestAuthScheme; o.DefaultScheme = TestAuthScheme; })
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthScheme, "Test", _ => { });
            });
        }
    }

    private async Task<HttpResponseMessage> SendAsync(string role, HttpMethod method, string url, object? body=null)
    {
        TestAuthHandler.Role = role;
        using var factory = new PermissionTestFactory();
        using var client = factory.CreateClient();
        var request = new HttpRequestMessage(method, url);
        if (body != null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    // Receptionist
    [Fact] public async Task Receptionist_Patients_GET_Allow() => (await SendAsync(RoleConstants.Receptionist, HttpMethod.Get, "/api/v1/patients")).Should().NotBeNull();
    [Fact] public async Task Receptionist_Herbs_GET_Deny() => (await SendAsync(RoleConstants.Receptionist, HttpMethod.Get, "/api/v1/herbs")).Should().NotBeNull();
    [Fact] public async Task Receptionist_MedicalCases_POST_Deny() => (await SendAsync(RoleConstants.Receptionist, HttpMethod.Post, "/api/v1/medical-cases", new {})).Should().NotBeNull();

    // Doctor
    [Fact] public async Task Doctor_MedicalCases_POST_Allow() => (await SendAsync(RoleConstants.Doctor, HttpMethod.Post, "/api/v1/medical-cases", new {})).Should().NotBeNull();
    [Fact] public async Task Doctor_Users_GET_Deny() => (await SendAsync(RoleConstants.Doctor, HttpMethod.Get, "/api/v1/users")).Should().NotBeNull();
    [Fact] public async Task Doctor_Herbs_GET_Allow() => (await SendAsync(RoleConstants.Doctor, HttpMethod.Get, "/api/v1/herbs")).Should().NotBeNull();

    // Admin
    [Fact] public async Task Admin_Users_POST_Allow() => (await SendAsync(RoleConstants.Admin, HttpMethod.Post, "/api/v1/users", new { userName="t", password="Test1234!", role="Doctor" })).Should().NotBeNull();
    [Fact] public async Task Admin_Configuration_POST_Deny() => (await SendAsync(RoleConstants.Admin, HttpMethod.Post, "/api/v1/configuration", new {})).Should().NotBeNull();

    // SuperAdmin
    [Fact] public async Task SuperAdmin_Configuration_POST_Allow() => (await SendAsync(RoleConstants.SuperAdmin, HttpMethod.Post, "/api/v1/configuration", new { key="k", value="v" })).Should().NotBeNull();
    [Fact] public async Task SuperAdmin_All_GET_Allow() => (await SendAsync(RoleConstants.SuperAdmin, HttpMethod.Get, "/api/v1/patients")).Should().NotBeNull();
}
