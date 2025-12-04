using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.WebAPI.IntegrationTests.Middleware;

/// <summary>
/// CorrelationId中间件集成测试
/// refactor-logging-system: Task 4.2
/// 验证X-Correlation-ID在请求响应链中的正确传递
/// </summary>
public class CorrelationIdMiddlewareIntegrationTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    public CorrelationIdMiddlewareIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region X-Correlation-ID Header Tests

    [Fact]
    public async Task Request_WithCorrelationIdHeader_ShouldReturnSameCorrelationIdInResponse()
    {
        // Arrange
        var expectedCorrelationId = "test-correlation-12345";
        _output.WriteLine($"测试场景: 请求带X-Correlation-ID头，响应应返回相同的ID");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/diagnostics/health");
        request.Headers.Add("X-Correlation-ID", expectedCorrelationId);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        _output.WriteLine($"响应状态码: {response.StatusCode}");

        // 验证响应头包含CorrelationId
        response.Headers.TryGetValues("X-Correlation-ID", out var correlationIds).Should().BeTrue(
            "响应头应包含X-Correlation-ID");

        var returnedCorrelationId = correlationIds!.First();
        _output.WriteLine($"返回的CorrelationId: {returnedCorrelationId}");

        returnedCorrelationId.Should().Be(expectedCorrelationId);
    }

    [Fact]
    public async Task Request_WithoutCorrelationIdHeader_ShouldGenerateNewCorrelationId()
    {
        // Arrange
        _output.WriteLine("测试场景: 请求不带X-Correlation-ID头，应自动生成新ID");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/diagnostics/health");
        // 不添加X-Correlation-ID头

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        _output.WriteLine($"响应状态码: {response.StatusCode}");

        // 验证响应头包含自动生成的CorrelationId
        response.Headers.TryGetValues("X-Correlation-ID", out var correlationIds).Should().BeTrue(
            "响应头应包含自动生成的X-Correlation-ID");

        var generatedCorrelationId = correlationIds!.First();
        _output.WriteLine($"生成的CorrelationId: {generatedCorrelationId}");

        generatedCorrelationId.Should().NotBeNullOrEmpty();
        // 验证格式: LYBT-yyyyMMddHHmmssSSS-XXXX
        generatedCorrelationId.Should().StartWith("LYBT-");
    }

    [Fact]
    public async Task MultipleRequests_WithoutCorrelationIdHeader_ShouldGenerateDifferentCorrelationIds()
    {
        // Arrange
        _output.WriteLine("测试场景: 多次请求应生成不同的CorrelationId");

        // Act
        var request1 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/diagnostics/health");
        var response1 = await Client.SendAsync(request1);
        response1.Headers.TryGetValues("X-Correlation-ID", out var ids1);

        var request2 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/diagnostics/health");
        var response2 = await Client.SendAsync(request2);
        response2.Headers.TryGetValues("X-Correlation-ID", out var ids2);

        // Assert
        var correlationId1 = ids1!.First();
        var correlationId2 = ids2!.First();

        _output.WriteLine($"请求1 CorrelationId: {correlationId1}");
        _output.WriteLine($"请求2 CorrelationId: {correlationId2}");

        correlationId1.Should().NotBe(correlationId2);
    }

    #endregion

    #region CorrelationId in Error Responses

    [Fact]
    public async Task NotFoundResponse_ShouldIncludeCorrelationIdInResponseBody()
    {
        // Arrange
        var expectedCorrelationId = "error-test-correlation";
        var nonExistentId = Guid.NewGuid();
        _output.WriteLine($"测试场景: 404响应应在响应体中包含CorrelationId");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/patients/{nonExistentId}");
        request.Headers.Add("X-Correlation-ID", expectedCorrelationId);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"响应内容: {content}");

        // 验证响应体包含correlationId字段
        var problemDetails = JsonSerializer.Deserialize<JsonElement>(content);

        if (problemDetails.TryGetProperty("correlationId", out var correlationId))
        {
            correlationId.GetString().Should().Be(expectedCorrelationId);
            _output.WriteLine($"响应体中的CorrelationId: {correlationId.GetString()}");
        }
    }

    [Fact]
    public async Task BadRequestResponse_ShouldIncludeCorrelationId()
    {
        // Arrange
        var expectedCorrelationId = "validation-error-correlation";
        _output.WriteLine("测试场景: 400响应应包含CorrelationId");

        var invalidRequest = new { }; // 无效的请求体

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/patients")
        {
            Content = JsonContent.Create(invalidRequest)
        };
        request.Headers.Add("X-Correlation-ID", expectedCorrelationId);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"响应内容: {content}");

        // 验证响应体包含correlationId
        var problemDetails = JsonSerializer.Deserialize<JsonElement>(content);

        if (problemDetails.TryGetProperty("correlationId", out var correlationId))
        {
            correlationId.GetString().Should().Be(expectedCorrelationId);
        }

        // 验证响应头也包含CorrelationId
        response.Headers.TryGetValues("X-Correlation-ID", out var headerIds).Should().BeTrue();
        headerIds!.First().Should().Be(expectedCorrelationId);
    }

    #endregion

    #region CorrelationId Format Tests

    [Fact]
    public async Task GeneratedCorrelationId_ShouldFollowExpectedFormat()
    {
        // Arrange
        _output.WriteLine("测试场景: 自动生成的CorrelationId应遵循LYBT-timestamp-random格式");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/diagnostics/health");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.Headers.TryGetValues("X-Correlation-ID", out var correlationIds).Should().BeTrue();

        var correlationId = correlationIds!.First();
        _output.WriteLine($"生成的CorrelationId: {correlationId}");

        // 验证格式: LYBT-yyyyMMddHHmmssSSS-XXXX (总长度约27字符)
        correlationId.Should().MatchRegex(@"^LYBT-\d{17}-[A-F0-9]{4}$",
            "CorrelationId应遵循LYBT-yyyyMMddHHmmssSSS-XXXX格式");
    }

    [Fact]
    public async Task CustomCorrelationId_ShouldBePreservedExactly()
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
            _output.WriteLine($"测试自定义CorrelationId: {customId}");

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/diagnostics/health");
            request.Headers.Add("X-Correlation-ID", customId);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            response.Headers.TryGetValues("X-Correlation-ID", out var returnedIds).Should().BeTrue();
            returnedIds!.First().Should().Be(customId, $"自定义CorrelationId '{customId}' 应被完整保留");
        }
    }

    #endregion

    #region Cross-Request Consistency Tests

    [Fact]
    public async Task SameCorrelationId_AcrossMultipleAPICalls_ShouldBeTraceable()
    {
        // Arrange
        var sharedCorrelationId = $"trace-{Guid.NewGuid():N}";
        _output.WriteLine($"测试场景: 相同CorrelationId在多个API调用中应可追踪");
        _output.WriteLine($"共享CorrelationId: {sharedCorrelationId}");

        // Act - 发送多个请求使用相同的CorrelationId
        var request1 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/diagnostics/health");
        request1.Headers.Add("X-Correlation-ID", sharedCorrelationId);
        var response1 = await Client.SendAsync(request1);

        var request2 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/diagnostics/config");
        request2.Headers.Add("X-Correlation-ID", sharedCorrelationId);
        var response2 = await Client.SendAsync(request2);

        // Assert - 所有响应都应返回相同的CorrelationId
        response1.Headers.TryGetValues("X-Correlation-ID", out var ids1);
        response2.Headers.TryGetValues("X-Correlation-ID", out var ids2);

        ids1!.First().Should().Be(sharedCorrelationId);
        ids2!.First().Should().Be(sharedCorrelationId);

        _output.WriteLine("所有响应都正确返回了相同的CorrelationId");
    }

    #endregion
}
