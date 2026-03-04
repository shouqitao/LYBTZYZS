using System.Net;
using FluentAssertions;
using LYBT.Tests.Server.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Server.Middleware;

/// <summary>
/// CorrelationId 中间件集成测试 -- 基于实际中间件重设计。
/// 实际行为 (CorrelationIdMiddleware.cs):
///   - Header: X-Correlation-ID
///   - 自动生成格式: Guid.NewGuid().ToString("N")[..12] = 12 字符十六进制
///   - 支持 traceparent 回退 (W3C Trace Context)
///   - 响应头始终包含 X-Correlation-ID
///
/// 测试端点: /api/v1/health (AllowAnonymous，确保存在且无副作用)
/// </summary>
public sealed class CorrelationIdMiddlewareIntegrationTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    // 使用 Health 端点测试中间件行为 (AllowAnonymous, 一定存在)
    private const string TestEndpoint = "/api/v1/health";

    public CorrelationIdMiddlewareIntegrationTests(ServerFixture fixture, ITestOutputHelper output) : base(fixture)
    {
        _output = output;
    }

    #region 自动生成 CorrelationId

    [Fact]
    public async Task Request_WithoutCorrelationId_ShouldAutoGenerate12CharHex()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, TestEndpoint);
        // 不添加任何 Correlation 相关 Header

        // Act
        var response = await AnonymousClient.SendAsync(request);

        // Assert
        response.Headers.TryGetValues("X-Correlation-ID", out var values).Should().BeTrue(
            "响应头应包含自动生成的 X-Correlation-ID");

        var correlationId = values!.First();
        _output.WriteLine($"Auto-generated CorrelationId: {correlationId}");

        // 格式: 12 字符十六进制 (Guid.NewGuid().ToString("N")[..12])
        correlationId.Should().MatchRegex(@"^[a-f0-9]{12}$",
            "自动生成的 CorrelationId 应为 12 字符十六进制");
    }

    [Fact]
    public async Task MultipleRequests_ShouldGenerateDifferentCorrelationIds()
    {
        // Act
        var request1 = new HttpRequestMessage(HttpMethod.Get, TestEndpoint);
        var response1 = await AnonymousClient.SendAsync(request1);
        response1.Headers.TryGetValues("X-Correlation-ID", out var ids1);

        var request2 = new HttpRequestMessage(HttpMethod.Get, TestEndpoint);
        var response2 = await AnonymousClient.SendAsync(request2);
        response2.Headers.TryGetValues("X-Correlation-ID", out var ids2);

        // Assert
        var id1 = ids1!.First();
        var id2 = ids2!.First();

        _output.WriteLine($"Request 1 CorrelationId: {id1}");
        _output.WriteLine($"Request 2 CorrelationId: {id2}");

        id1.Should().NotBe(id2, "不同请求应生成不同的 CorrelationId");
    }

    #endregion

    #region 客户端传入 X-Correlation-ID

    [Fact]
    public async Task Request_WithCustomCorrelationId_ShouldPreserveInResponse()
    {
        // Arrange
        var customId = "test-correlation-12345";

        var request = new HttpRequestMessage(HttpMethod.Get, TestEndpoint);
        request.Headers.Add("X-Correlation-ID", customId);

        // Act
        var response = await AnonymousClient.SendAsync(request);

        // Assert
        response.Headers.TryGetValues("X-Correlation-ID", out var values).Should().BeTrue();
        values!.First().Should().Be(customId, "客户端传入的 CorrelationId 应被完整保留");
    }

    [Fact]
    public async Task Request_WithVariousCustomFormats_ShouldAllBePreserved()
    {
        // Arrange
        var customFormats = new[]
        {
            "my-custom-id",
            "123456789",
            "abc-def-ghi-jkl",
            "UPPERCASE-ID-12345"
        };

        foreach (var customId in customFormats)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, TestEndpoint);
            request.Headers.Add("X-Correlation-ID", customId);

            // Act
            var response = await AnonymousClient.SendAsync(request);

            // Assert
            response.Headers.TryGetValues("X-Correlation-ID", out var values).Should().BeTrue();
            values!.First().Should().Be(customId, $"自定义 CorrelationId '{customId}' 应被完整保留");

            _output.WriteLine($"Preserved: {customId}");
        }
    }

    #endregion

    #region traceparent 回退

    [Fact]
    public async Task Request_WithTraceparent_ShouldUseAsCorrelationId()
    {
        // Arrange -- W3C Trace Context: traceparent header 优先于 X-Correlation-ID
        var traceparent = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";

        var request = new HttpRequestMessage(HttpMethod.Get, TestEndpoint);
        request.Headers.Add("traceparent", traceparent);

        // Act
        var response = await AnonymousClient.SendAsync(request);

        // Assert
        response.Headers.TryGetValues("X-Correlation-ID", out var values).Should().BeTrue();
        values!.First().Should().Be(traceparent, "traceparent 应被用作 CorrelationId");
    }

    [Fact]
    public async Task Request_WithBothHeaders_TraceparentShouldTakePrecedence()
    {
        // Arrange -- 当 traceparent 和 X-Correlation-ID 同时存在时，traceparent 优先
        var traceparent = "00-abcdef1234567890abcdef1234567890-1234567890abcdef-01";
        var customId = "should-be-ignored";

        var request = new HttpRequestMessage(HttpMethod.Get, TestEndpoint);
        request.Headers.Add("traceparent", traceparent);
        request.Headers.Add("X-Correlation-ID", customId);

        // Act
        var response = await AnonymousClient.SendAsync(request);

        // Assert
        response.Headers.TryGetValues("X-Correlation-ID", out var values).Should().BeTrue();
        values!.First().Should().Be(traceparent,
            "当 traceparent 和 X-Correlation-ID 同时存在时，traceparent 优先");
    }

    #endregion

    #region 错误响应也应包含 CorrelationId

    [Fact]
    public async Task ErrorResponse_ShouldStillIncludeCorrelationIdHeader()
    {
        // Arrange -- 用不存在的患者ID触发 404
        var admin = await LoginAsAdminAsync();
        var customId = "error-test-correlation";
        var nonExistentId = Guid.NewGuid();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/patients/{nonExistentId}");
        request.Headers.Add("X-Correlation-ID", customId);
        request.Headers.Authorization = admin.DefaultRequestHeaders.Authorization;

        // Act
        var response = await AnonymousClient.SendAsync(request);

        // Assert -- 无论状态码如何，响应头都应包含 X-Correlation-ID
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        response.Headers.TryGetValues("X-Correlation-ID", out var values).Should().BeTrue(
            "错误响应也应包含 X-Correlation-ID 头");
        values!.First().Should().Be(customId);

        _output.WriteLine($"404 response includes CorrelationId: {customId}");
    }

    [Fact]
    public async Task UnauthorizedResponse_ShouldIncludeCorrelationIdHeader()
    {
        // Arrange -- 匿名访问需要认证的端点
        var customId = "auth-test-correlation";

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/health/details");
        request.Headers.Add("X-Correlation-ID", customId);

        // Act
        var response = await AnonymousClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        response.Headers.TryGetValues("X-Correlation-ID", out var values).Should().BeTrue(
            "401 响应也应包含 X-Correlation-ID 头");
        values!.First().Should().Be(customId);
    }

    #endregion
}
