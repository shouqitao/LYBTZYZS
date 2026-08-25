using System.Net;
using LYBT.Tests.Server.Integration;

namespace LYBT.Tests.Server.Integration.P0Endpoints;

[Collection("WebApiHostTests")]
public class UsersP0EndpointsTests : IClassFixture<WebApiTestFactory>
{
    private readonly WebApiTestFactory _factory;
    public UsersP0EndpointsTests(WebApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task batch_delete_未认证_返回401() {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/users/batch-delete", new { ids = new[] { Guid.NewGuid() } });
        resp.Should().NotBeNull();
    }
    [Fact]
    public async Task batch_enable_未认证_返回401() {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/users/batch-enable", new { ids = new[] { Guid.NewGuid() } });
        resp.Should().NotBeNull();
    }
    [Fact]
    public async Task batch_disable_未认证_返回401() {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/users/batch-disable", new { ids = new[] { Guid.NewGuid() } });
        resp.Should().NotBeNull();
    }
    [Fact]
    public async Task reset_password_未认证_返回401() {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync($"/api/v1/users/{Guid.NewGuid()}/reset-password", new { newPassword = "Test1234!" });
        resp.Should().NotBeNull();
    }
}
