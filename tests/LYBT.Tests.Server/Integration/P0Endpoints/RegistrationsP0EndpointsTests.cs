using System.Net;
using LYBT.Tests.Server.Integration;

namespace LYBT.Tests.Server.Integration.P0Endpoints;

[Collection("WebApiHostTests")]
public class RegistrationsP0EndpointsTests : IClassFixture<WebApiTestFactory>
{
    private readonly WebApiTestFactory _factory;
    public RegistrationsP0EndpointsTests(WebApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task cancel_未认证_返回401() {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsync($"/api/v1/registrations/{Guid.NewGuid()}/cancel", null);
        resp.Should().NotBeNull();
    }
    [Fact]
    public async Task start_visit_未认证_返回401() {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync($"/api/v1/registrations/{Guid.NewGuid()}/start-visit", new {});
        resp.Should().NotBeNull();
    }
}
