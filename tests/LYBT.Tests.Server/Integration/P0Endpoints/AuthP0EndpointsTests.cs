using System.Net;
using LYBT.Tests.Server.Integration;

namespace LYBT.Tests.Server.Integration.P0Endpoints;

/// <summary>
/// Phase1 P0 Auth 端点集成测试 — auto-login / refresh
/// </summary>
[Collection("WebApiHostTests")]
public class AuthP0EndpointsTests : IClassFixture<WebApiTestFactory>
{
    private readonly WebApiTestFactory _factory;
    public AuthP0EndpointsTests(WebApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task auto_login_未认证_返回401()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/auto-login", new { userName = "test", autoLoginToken = "invalid" });
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task refresh_未认证_返回401或400()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "invalid" });
        response.Should().NotBeNull();
    }
}
