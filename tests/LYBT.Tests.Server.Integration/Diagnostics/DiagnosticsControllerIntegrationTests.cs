using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Tests.Server.Integration.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Server.Integration.Diagnostics;

/// <summary>
/// DiagnosticsController集成测试
/// refactor-logging-system: Task 4.9
/// 测试日志级别管理API
/// </summary>
[Collection("ServerIntegration")]
public class DiagnosticsControllerIntegrationTests
{
    private readonly WebApiFixture _fixture;
    private readonly ITestOutputHelper _output;

    public DiagnosticsControllerIntegrationTests(WebApiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    #region 日志状态查询测试

    [Fact]
    public async Task GetLoggingStatus_ShouldReturnCurrentStatus()
    {
        // Arrange
        _output.WriteLine("测试场景: 获取当前日志级别状态");

        // Act
        var response = await _fixture.SysAdminClient.GetAsync("/api/v1/diagnostics/logging/status");

        // Assert
        _output.WriteLine($"响应状态码: {response.StatusCode}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"响应内容: {content}");

        var status = JsonSerializer.Deserialize<JsonElement>(content);

        // 验证返回的字段
        status.TryGetProperty("currentLevel", out _).Should().BeTrue("应包含currentLevel字段");
        status.TryGetProperty("defaultLevel", out _).Should().BeTrue("应包含defaultLevel字段");
        status.TryGetProperty("isDebugModeActive", out _).Should().BeTrue("应包含isDebugModeActive字段");

        _output.WriteLine("日志状态查询测试通过");
    }

    #endregion

    #region 调试模式测试

    [Fact]
    public async Task EnableDebugMode_WithValidRequest_ShouldSucceed()
    {
        // Arrange
        _output.WriteLine("测试场景: 启用调试模式");

        var request = new
        {
            level = "Debug",
            durationMinutes = 30
        };

        // Act
        var response = await _fixture.SysAdminClient.PostAsJsonAsync("/api/v1/diagnostics/logging/debug/enable", request);

        // Assert
        _output.WriteLine($"响应状态码: {response.StatusCode}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"响应内容: {content}");

        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.TryGetProperty("message", out var message).Should().BeTrue();
        message.GetString().Should().Contain("已启用");

        result.TryGetProperty("currentLevel", out _).Should().BeTrue();
        result.TryGetProperty("expiresAt", out _).Should().BeTrue();

        _output.WriteLine("启用调试模式测试通过");
    }

    [Fact]
    public async Task DisableDebugMode_ShouldRestoreDefaultLevel()
    {
        // Arrange
        _output.WriteLine("测试场景: 禁用调试模式恢复默认级别");

        // Act
        var response = await _fixture.SysAdminClient.PostAsync("/api/v1/diagnostics/logging/debug/disable", null);

        // Assert
        _output.WriteLine($"响应状态码: {response.StatusCode}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"响应内容: {content}");

        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.TryGetProperty("message", out var message).Should().BeTrue();
        message.GetString().Should().Contain("已禁用");

        _output.WriteLine("禁用调试模式测试通过");
    }

    #endregion

    #region 日志级别设置测试

    [Fact]
    public async Task SetLoggingLevel_WithValidLevel_ShouldSucceed()
    {
        // Arrange
        _output.WriteLine("测试场景: 设置有效的日志级别");

        var request = new { level = "Warning" };

        // Act
        var response = await _fixture.SysAdminClient.PostAsJsonAsync("/api/v1/diagnostics/logging/level", request);

        // Assert
        _output.WriteLine($"响应状态码: {response.StatusCode}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"响应内容: {content}");

        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.TryGetProperty("message", out var message).Should().BeTrue();
        message.GetString().Should().Contain("已更新");

        result.TryGetProperty("currentLevel", out var currentLevel).Should().BeTrue();
        currentLevel.GetString().Should().Be("Warning");

        _output.WriteLine("设置日志级别测试通过");
    }

    [Fact]
    public async Task SetLoggingLevel_WithInvalidLevel_ShouldReturnBadRequest()
    {
        // Arrange
        _output.WriteLine("测试场景: 设置无效的日志级别应返回400");

        var request = new { level = "InvalidLevel" };

        // Act
        var response = await _fixture.SysAdminClient.PostAsJsonAsync("/api/v1/diagnostics/logging/level", request);

        // Assert
        _output.WriteLine($"响应状态码: {response.StatusCode}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"响应内容: {content}");

        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.TryGetProperty("error", out var error).Should().BeTrue();
        error.GetString().Should().Contain("无效");

        // 应返回有效的日志级别列表
        result.TryGetProperty("validLevels", out _).Should().BeTrue();

        _output.WriteLine("无效日志级别测试通过");
    }

    [Fact]
    public async Task SetLoggingLevel_WithEmptyLevel_ShouldReturnBadRequest()
    {
        // Arrange
        _output.WriteLine("测试场景: 设置空的日志级别应返回400");

        var request = new { level = "" };

        // Act
        var response = await _fixture.SysAdminClient.PostAsJsonAsync("/api/v1/diagnostics/logging/level", request);

        // Assert
        _output.WriteLine($"响应状态码: {response.StatusCode}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        _output.WriteLine("空日志级别测试通过");
    }

    #endregion

    #region 权限验证测试

    [Fact]
    public async Task DiagnosticsEndpoints_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        _output.WriteLine("测试场景: 未认证用户访问诊断端点应返回401");

        // 创建一个没有认证头的客户端
        var unauthClient = _fixture.CreateClient();

        // Act
        var response = await unauthClient.GetAsync("/api/v1/diagnostics/logging/status");

        // Assert
        _output.WriteLine($"响应状态码: {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        _output.WriteLine("未授权访问测试通过");
    }

    #endregion
}
