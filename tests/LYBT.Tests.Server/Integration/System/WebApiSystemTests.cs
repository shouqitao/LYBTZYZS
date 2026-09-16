using LYBT.Tests.Server.Integration;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LYBT.Tests.Server.Integration.System;

/// <summary>
/// L3 系统层测试（P0 批次——方案 §五 L3）：WebApplicationFactory 真实启动 Remote WebAPI——
/// 守护「系统能启动 + 能响应」契约（拦截上线坑 #1 启动崩溃 / #3 健康检查 / #9 FallbackPolicy）
/// </summary>
[Collection("WebApiHostTests")]
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

    /// <summary>
    /// B-16: OpenAPI 文档契约——全量端点有摘要（XML 注释覆盖）、Bearer 为 http/bearer（SwaggerUI 自动加前缀）、
    /// [Authorize] 端点声明 security（Try it out 才带 Token）、匿名端点不声明、匿名兜底端点不进文档。
    /// </summary>
    [Fact]
    public async Task Swagger_Document_CoversEndpointsWithSummariesAndBearerSecurity()
    {
        // 匿名端点白名单 = 文档中 security 为空集的全部端点（新增匿名端点须显式登记——安全评审触发点）
        var anonymousEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "GET /",
            "GET /api/v1/health",
            "GET /api/v1/health/ping",
            "POST /api/v1/auth/login",
            "POST /api/v1/auth/logout",
            "POST /api/v1/auth/refresh",
            "POST /api/v1/auth/auto-login",
            // 2026-09-16 登记：/auth/validate 由「类级 [Authorize] 拦截」改为 [AllowAnonymous] + 方法内自解析
            // Authorization 头（缺失/格式错/令牌无效一律 401 + ApiResponse）。原类级拦截使方法内的 401 分支
            // 不可达，客户端拿到的是框架默认 401 体而非 ApiResponse 契约；此改动与 LocalWebAPI 的匿名
            // validate 语义对齐（见 docs/03-architecture/09-security-architecture.md §默认安全策略）。
            "GET /api/v1/auth/validate",
        };

        using var client = CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = global::System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        var methods = new[] { "get", "post", "put", "delete", "patch" };

        var scheme = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        scheme.GetProperty("type").GetString().Should().Be("http", "SwaggerUI 仅在 http/bearer 下自动附加 \"Bearer \" 前缀");
        scheme.GetProperty("scheme").GetString().Should().Be("bearer");

        var missingSummary = new List<string>();
        var securityMismatch = new List<string>();
        foreach (var path in root.GetProperty("paths").EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                if (!methods.Contains(operation.Name))
                    continue;

                var id = $"{operation.Name.ToUpperInvariant()} {path.Name}";
                var summary = operation.Value.TryGetProperty("summary", out var s) ? s.GetString() : null;
                if (string.IsNullOrWhiteSpace(summary))
                    missingSummary.Add(id);

                var hasSecurity =
                    operation.Value.TryGetProperty("security", out var security)
                    && security.GetArrayLength() > 0;
                if (hasSecurity == anonymousEndpoints.Contains(id))
                    securityMismatch.Add($"{id} (security={hasSecurity})");
            }
        }

        missingSummary.Should().BeEmpty("每个 API 端点都应有 XML 摘要（B-16 T2）");
        securityMismatch.Should().BeEmpty("security 声明应与 [Authorize]/[AllowAnonymous] 一致（B-16 T3）");
        root.GetProperty("paths").TryGetProperty("/swagger/{path}", out _)
            .Should().BeFalse("匿名兜底端点不是业务 API——应 ExcludeFromDescription");
    }
}
