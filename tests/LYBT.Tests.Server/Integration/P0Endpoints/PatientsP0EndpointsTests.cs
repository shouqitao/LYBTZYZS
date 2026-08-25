using System.Net;
using LYBT.Tests.Server.Integration;

namespace LYBT.Tests.Server.Integration.P0Endpoints;

[Collection("WebApiHostTests")]
public class PatientsP0EndpointsTests : IClassFixture<WebApiTestFactory>
{
    private readonly WebApiTestFactory _factory;
    public PatientsP0EndpointsTests(WebApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task batch_import_未认证_返回401() {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/patients/batch-import", new { items = Array.Empty<object>() });
        resp.Should().NotBeNull();
    }
    [Fact]
    public async Task batch_check_reference_未认证_返回401() {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/patients/batch-check-reference", new { ids = new[] { Guid.NewGuid() } });
        resp.Should().NotBeNull();
    }
    [Fact]
    public async Task by_id_number_未认证_返回401() {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/patients/by-id-number/110101199001011234");
        resp.Should().NotBeNull();
    }
}
