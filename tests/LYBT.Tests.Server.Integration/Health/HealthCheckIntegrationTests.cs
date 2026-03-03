using System.Text.Json;
using LYBT.Tests.Server.Integration.Fixtures;
using Xunit.Abstractions;

namespace LYBT.Tests.Server.Integration.Health;

/// <summary>
/// HealthController 集成测试 -- 基于实际端点重设计。
/// 端点:
///   GET /api/v1/health        [AllowAnonymous] -> { status, timestamp }
///   GET /api/v1/health/ping   [AllowAnonymous] -> { message, timestamp }
///   GET /api/v1/health/details [Authorize]      -> { status, timestamp, database: { status, duration, provider, ... } }
///
/// 注意: Health 端点返回原始 JSON，不使用 ApiResponse 包装。
/// 关键修复: 不修改 Fixture 共享客户端的 Authorization 头 (防止 AdminClient 污染)。
/// </summary>
[Collection("ServerIntegration")]
public class HealthCheckIntegrationTests
{
    private readonly WebApiFixture _fixture;
    private readonly ITestOutputHelper _output;
    private const string BaseUrl = "/api/v1/health";

    public HealthCheckIntegrationTests(WebApiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    #region GET /api/v1/health (基础健康检查)

    [Fact]
    public async Task Get_AnonymousAccess_ShouldReturnHealthyStatus()
    {
        // Act -- 使用 AnonymousClient，不污染 AdminClient
        var response = await _fixture.AnonymousClient.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        root.GetProperty("status").GetString().Should().Be("Healthy");
        root.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Get_AuthenticatedAccess_ShouldAlsoSucceed()
    {
        // Act -- 认证客户端也可以访问 AllowAnonymous 端点
        var response = await _fixture.AdminClient.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ShouldReturnJsonContentType()
    {
        // Act
        var response = await _fixture.AnonymousClient.GetAsync(BaseUrl);

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Get_ResponseShouldBeValidJson()
    {
        // Act
        var response = await _fixture.AnonymousClient.GetAsync(BaseUrl);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        var parseAction = () => JsonDocument.Parse(content);
        parseAction.Should().NotThrow();
    }

    #endregion

    #region GET /api/v1/health/ping

    [Fact]
    public async Task Ping_AnonymousAccess_ShouldReturnPong()
    {
        // Act
        var response = await _fixture.AnonymousClient.GetAsync($"{BaseUrl}/ping");

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
    public async Task Ping_ShouldRespondWithinOneSecond()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _fixture.AnonymousClient.GetAsync($"{BaseUrl}/ping");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        _output.WriteLine($"Ping response time: {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region GET /api/v1/health/details (详细健康检查)

    [Fact]
    public async Task Details_WithAuthentication_ShouldReturnDetailedInfo()
    {
        // Arrange -- 创建独立认证客户端，避免污染 Fixture 客户端
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            _fixture.AdminClient.DefaultRequestHeaders.Authorization;

        // Act
        var response = await client.GetAsync($"{BaseUrl}/details");

        // Assert -- 200 = Healthy, 503 = Degraded/Unhealthy
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {content}");

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.TryGetProperty("status", out var statusProp).Should().BeTrue();
        statusProp.GetString().Should().BeOneOf("Healthy", "Degraded", "Unhealthy");

        root.TryGetProperty("timestamp", out _).Should().BeTrue();

        root.TryGetProperty("database", out var dbProp).Should().BeTrue();
        dbProp.TryGetProperty("status", out _).Should().BeTrue();
        dbProp.TryGetProperty("duration", out var durationProp).Should().BeTrue();
        durationProp.GetInt64().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task Details_WithoutAuthentication_ShouldReturn401()
    {
        // Act
        var response = await _fixture.AnonymousClient.GetAsync($"{BaseUrl}/details");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Details_WhenHealthy_ShouldReturn200()
    {
        // Arrange
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            _fixture.AdminClient.DefaultRequestHeaders.Authorization;

        // Act
        var response = await client.GetAsync($"{BaseUrl}/details");
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
            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }
    }

    [Fact]
    public async Task Details_ShouldIncludeDatabaseProviderInfo()
    {
        // Arrange
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            _fixture.AdminClient.DefaultRequestHeaders.Authorization;

        // Act
        var response = await client.GetAsync($"{BaseUrl}/details");
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response: {content}");

        // Assert
        using var doc = JsonDocument.Parse(content);
        var dbProp = doc.RootElement.GetProperty("database");

        // SQL Server 环境下应包含 provider 信息
        dbProp.TryGetProperty("provider", out var providerProp).Should().BeTrue();
        providerProp.GetString().Should().Contain("SqlServer");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Get_MultipleRequests_ShouldAllSucceed()
    {
        // Act & Assert -- 连续 5 次请求都应成功
        for (var i = 0; i < 5; i++)
        {
            var response = await _fixture.AnonymousClient.GetAsync(BaseUrl);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    #endregion
}
