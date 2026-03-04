using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.WebAPI;

/// <summary>
/// DatabaseServiceCollectionExtensions 测试
/// A1-03: 验证连接字符串缺失时抛出 InvalidOperationException
/// </summary>
public class DatabaseServiceCollectionExtensionsTests
{
    [Fact]
    public void RegisterInfrastructureServices_WithoutConnectionString_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // 不提供任何连接字符串配置
                ["Database:RetryPolicy:MaxRetryCount"] = "3",
                ["Database:RetryPolicy:MaxDelayMs"] = "5000"
            })
            .Build();

        // Act
        var act = () => LYBT.WebAPI.Extensions.DatabaseServiceCollectionExtensions
            .RegisterInfrastructureServices(services, configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*数据库连接字符串未配置*");
    }

    [Fact]
    public void RegisterInfrastructureServices_WithConnectionStringInDatabaseSection_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Server=test;Database=test;",
                ["Database:RetryPolicy:MaxRetryCount"] = "3",
                ["Database:RetryPolicy:MaxDelayMs"] = "5000"
            })
            .Build();

        // Act - 不应抛异常（后续 DbContext 注册可能因缺少完整配置而有其他问题，但连接串检查应通过）
        var act = () => LYBT.WebAPI.Extensions.DatabaseServiceCollectionExtensions
            .RegisterInfrastructureServices(services, configuration);

        // Assert - 不应因连接字符串检查而抛出 InvalidOperationException
        act.Should().NotThrow<InvalidOperationException>();
    }
}
