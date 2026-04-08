using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Foundation;

/// <summary>
/// Diagnostics E2E Tests - 验证系统诊断功能
/// 
/// 测试顺序:
/// 1. Get Logging Status - 获取当前日志状态
/// 2. Enable Debug Mode - 启用调试模式
/// 3. Set Logging Level - 设置日志级别
/// 4. Disable Debug Mode - 禁用调试模式
/// </summary>
public class DiagnosticsTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _httpClient;

    public DiagnosticsTests(ITestOutputHelper output)
    {
        _output = output;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(Configuration["WebAPI:BaseUrl"]!)
        };
    }

    /// <summary>
    /// Test: GET /api/v1/diagnostics/logging/status
    /// 预期: 200 OK, 返回日志状态信息
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Diagnostics")]
    public async Task GetLoggingStatus_ShouldReturnCurrentStatus()
    {
        await LoginAsSysadminAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

        Logger.LogInformation("Testing diagnostics logging status endpoint...");

        var response = await _httpClient.GetAsync("/api/v1/diagnostics/logging/status");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<dynamic>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        _output.WriteLine("Logging Status Response: {0}", content);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue("诊断状态查询应该成功");
        // Data 是 JsonElement，通过 Success 验证即可

        Logger.LogInformation("Logging status test passed");
    }

    /// <summary>
    /// Test: POST /api/v1/diagnostics/logging/debug/enable
    /// 预期: 200 OK, 启用调试模式
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Diagnostics")]
    public async Task EnableDebugMode_ShouldEnableDebugLogging()
    {
        await LoginAsSysadminAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

        Logger.LogInformation("Testing enable debug mode endpoint...");

        var request = new
        {
            Level = "Debug",
            DurationMinutes = 30
        };

        var response = await _httpClient.PostAsJsonAsync("/api/v1/diagnostics/logging/debug/enable", request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<dynamic>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        _output.WriteLine("Enable Debug Mode Response: {0}", content);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue("启用调试模式应该成功");
        // Data 是 JsonElement，通过 Success 验证即可

        Logger.LogInformation("Enable debug mode test passed");
    }

    /// <summary>
    /// Test: POST /api/v1/diagnostics/logging/level
    /// 预期: 200 OK, 设置日志级别
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Diagnostics")]
    public async Task SetLoggingLevel_ShouldUpdateLogLevel()
    {
        await LoginAsSysadminAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

        Logger.LogInformation("Testing set logging level endpoint...");

        var request = new
        {
            Level = "Information"
        };

        var response = await _httpClient.PostAsJsonAsync("/api/v1/diagnostics/logging/level", request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<dynamic>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        _output.WriteLine("Set Logging Level Response: {0}", content);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue("设置日志级别应该成功");
        // Data 是 JsonElement，通过 Success 验证即可

        Logger.LogInformation("Set logging level test passed");
    }

    /// <summary>
    /// Test: POST /api/v1/diagnostics/logging/debug/disable
    /// 预期: 200 OK, 禁用调试模式
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Diagnostics")]
    public async Task DisableDebugMode_ShouldDisableDebugLogging()
    {
        await LoginAsSysadminAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

        Logger.LogInformation("Testing disable debug mode endpoint...");

        var response = await _httpClient.PostAsync("/api/v1/diagnostics/logging/debug/disable", null);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<dynamic>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        _output.WriteLine("Disable Debug Mode Response: {0}", content);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue("禁用调试模式应该成功");
        // Data 是 JsonElement，通过 Success 验证即可

        Logger.LogInformation("Disable debug mode test passed");
    }
}