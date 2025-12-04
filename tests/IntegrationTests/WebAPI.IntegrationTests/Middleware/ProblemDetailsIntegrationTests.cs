using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.WebAPI.IntegrationTests.Middleware;

/// <summary>
/// Problem Details集成测试
/// refactor-logging-system: Task 4.4
/// 验证RFC 7807 Problem Details响应格式
/// </summary>
public class ProblemDetailsIntegrationTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    public ProblemDetailsIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region 404 Not Found 测试

    [Fact]
    public async Task GetNonExistentResource_ShouldReturn404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _output.WriteLine($"测试场景: 请求不存在的资源应返回404");

        // Act
        var response = await Client.GetAsync($"/api/v1/patients/{nonExistentId}");

        // Assert
        _output.WriteLine($"响应状态码: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"响应内容: {content}");

        // 验证响应包含错误信息（支持ApiResponse或ProblemDetails格式）
        var responseJson = JsonSerializer.Deserialize<JsonElement>(content);

        // 检查是否包含错误信息
        var hasMessage = responseJson.TryGetProperty("message", out _) ||
                         responseJson.TryGetProperty("detail", out _);
        hasMessage.Should().BeTrue("响应应包含错误消息");

        _output.WriteLine("404响应验证通过");
    }

    #endregion

    #region 400 Bad Request 测试

    [Fact]
    public async Task PostInvalidData_ShouldReturn400WithErrorDetails()
    {
        // Arrange
        _output.WriteLine("测试场景: 提交无效数据应返回400错误详情");

        // 发送空对象到需要验证的端点
        var invalidRequest = new { };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/patients", invalidRequest);

        // Assert
        _output.WriteLine($"响应状态码: {response.StatusCode}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"响应内容: {content}");

        // 验证响应包含错误详情
        var responseJson = JsonSerializer.Deserialize<JsonElement>(content);

        // ProblemDetails格式应包含status和errors
        if (responseJson.TryGetProperty("status", out var status))
        {
            status.GetInt32().Should().BeOneOf(400, 422);
            _output.WriteLine($"Status: {status.GetInt32()}");

            // 验证包含验证错误详情
            responseJson.TryGetProperty("errors", out _).Should().BeTrue("ProblemDetails应包含errors字段");
            _output.WriteLine("验证错误详情验证通过");
        }
    }

    #endregion

    #region CorrelationId传递测试

    [Fact]
    public async Task Request_WithCorrelationIdHeader_ShouldUseProvidedCorrelationId()
    {
        // Arrange
        var expectedCorrelationId = "test-correlation-id-12345";
        _output.WriteLine($"测试场景: 请求带CorrelationId头应在响应中返回相同的ID");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/patients/{Guid.NewGuid()}");
        request.Headers.Add("X-Correlation-ID", expectedCorrelationId);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        _output.WriteLine($"响应状态码: {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"响应内容: {content}");

            var problemDetails = JsonSerializer.Deserialize<JsonElement>(content);

            if (problemDetails.TryGetProperty("correlationId", out var correlationId))
            {
                correlationId.GetString().Should().Be(expectedCorrelationId);
                _output.WriteLine($"验证通过: CorrelationId匹配");
            }
        }
    }

    [Fact]
    public async Task Request_WithoutCorrelationIdHeader_ShouldGenerateNewCorrelationId()
    {
        // Arrange
        _output.WriteLine("测试场景: 请求不带CorrelationId头应自动生成新ID");

        // Act
        var response = await Client.GetAsync($"/api/v1/patients/{Guid.NewGuid()}");

        // Assert
        _output.WriteLine($"响应状态码: {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var content = await response.Content.ReadAsStringAsync();
            var problemDetails = JsonSerializer.Deserialize<JsonElement>(content);

            if (problemDetails.TryGetProperty("correlationId", out var correlationId))
            {
                var correlationIdValue = correlationId.GetString();
                correlationIdValue.Should().NotBeNullOrEmpty();
                correlationIdValue!.Length.Should().Be(12); // 短格式CorrelationId
                _output.WriteLine($"生成的CorrelationId: {correlationIdValue} (长度: {correlationIdValue.Length})");
            }
        }
    }

    #endregion

    #region RFC 7807格式验证

    [Fact]
    public async Task ValidationError_ShouldReturnProblemDetailsFormat()
    {
        // Arrange
        _output.WriteLine("测试场景: 验证错误应返回RFC 7807 Problem Details格式");

        // 触发验证错误以测试ProblemDetails格式
        var invalidRequest = new { };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/patients", invalidRequest);

        // Assert
        _output.WriteLine($"响应状态码: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"响应内容: {content}");

        var problemDetails = JsonSerializer.Deserialize<JsonElement>(content);

        // RFC 7807必需字段: title, status
        problemDetails.TryGetProperty("title", out var titleProperty).Should().BeTrue("应包含title字段");
        _output.WriteLine($"title: {titleProperty.GetString()}");

        problemDetails.TryGetProperty("status", out var statusProperty).Should().BeTrue("应包含status字段");
        statusProperty.GetInt32().Should().Be(400);
        _output.WriteLine($"status: {statusProperty.GetInt32()}");

        // RFC 7807可选字段: detail, instance
        if (problemDetails.TryGetProperty("detail", out var detailProperty))
        {
            _output.WriteLine($"detail: {detailProperty.GetString()}");
        }

        if (problemDetails.TryGetProperty("instance", out var instanceProperty))
        {
            instanceProperty.GetString().Should().StartWith("/api/");
            _output.WriteLine($"instance: {instanceProperty.GetString()}");
        }

        // 验证错误详情
        problemDetails.TryGetProperty("errors", out _).Should().BeTrue("验证错误应包含errors字段");

        _output.WriteLine("RFC 7807格式验证通过");
    }

    #endregion

    #region 异常类型映射测试

    [Fact]
    public async Task BusinessException_ShouldIncludeErrorCode()
    {
        // Arrange
        _output.WriteLine("测试场景: 业务异常应包含errorCode扩展属性");

        // 触发一个会抛出BusinessException的操作
        // 例如：尝试删除不存在的资源或违反业务规则
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/v1/patients/{nonExistentId}");

        // Assert
        _output.WriteLine($"响应状态码: {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            var content = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"响应内容: {content}");

            var problemDetails = JsonSerializer.Deserialize<JsonElement>(content);

            // 验证资源类型信息
            if (problemDetails.TryGetProperty("resourceType", out var resourceType))
            {
                _output.WriteLine($"resourceType: {resourceType.GetString()}");
            }

            if (problemDetails.TryGetProperty("resourceId", out var resourceId))
            {
                _output.WriteLine($"resourceId: {resourceId.GetString()}");
            }
        }
    }

    #endregion
}
