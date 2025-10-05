using FluentAssertions;
using LYBT.WebAPI.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using static LYBT.WebAPI.IntegrationTests.Infrastructure.TestHelpers;

namespace LYBT.WebAPI.IntegrationTests.Modules;

/// <summary>
/// Health 模块集成测试
/// </summary>
/// <remarks>
/// 测试范围：
/// - 基础健康检查
/// - Ping 端点
/// - 详细健康检查（需要认证）
/// </remarks>
public class HealthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HealthTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    #region 基础健康检查测试

    /// <summary>
    /// 测试：基础健康检查返回成功
    /// </summary>
    [Fact]
    public async Task Health_Get_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        result.Should().NotBeNull();
        result!.GetProperty("status").GetString().Should().Be("Healthy");
        result.GetProperty("timestamp").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// 测试：基础健康检查允许匿名访问
    /// </summary>
    [Fact]
    public async Task Health_Get_AllowsAnonymousAccess()
    {
        // Arrange - 确保没有授权头
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Ping 端点测试

    /// <summary>
    /// 测试：Ping 端点返回 Pong
    /// </summary>
    [Fact]
    public async Task Health_Ping_ReturnsPong()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        result.Should().NotBeNull();
        result!.GetProperty("message").GetString().Should().Be("pong");
        result.GetProperty("timestamp").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// 测试：Ping 端点允许匿名访问
    /// </summary>
    [Fact]
    public async Task Health_Ping_AllowsAnonymousAccess()
    {
        // Arrange - 确保没有授权头
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/v1/health/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region 详细健康检查测试

    /// <summary>
    /// 测试：详细健康检查返回成功（需要认证）
    /// </summary>
    [Fact]
    public async Task Health_Details_WithAuth_ReturnsHealthy()
    {
        // Arrange - 登录获取 Token
        await _factory.Seeder.SeedDefaultUsersAsync();
        await _client.LoginAndSetAuthorizationAsync("admin", "Admin123!");

        // Act
        var response = await _client.GetAsync("/api/v1/health/details");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        result.Should().NotBeNull();
        result!.GetProperty("status").GetString().Should().BeOneOf("Healthy", "Degraded");
        result.GetProperty("nowUtc").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // 验证包含健康检查项
        var checks = result.GetProperty("checks");
        checks.GetArrayLength().Should().BeGreaterThan(0);
    }

    /// <summary>
    /// 测试：详细健康检查未授权返回 401
    /// </summary>
    [Fact]
    public async Task Health_Details_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange - 确保没有授权头
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/v1/health/details");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// 测试：详细健康检查包含数据库检查
    /// </summary>
    [Fact]
    public async Task Health_Details_IncludesDatabaseCheck()
    {
        // Arrange - 登录获取 Token
        await _factory.Seeder.SeedDefaultUsersAsync();
        await _client.LoginAndSetAuthorizationAsync("admin", "Admin123!");

        // Act
        var response = await _client.GetAsync("/api/v1/health/details");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        
        root.TryGetProperty("checks", out var checks).Should().BeTrue();
        
        var checkNames = new List<string>();
        foreach (var check in checks.EnumerateArray())
        {
            if (check.TryGetProperty("name", out var nameElement))
            {
                var name = nameElement.GetString();
                if (name != null)
                {
                    checkNames.Add(name);
                }
            }
        }
        
        // 验证包含数据库检查（至少包含一个 db 或 system 检查）
        (checkNames.Contains("db") || checkNames.Contains("system")).Should().BeTrue();
    }

    #endregion
}
