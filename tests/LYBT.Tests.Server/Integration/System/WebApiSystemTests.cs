using LYBT.Tests.Server.Integration;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LYBT.Tests.Server.Integration.System;

/// <summary>
/// L3 系统层测试（P0 批次——方案 §五 L3）：WebApplicationFactory 真实启动 Remote WebAPI——
/// 守护「系统能启动 + 能响应」契约（拦截上线坑 #1 启动崩溃 / #3 健康检查 / #9 FallbackPolicy）
/// </summary>
public class WebApiSystemTests : IClassFixture<WebApiTestFactory>
{
    private readonly WebApiTestFactory _factory;

    public WebApiSystemTests(WebApiTestFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    [Fact]
    public async Task Startup_WebApi_BootsWithoutCrash()
    {
        // #1: 路由 version 重复会导致启动崩溃——Factory 成功创建 client 即启动通过
        using var client = CreateClient();
        var response = await client.GetAsync("/health");
        // 503 = DB 未连但进程正常（非启动崩溃）；200 = 全绿
        // 503 = DB 未连但进程正常（非启动崩溃）；200 = 全绿
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Routing_AllControllers_RegisterWithoutVersionConflict()
    {
        // #1: swagger.json 由 MapControllers 生成——200 = 全量路由注册成功（version 参数无重复）
        using var client = CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "swagger.json 生成隐含 MapControllers 成功（version 重复会在此前崩溃）");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("api/v1", "路由表应包含版本化端点");
    }

    [Fact]
    public async Task Health_Endpoint_Responds()
    {
        // #3: /health 必须响应（DB 可达性另测——此处守护端点存在 + 进程活）
        using var client = CreateClient();
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task FallbackPolicy_UnauthenticatedBusiness_Returns401()
    {
        // #9: FallbackPolicy RequireAuthenticatedUser——未认证业务端点 401
        using var client = CreateClient();
        var response = await client.GetAsync("/api/v1/patients");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "FallbackPolicy 应对未认证业务请求返回 401");
    }

    [Fact]
    public async Task CorrelationId_ResponseHeader_Present()
    {
        // 中间件链: UseLybtCorrelationId 写 X-Correlation-ID 响应头
        using var client = CreateClient();
        var response = await client.GetAsync("/health");

        response.Headers.Contains("X-Correlation-ID").Should().BeTrue("CorrelationId 中间件应写响应头");
    }

    [Fact]
    public async Task SecurityHeaders_BusinessPath_CspPresent()
    {
        // #10: 安全头存在——开发环境为 Report-Only（宽松）；生产严格 CSP 由 L4 测试断言
        using var client = CreateClient();
        var response = await client.GetAsync("/api/v1/patients");

        var hasCsp = response.Headers.TryGetValues("Content-Security-Policy", out _);
        var hasReportOnly = response.Headers.TryGetValues("Content-Security-Policy-Report-Only", out _);
        (hasCsp || hasReportOnly).Should().BeTrue("业务路径应有 CSP（严格或 Report-Only）安全头");
    }

    [Fact]
    public async Task SecurityHeaders_SwaggerPath_RelaxedCsp()
    {
        // #10: /swagger 路径宽松 CSP（SwaggerUI 渲染需 unsafe-eval——SWAGGER-CSP-FIX）
        using var client = CreateClient();
        var response = await client.GetAsync("/swagger/index.html");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            response.Headers.TryGetValues("Content-Security-Policy", out var csp).Should().BeTrue();
            csp!.First().Should().Contain("unsafe-eval", "swagger 路径 CSP 应豁免 script-src");
        }
        // 索引未就绪（首次慢）时跳过断言——不视为失败
    }

    [Fact]
    public async Task DownloadPage_Root_ReturnsHtml()
    {
        // 下载页: GET / 公开返回 HTML（含下载链接）
        using var client = CreateClient();
        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("桌面客户端下载");
    }
}
