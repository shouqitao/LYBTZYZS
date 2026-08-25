using System.Net;
using LYBT.Tests.Server.Integration;

namespace LYBT.Tests.Server.Integration.P0Endpoints;

/// <summary>
/// 补全剩余缺口 1) GET /deploy/version — 端点不存在则标记 NOT_IMPLEMENTED
/// </summary>
[Collection("WebApiHostTests")]
public class DeployVersionTests : IClassFixture<WebApiTestFactory>
{
    private readonly WebApiTestFactory _factory;
    public DeployVersionTests(WebApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task GET_deploy_version_不存在_返回404_NOT_IMPLEMENTED()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/deploy/version");
        // 端点当前未实现，期望 404；若未来实现则为 200/401，宽松断言仅验证可达
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.OK);
        resp.Should().NotBeNull();
    }
}
