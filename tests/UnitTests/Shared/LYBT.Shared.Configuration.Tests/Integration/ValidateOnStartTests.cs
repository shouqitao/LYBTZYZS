using FluentAssertions;
using LYBT.Shared.Configuration.Extensions;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace LYBT.Shared.Configuration.Tests.Integration;

/// <summary>
/// ValidateOnStart 验证测试
/// </summary>
public class ValidateOnStartTests
{
    #region JwtOptions 验证失败测试

    [Fact]
    public void ValidateOnStart_InvalidJwtSecretKey_ThrowsOptionsValidationException()
    {
        // Arrange - 使用无效的Base64密钥
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "not-valid-base64!!!",
            ["Jwt:Issuer"] = "LYBT.WebAPI",
            ["Jwt:Audience"] = "LYBT.Client",
            ["Jwt:AccessTokenExpirationMinutes"] = "30",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Database:ConnectionString"] = "Server=localhost;Database=Test;"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - 访问Options时应该抛出验证异常
        var action = () => serviceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;
        action.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void ValidateOnStart_ShortJwtSecretKey_ThrowsOptionsValidationException()
    {
        // Arrange - 使用太短的密钥 (少于32字节)
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "c2hvcnQ=", // "short" in Base64, 只有5字节
            ["Jwt:Issuer"] = "LYBT.WebAPI",
            ["Jwt:Audience"] = "LYBT.Client",
            ["Jwt:AccessTokenExpirationMinutes"] = "30",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Database:ConnectionString"] = "Server=localhost;Database=Test;"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var action = () => serviceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;
        action.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void ValidateOnStart_AccessTokenLongerThanRefreshToken_ThrowsOptionsValidationException()
    {
        // Arrange - AccessToken过期时间超过RefreshToken
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
            ["Jwt:Issuer"] = "LYBT.WebAPI",
            ["Jwt:Audience"] = "LYBT.Client",
            ["Jwt:AccessTokenExpirationMinutes"] = "10081", // 7天+1分钟
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Database:ConnectionString"] = "Server=localhost;Database=Test;"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var action = () => serviceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;
        action.Should().Throw<OptionsValidationException>();
    }

    #endregion

    #region DatabaseOptions 验证失败测试

    [Fact]
    public void ValidateOnStart_ConnectionPoolMinGreaterThanMax_ThrowsOptionsValidationException()
    {
        // Arrange - MinConnections > MaxConnections
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
            ["Jwt:Issuer"] = "LYBT.WebAPI",
            ["Jwt:Audience"] = "LYBT.Client",
            ["Jwt:AccessTokenExpirationMinutes"] = "30",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Database:ConnectionString"] = "Server=localhost;Database=Test;",
            ["Database:ConnectionPool:MinConnections"] = "100",
            ["Database:ConnectionPool:MaxConnections"] = "10" // Min > Max
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var action = () => serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        action.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void ValidateOnStart_RetryPolicyBaseDelayGreaterThanMax_ThrowsOptionsValidationException()
    {
        // Arrange - BaseDelayMs > MaxDelayMs
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
            ["Jwt:Issuer"] = "LYBT.WebAPI",
            ["Jwt:Audience"] = "LYBT.Client",
            ["Jwt:AccessTokenExpirationMinutes"] = "30",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Database:ConnectionString"] = "Server=localhost;Database=Test;",
            ["Database:RetryPolicy:BaseDelayMs"] = "10000",
            ["Database:RetryPolicy:MaxDelayMs"] = "1000" // Base > Max
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var action = () => serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        action.Should().Throw<OptionsValidationException>();
    }

    #endregion

    #region SecurityOptions 验证失败测试

    [Fact]
    public void ValidateOnStart_LoginLimitInternalLessThanPermit_ThrowsOptionsValidationException()
    {
        // Arrange - InternalPermitLimit < PermitLimit
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
            ["Jwt:Issuer"] = "LYBT.WebAPI",
            ["Jwt:Audience"] = "LYBT.Client",
            ["Jwt:AccessTokenExpirationMinutes"] = "30",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Database:ConnectionString"] = "Server=localhost;Database=Test;",
            ["Security:RateLimiting:Enabled"] = "true",
            ["Security:RateLimiting:LoginLimit:PermitLimit"] = "100",
            ["Security:RateLimiting:LoginLimit:InternalPermitLimit"] = "50" // Internal < Permit
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var action = () => serviceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value;
        action.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void ValidateOnStart_ApiLimitAdminLessThanPermit_ThrowsOptionsValidationException()
    {
        // Arrange - AdminPermitLimit < PermitLimit
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
            ["Jwt:Issuer"] = "LYBT.WebAPI",
            ["Jwt:Audience"] = "LYBT.Client",
            ["Jwt:AccessTokenExpirationMinutes"] = "30",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Database:ConnectionString"] = "Server=localhost;Database=Test;",
            ["Security:RateLimiting:Enabled"] = "true",
            ["Security:RateLimiting:ApiLimit:PermitLimit"] = "1000",
            ["Security:RateLimiting:ApiLimit:AdminPermitLimit"] = "500" // Admin < Permit
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var action = () => serviceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value;
        action.Should().Throw<OptionsValidationException>();
    }

    #endregion

    #region 验证成功测试

    [Fact]
    public void ValidateOnStart_ValidConfiguration_NoExceptionThrown()
    {
        // Arrange - 完全有效的配置
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
            ["Jwt:Issuer"] = "LYBT.WebAPI",
            ["Jwt:Audience"] = "LYBT.Client",
            ["Jwt:AccessTokenExpirationMinutes"] = "30",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Database:ConnectionString"] = "Server=localhost;Database=LYBTZYZS;",
            ["Database:ConnectionPool:MinConnections"] = "5",
            ["Database:ConnectionPool:MaxConnections"] = "100",
            ["Database:RetryPolicy:BaseDelayMs"] = "100",
            ["Database:RetryPolicy:MaxDelayMs"] = "10000",
            ["Security:RateLimiting:Enabled"] = "true",
            ["Security:RateLimiting:LoginLimit:PermitLimit"] = "10",
            ["Security:RateLimiting:LoginLimit:InternalPermitLimit"] = "100",
            ["Security:RateLimiting:ApiLimit:PermitLimit"] = "100",
            ["Security:RateLimiting:ApiLimit:AdminPermitLimit"] = "1000"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - 不应该抛出异常
        var action = () =>
        {
            _ = serviceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;
            _ = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            _ = serviceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value;
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateOnStart_RateLimitingDisabled_SkipsRateLimitingValidation()
    {
        // Arrange - 速率限制禁用时，不验证速率限制配置的有效性
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
            ["Jwt:Issuer"] = "LYBT.WebAPI",
            ["Jwt:Audience"] = "LYBT.Client",
            ["Jwt:AccessTokenExpirationMinutes"] = "30",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Database:ConnectionString"] = "Server=localhost;Database=Test;",
            ["Security:RateLimiting:Enabled"] = "false", // 禁用速率限制
            ["Security:RateLimiting:LoginLimit:PermitLimit"] = "100",
            ["Security:RateLimiting:LoginLimit:InternalPermitLimit"] = "10" // 无效配置，但因禁用不验证
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - 因禁用，无效配置不应该抛出异常
        var action = () => serviceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value;
        action.Should().NotThrow();
    }

    #endregion
}
