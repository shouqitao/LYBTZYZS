using FluentAssertions;
using LYBT.WebAPI.IntegrationTests.Infrastructure;
using Xunit;

namespace LYBT.WebAPI.IntegrationTests;

/// <summary>
/// 基础设施验证测试
/// </summary>
/// <remarks>
/// 用于验证测试基础设施是否正常工作
/// </remarks>
public class InfrastructureTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public InfrastructureTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// 验证 Web 应用程序工厂可以正常创建
    /// </summary>
    [Fact]
    public void Factory_ShouldCreateSuccessfully()
    {
        // Arrange & Act
        var factory = _factory;

        // Assert
        factory.Should().NotBeNull();
        factory.TestDatabaseName.Should().NotBeNullOrWhiteSpace();
        factory.TestDatabaseName.Should().StartWith("LYBT_IntegrationTest_");
    }

    /// <summary>
    /// 验证可以创建 HTTP 客户端
    /// </summary>
    [Fact]
    public void Factory_ShouldCreateHttpClient()
    {
        // Act
        var client = _factory.CreateClient();

        // Assert
        client.Should().NotBeNull();
        client.BaseAddress.Should().NotBeNull();
    }

    /// <summary>
    /// 验证健康检查端点可访问
    /// </summary>
    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
