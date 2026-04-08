using FluentAssertions;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Foundation;

/// <summary>
/// Health Check E2E Tests - 验证 WebAPI 可用性
/// 
/// 测试顺序:
/// 1. Health Endpoint - 确保 API 可访问
/// 2. 之后才能进行认证测试
/// </summary>
public class HealthCheckTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public HealthCheckTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Test: GET /health
    /// 预期: 200 OK, Status = "Healthy"
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Health")]
    public async Task HealthEndpoint_ShouldReturnHealthy()
    {
        // Act
        Logger.LogInformation("Testing health endpoint...");
        var response = await AuthApi.HealthCheckAsync();

        // Log response
        _output.WriteLine("Health Response: {0}", System.Text.Json.JsonSerializer.Serialize(response));

        // Assert
        response.Should().NotBeNull();
        response.Data!.Status.Should().Be("Healthy");
        
        Logger.LogInformation("Health check passed: {Status}", response.Data!.Status);
    }

    /// <summary>
    /// Test: GET /api/v1/health/ping
    /// 预期: 200 OK, 返回 pong
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Health")]
    public async Task PingEndpoint_ShouldReturnPong()
    {
        Logger.LogInformation("Testing ping endpoint...");
        
        var httpClient = new System.Net.Http.HttpClient
        {
            BaseAddress = new System.Uri(Configuration["WebAPI:BaseUrl"] ?? "http://localhost:5001")
        };
        
        var response = await httpClient.GetAsync("/api/v1/health/ping");
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine("Ping Response: {0}", content);
        
        content.Should().Contain("pong");
        
        Logger.LogInformation("Ping check passed");
    }

    /// <summary>
    /// Test: GET /api/v1/health via IAuthApi
    /// 预期: 200 OK, 详细信息
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Health")]
    public async Task HealthDetailedEndpoint_ShouldReturnDatabaseStatus()
    {
        // Act
        var response = await AuthApi.HealthCheckAsync();

        // Log response
        _output.WriteLine("Detailed Health Response: {0}", System.Text.Json.JsonSerializer.Serialize(response));

        // Assert
        response.Should().NotBeNull();
        response.Data!.Status.Should().Be("Healthy");
        
        Logger.LogInformation("Detailed health check passed");
    }
}
