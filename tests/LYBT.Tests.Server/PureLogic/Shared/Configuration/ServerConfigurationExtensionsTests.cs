using FluentAssertions;
using LYBT.Shared.Configuration.Extensions;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.Shared.Configuration;

/// <summary>
/// ServerConfigurationExtensions 单元测试
/// </summary>
public class ServerConfigurationExtensionsTests
{
    [Fact]
    public void AddLybtServerConfiguration_RegistersJwtOptions()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<JwtOptions>>();
        options.Should().NotBeNull();
        options!.Value.Should().NotBeNull();
        options.Value.SecretKey.Should().Be("dGVzdC1zZWNyZXQta2V5LXRoYXQtaXMtbG9uZy1lbm91Z2gtZm9yLXRlc3Rpbmc=");
    }

    [Fact]
    public void AddLybtServerConfiguration_RegistersDatabaseOptions()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<DatabaseOptions>>();
        options.Should().NotBeNull();
        options!.Value.Should().NotBeNull();
        options.Value.ConnectionString.Should().Be("Server=localhost;Database=LYBTZYZS_Test;");
    }

    [Fact]
    public void AddLybtServerConfiguration_RegistersSecurityOptions()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<SecurityOptions>>();
        options.Should().NotBeNull();
        options!.Value.Should().NotBeNull();
    }

    [Fact]
    public void AddLybtServerConfiguration_RegistersSessionOptions()
    {
        // Arrange
        var configuration = CreateTestConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<SessionOptions>>();
        options.Should().NotBeNull();
        options!.Value.Should().NotBeNull();
    }

    private static IConfiguration CreateTestConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "dGVzdC1zZWNyZXQta2V5LXRoYXQtaXMtbG9uZy1lbm91Z2gtZm9yLXRlc3Rpbmc=",
            ["Jwt:Issuer"] = "LYBT.WebAPI",
            ["Jwt:Audience"] = "LYBT.Client",
            ["Jwt:AccessTokenExpirationMinutes"] = "30",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Database:ConnectionString"] = "Server=localhost;Database=LYBTZYZS_Test;",
            ["Database:EnableRetryOnFailure"] = "true",
            ["Security:RateLimiting:EnableRateLimiting"] = "false",
            ["Session:SessionTimeoutMinutes"] = "30"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }
}
