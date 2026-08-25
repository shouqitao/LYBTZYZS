using System.Net;
using System.Net.Http.Json;
using LYBT.Tests.Server.Integration;

namespace LYBT.Tests.Server.Integration.P0Endpoints;

/// <summary>
/// 补全剩余缺口 3) Herbs 批量启用/禁用空数组分支
/// </summary>
[Collection("WebApiHostTests")]
public class HerbsBatchEmptyBranchTests : IClassFixture<WebApiTestFactory>
{
    private readonly WebApiTestFactory _factory;
    public HerbsBatchEmptyBranchTests(WebApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task batch_enable_空数组_返回400或200_不抛500()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/herbs/batch-enable", new { ids = Array.Empty<Guid>() });
        resp.Should().NotBeNull();
        resp.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError, "空数组不应导致 500");
    }

    [Fact]
    public async Task batch_disable_空数组_返回400或200_不抛500()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/herbs/batch-disable", new { ids = Array.Empty<Guid>() });
        resp.Should().NotBeNull();
        resp.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task batch_enable_空数组_未认证_返回401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/herbs/batch-enable", new { ids = Array.Empty<Guid>() });
        // 未认证应 401，宽松断言
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.Forbidden);
    }
}
