using System.Net;
using System.Text.Json;
using FluentAssertions;
using LYBT.Tests.Server.Integration.Fixtures;

namespace LYBT.Tests.Server.Integration.Compatibility;

/// <summary>
/// API 响应契约测试 - 验证所有端点遵循标准 ApiResponse envelope 格式。
/// 迁移自 LYBT.Server.CompatibilityTests，使用 WebApiFixture 替代自建 WebApplicationFactory。
/// </summary>
[Collection("ServerIntegration")]
public class ApiResponseContractTests
{
    private readonly WebApiFixture _fixture;

    public ApiResponseContractTests(WebApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("/api/v1/users")]
    [InlineData("/api/v1/patients")]
    [InlineData("/api/v1/herbs")]
    [InlineData("/api/v1/formulas")]
    public async Task AllListEndpoints_ShouldReturn_StandardApiResponseFormat(string endpoint)
    {
        // Act
        var response = await _fixture.AdminClient.GetAsync($"{endpoint}?page=1&pageSize=5");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        AssertStandardApiResponseFormat(content);
    }

    [Fact]
    public async Task UnauthorizedRequest_ShouldReturn401()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _fixture.AnonymousClient.GetAsync($"/api/v1/users/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// 验证标准 ApiResponse envelope 格式: { success: bool, data: any, message: string }
    /// </summary>
    private static void AssertStandardApiResponseFormat(string content)
    {
        var apiResponse = JsonSerializer.Deserialize<JsonElement>(content);

        apiResponse.TryGetProperty("success", out var successProperty)
            .Should().BeTrue("ApiResponse 应包含 'success' 字段");
        apiResponse.TryGetProperty("data", out _)
            .Should().BeTrue("ApiResponse 应包含 'data' 字段");
        apiResponse.TryGetProperty("message", out _)
            .Should().BeTrue("ApiResponse 应包含 'message' 字段");

        (successProperty.ValueKind == JsonValueKind.True ||
         successProperty.ValueKind == JsonValueKind.False)
            .Should().BeTrue("'success' 字段应为布尔类型");
    }
}
