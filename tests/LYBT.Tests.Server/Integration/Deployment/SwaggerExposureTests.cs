using System.Net;
using LYBT.Tests.Server.Integration;
using Xunit;

namespace LYBT.Tests.Server.Integration.Deployment;

/// <summary>
/// B-16: 生产环境 Swagger 暴露面契约——默认关闭（appsettings.Production.json Swagger:Enabled=false）、
/// 关闭时 /swagger 不豁免 CSP、下载主页不显示 API 文档入口（防死链）。
/// 需在线启用时注入环境变量 Swagger__Enabled=true（测试发布流程——见 06-operations/01-deployment.md）。
/// </summary>
[Collection("EnvIsolated")]
public class SwaggerExposureTests : IClassFixture<ProductionWebApiTestFactory>
{
    private readonly ProductionWebApiTestFactory _factory;

    public SwaggerExposureTests(ProductionWebApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Production_Swagger_DisabledByDefault_WithoutCspRelaxation()
    {
        using var client = _factory.CreateClient();

        var document = await client.GetAsync("/swagger/v1/swagger.json");
        document.StatusCode.Should().NotBe(HttpStatusCode.OK, "生产默认关闭 Swagger（Swagger:Enabled 默认 false）");

        var ui = await client.GetAsync("/swagger/index.html");
        ui.StatusCode.Should().NotBe(HttpStatusCode.OK, "生产默认不暴露 SwaggerUI");

        ui.Headers.TryGetValues("Content-Security-Policy", out var csp).Should().BeTrue("所有响应都应带 CSP 头");
        csp!.First().Should().NotContain("unsafe-eval", "Swagger 关闭时 /swagger 不享受 CSP 豁免");

        var home = await client.GetAsync("/");
        home.StatusCode.Should().Be(HttpStatusCode.OK);
        (await home.Content.ReadAsStringAsync())
            .Should().NotContain("href=\"/swagger", "Swagger 关闭时下载主页不应提供 API 文档入口");
    }
}
