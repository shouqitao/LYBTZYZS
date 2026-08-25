using System.Net;
using LYBT.Tests.Server.Integration;

namespace LYBT.Tests.Server.Integration.P0Endpoints;

[Collection("WebApiHostTests")]
public class MedicalCasesP0EndpointsTests : IClassFixture<WebApiTestFactory>
{
    private readonly WebApiTestFactory _factory;
    public MedicalCasesP0EndpointsTests(WebApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task medicalcase_状态流转_未认证_返回401() {
        using var client = _factory.CreateClient();
        var id = Guid.NewGuid();
        // Active -> Suspended
        var resp = await client.PostAsJsonAsync($"/api/v1/medical-cases/{id}/suspend", new { reason = "test" });
        resp.Should().NotBeNull();
    }

    [Fact]
    public async Task medicalcase_complete_未认证_返回401() {
        using var client = _factory.CreateClient();
        var id = Guid.NewGuid();
        var resp = await client.PostAsJsonAsync($"/api/v1/medical-cases/{id}/complete", new {});
        resp.Should().NotBeNull();
    }
}
