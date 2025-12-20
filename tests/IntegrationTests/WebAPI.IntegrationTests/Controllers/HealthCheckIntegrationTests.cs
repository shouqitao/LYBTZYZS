using FluentAssertions;
using LYBT.Tests.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.IntegrationTests.WebAPI.Controllers;

/// <summary>
/// HealthController集成测试
/// OpenSpec: optimize-integration-tests - Phase 2.3
/// 测试健康检查API端点
/// </summary>
public class HealthCheckIntegrationTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    private const string BaseUrl = "/api/v1/health";

    public HealthCheckIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Basic Health Check Tests

    [Fact]
    public async Task Get_ShouldReturnHealthyStatus()
    {
        // Arrange - 基础健康检查允许匿名访问，无需认证
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {content}");

        content.Should().NotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        root.GetProperty("status").GetString().Should().Be("Healthy");
        root.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Get_WithoutAuthentication_ShouldSucceed()
    {
        // Arrange - 确保没有认证头
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync(BaseUrl);

        // Assert - AllowAnonymous端点应该返回200
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Ping Endpoint Tests

    [Fact]
    public async Task Ping_ShouldReturnPong()
    {
        // Arrange - Ping端点允许匿名访问
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync($"{BaseUrl}/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        root.GetProperty("message").GetString().Should().Be("pong");
        root.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Ping_WithoutAuthentication_ShouldSucceed()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync($"{BaseUrl}/ping");

        // Assert - AllowAnonymous端点应该返回200
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Detailed Health Check Tests

    [Fact]
    public async Task GetDetailedHealth_WithValidToken_ShouldReturnDetailedInfo()
    {
        // Arrange - 详细健康检查需要认证
        SetAuthorizationHeader();

        // Act
        var response = await Client.GetAsync($"{BaseUrl}/details");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // 验证响应结构
        root.TryGetProperty("status", out var statusProp).Should().BeTrue();
        statusProp.GetString().Should().BeOneOf("Healthy", "Degraded");

        root.TryGetProperty("timestamp", out _).Should().BeTrue();

        // 验证数据库检查信息
        root.TryGetProperty("database", out var dbProp).Should().BeTrue();
        dbProp.TryGetProperty("status", out var dbStatusProp).Should().BeTrue();
        dbStatusProp.GetString().Should().BeOneOf("Healthy", "Degraded", "Unhealthy");
        dbProp.TryGetProperty("duration", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetDetailedHealth_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange - 移除认证头
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync($"{BaseUrl}/details");

        // Assert - 需要认证的端点应该返回401
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDetailedHealth_ShouldIncludeDatabaseCheck()
    {
        // Arrange
        SetAuthorizationHeader();

        // Act
        var response = await Client.GetAsync($"{BaseUrl}/details");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // 验证数据库检查结果存在
        root.TryGetProperty("database", out var dbProp).Should().BeTrue();

        // 验证数据库检查包含duration（响应时间）
        dbProp.TryGetProperty("duration", out var durationProp).Should().BeTrue();
        durationProp.GetInt64().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetDetailedHealth_WhenHealthy_ShouldReturn200()
    {
        // Arrange
        SetAuthorizationHeader();

        // Act
        var response = await Client.GetAsync($"{BaseUrl}/details");
        var content = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(content);
        var status = doc.RootElement.GetProperty("status").GetString();

        // Assert
        if (status == "Healthy")
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        else
        {
            // Degraded状态返回503
            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }
    }

    #endregion

    #region Response Format Tests

    [Fact]
    public async Task HealthEndpoints_ShouldReturnJsonContentType()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync(BaseUrl);

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task HealthEndpoints_ShouldReturnValidJsonStructure()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync(BaseUrl);
        var content = await response.Content.ReadAsStringAsync();

        // Assert - 验证是有效的JSON
        var parseAction = () => JsonDocument.Parse(content);
        parseAction.Should().NotThrow();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Get_MultipleRequests_ShouldAllSucceed()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;
        var requestCount = 5;

        // Act & Assert
        for (int i = 0; i < requestCount; i++)
        {
            var response = await Client.GetAsync(BaseUrl);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Ping_ShouldRespondQuickly()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = null;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await Client.GetAsync($"{BaseUrl}/ping");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Ping端点应该在1秒内响应
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        _output.WriteLine($"Ping response time: {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Helper Methods

    private void SetAuthorizationHeader()
    {
        var token = GenerateTestToken();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    #endregion
}
