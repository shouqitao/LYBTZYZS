using FluentAssertions;
using LYBT.Shared.Configuration.Extensions;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// 配置加载集成测试
/// </summary>
public class ConfigurationLoadingTests
{
    #region 服务端配置加载测试

    [Fact]
    public void ServerConfiguration_LoadFromJson_AllOptionsRegistered()
    {
        // Arrange
        var configuration = CreateServerConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - 验证所有服务端Options都已注册
        serviceProvider.GetService<IOptions<JwtOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<DatabaseOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<SecurityOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<SessionOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<DefaultPasswordOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<SystemAdminOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<LoggingOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<MemoryCacheOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<LocalJwtOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<CorsOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<DesktopUpdateOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<AppInfoOptions>>().Should().NotBeNull();
    }

    [Fact]
    public void ServerConfiguration_LoadFromJson_ValuesBindCorrectly()
    {
        // Arrange
        var configuration = CreateServerConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - 验证值正确绑定
        var jwtOptions = serviceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;
        jwtOptions.SecretKey.Should().Be("J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==");
        jwtOptions.Issuer.Should().Be("LYBT.WebAPI");
        jwtOptions.Audience.Should().Be("LYBT.Client");
        jwtOptions.AccessTokenExpirationMinutes.Should().Be(60);
        jwtOptions.RefreshTokenExpirationDays.Should().Be(14);

        var dbOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        dbOptions.ConnectionString.Should().Be("Server=localhost;Database=LYBTZYZS;Integrated Security=true;TrustServerCertificate=true;");
        dbOptions.MigrationTimeoutSeconds.Should().Be(300);

        var sessionOptions = serviceProvider.GetRequiredService<IOptions<SessionOptions>>().Value;
        sessionOptions.TimeoutMinutes.Should().Be(45);
    }

    #endregion

    #region 客户端配置加载测试

    [Fact]
    public void ClientConfiguration_LoadFromJson_AllOptionsRegistered()
    {
        // Arrange
        var configuration = CreateClientConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddLybtClientConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - 验证所有客户端Options都已注册
        serviceProvider.GetService<IOptions<JwtOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<ApiClientOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<ClientSessionOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<ClinicSettingsOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<FeatureToggleOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<OfflineModeOptions>>().Should().NotBeNull();
    }

    [Fact]
    public void ClientConfiguration_LoadFromJson_ValuesBindCorrectly()
    {
        // Arrange
        var configuration = CreateClientConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddLybtClientConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - 验证值正确绑定
        var apiClientOptions = serviceProvider.GetRequiredService<IOptions<ApiClientOptions>>().Value;
        apiClientOptions.BaseUrl.Should().Be("https://localhost:5001/");
        apiClientOptions.TimeoutSeconds.Should().Be(30);
        apiClientOptions.IgnoreSslErrors.Should().BeTrue();

        var sessionOptions = serviceProvider.GetRequiredService<IOptions<ClientSessionOptions>>().Value;
        sessionOptions.InactivityTimeoutMinutes.Should().Be(20);
        sessionOptions.WarningBeforeTimeoutMinutes.Should().Be(3);

        var featureOptions = serviceProvider.GetRequiredService<IOptions<FeatureToggleOptions>>().Value;
        featureOptions.OverwriteConflicts.Should().BeFalse();
    }

    #endregion

    #region 环境变量覆盖测试

    [Fact]
    public void Configuration_EnvironmentVariables_OverrideJsonValues()
    {
        // Arrange - 设置环境变量
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpirationMinutes", "120");
        Environment.SetEnvironmentVariable("Session__TimeoutMinutes", "60");

        try
        {
            var configData = new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
                ["Jwt:Issuer"] = "LYBT.WebAPI",
                ["Jwt:Audience"] = "LYBT.Client",
                ["Jwt:AccessTokenExpirationMinutes"] = "30", // JSON设置30
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["Database:ConnectionString"] = "Server=localhost;Database=LYBTZYZS;",
                ["Session:TimeoutMinutes"] = "45" // JSON设置45
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .AddEnvironmentVariables() // 环境变量优先级更高
                .Build();

            var services = new ServiceCollection();
            services.AddLybtServerConfiguration(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var jwtOptions = serviceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;
            var sessionOptions = serviceProvider.GetRequiredService<IOptions<SessionOptions>>().Value;

            // Assert - 环境变量覆盖了JSON值
            jwtOptions.AccessTokenExpirationMinutes.Should().Be(120);
            sessionOptions.TimeoutMinutes.Should().Be(60);
        }
        finally
        {
            // Cleanup - 清理环境变量
            Environment.SetEnvironmentVariable("Jwt__AccessTokenExpirationMinutes", null);
            Environment.SetEnvironmentVariable("Session__TimeoutMinutes", null);
        }
    }

    #endregion

    #region IOptionsMonitor 热更新测试

    [Fact]
    public void Configuration_IOptionsMonitor_SupportsReload()
    {
        // Arrange
        var configuration = CreateClientConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddLybtClientConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - IOptionsMonitor 应该被注册
        var monitor = serviceProvider.GetService<IOptionsMonitor<ApiClientOptions>>();
        monitor.Should().NotBeNull();
        monitor!.CurrentValue.BaseUrl.Should().Be("https://localhost:5001/");
    }

    [Fact]
    public void Configuration_IOptionsSnapshot_RegisteredCorrectly()
    {
        // Arrange
        var configuration = CreateServerConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - IOptionsSnapshot 应该被注册
        using var scope = serviceProvider.CreateScope();
        var snapshot = scope.ServiceProvider.GetService<IOptionsSnapshot<JwtOptions>>();
        snapshot.Should().NotBeNull();
        snapshot!.Value.Issuer.Should().Be("LYBT.WebAPI");
    }

    #endregion

    #region IValidateOptions 验证器注册测试

    [Fact]
    public void ServerConfiguration_RegistersIValidateOptions_ForJwtOptions()
    {
        // Arrange
        var configuration = CreateServerConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - IValidateOptions<JwtOptions> 应注册为 JwtOptionsValidator
        var validators = serviceProvider.GetServices<IValidateOptions<JwtOptions>>().ToList();
        validators.Should().ContainSingle(v => v is JwtOptionsValidator);
    }

    [Fact]
    public void ServerConfiguration_RegistersIValidateOptions_ForDatabaseOptions()
    {
        // Arrange
        var configuration = CreateServerConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - IValidateOptions<DatabaseOptions> 应注册为 DatabaseOptionsValidator
        var validators = serviceProvider.GetServices<IValidateOptions<DatabaseOptions>>().ToList();
        validators.Should().ContainSingle(v => v is DatabaseOptionsValidator);
    }

    [Fact]
    public void ServerConfiguration_RegistersIValidateOptions_ForSecurityOptions()
    {
        // Arrange
        var configuration = CreateServerConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - IValidateOptions<SecurityOptions> 应注册为 SecurityOptionsValidator
        var validators = serviceProvider.GetServices<IValidateOptions<SecurityOptions>>().ToList();
        validators.Should().ContainSingle(v => v is SecurityOptionsValidator);
    }

    [Fact]
    public void ClientConfiguration_RegistersIValidateOptions_ForJwtOptions()
    {
        // Arrange
        var configuration = CreateClientConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddLybtClientConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - IValidateOptions<JwtOptions> 应注册为 JwtOptionsValidator
        var validators = serviceProvider.GetServices<IValidateOptions<JwtOptions>>().ToList();
        validators.Should().ContainSingle(v => v is JwtOptionsValidator);
    }

    #endregion

    #region LocalJwtOptions 验证器测试

    [Fact]
    public void ServerConfiguration_RegistersIValidateOptions_ForLocalJwtOptions()
    {
        // Arrange
        var configuration = CreateServerConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddLybtServerConfiguration(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - IValidateOptions<LocalJwtOptions> 应注册为 LocalJwtOptionsValidator
        var validators = serviceProvider.GetServices<IValidateOptions<LocalJwtOptions>>().ToList();
        validators.Should().ContainSingle(v => v is LocalJwtOptionsValidator);
    }

    [Fact]
    public void LocalJwtOptionsValidator_InvalidBase64Key_FailsValidation()
    {
        // Arrange
        var validator = new LocalJwtOptionsValidator();
        var options = new LocalJwtOptions { SecretKey = "not-valid-base64!!!" };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failures.Should().Contain(f => f.Contains("Base64"));
    }

    [Fact]
    public void LocalJwtOptionsValidator_ShortKey_FailsValidation()
    {
        // Arrange
        var validator = new LocalJwtOptionsValidator();
        var options = new LocalJwtOptions { SecretKey = "c2hvcnQ=" }; // "short" in Base64, 5 bytes

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failures.Should().Contain(f => f.Contains("32 字节"));
    }

    [Fact]
    public void LocalJwtOptionsValidator_ValidKey_PassesValidation()
    {
        // Arrange
        var validator = new LocalJwtOptionsValidator();
        var options = new LocalJwtOptions
        {
            SecretKey = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw=="
        };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region 新增 Options 默认值测试

    [Fact]
    public void LocalJwtOptions_DefaultValues_AreCorrect()
    {
        var options = new LocalJwtOptions();
        options.Issuer.Should().Be("LYBT-LocalWebAPI");
        options.Audience.Should().Be("LYBT-Desktop");
        options.ExpirationMinutes.Should().Be(60);
    }

    [Fact]
    public void CorsOptions_DefaultValues_AreCorrect()
    {
        var options = new CorsOptions();
        options.AllowedOrigins.Should().BeEmpty();
        options.AllowCredentials.Should().BeFalse();
        options.PreflightMaxAgeSeconds.Should().Be(86400);
    }

    [Fact]
    public void DesktopUpdateOptions_DefaultValues_AreCorrect()
    {
        var options = new DesktopUpdateOptions();
        options.Enabled.Should().BeFalse();
        options.CheckIntervalMinutes.Should().Be(60);
        options.DownloadBaseUrl.Should().BeNull();
    }

    [Fact]
    public void OfflineModeOptions_DefaultValues_AreCorrect()
    {
        var options = new OfflineModeOptions();
        options.Enabled.Should().BeFalse();
        options.LocalApiBaseUrl.Should().Be("http://localhost:5300");
        options.AutoSwitchToLocal.Should().BeTrue();
        options.HealthCheckIntervalSeconds.Should().Be(30);
        options.RetryCountBeforeSwitch.Should().Be(3);
    }

    [Fact]
    public void AppInfoOptions_DefaultValues_AreCorrect()
    {
        var options = new AppInfoOptions();
        options.Name.Should().Be("LYBTZYZS");
        options.Version.Should().Be("1.0.0");
        options.Environment.Should().Be("Development");
    }

    [Fact]
    public void AllNewOptions_SectionNameConstants_AreCorrect()
    {
        LocalJwtOptions.SectionName.Should().Be("LocalJwt");
        CorsOptions.SectionName.Should().Be("Cors");
        DesktopUpdateOptions.SectionName.Should().Be("DesktopUpdate");
        OfflineModeOptions.SectionName.Should().Be("OfflineMode");
        AppInfoOptions.SectionName.Should().Be("App");
    }

    #endregion

    #region 辅助方法

    private static IConfiguration CreateServerConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            // Jwt 配置
            ["Jwt:SecretKey"] = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
            ["Jwt:Issuer"] = "LYBT.WebAPI",
            ["Jwt:Audience"] = "LYBT.Client",
            ["Jwt:AccessTokenExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "14",
            ["Jwt:ClockSkewSeconds"] = "300",

            // Database 配置
            ["Database:ConnectionString"] = "Server=localhost;Database=LYBTZYZS;Integrated Security=true;TrustServerCertificate=true;",
            ["Database:AutoMigrate"] = "false",
            ["Database:MigrationTimeoutSeconds"] = "300",

            // Security 配置
            ["Security:EnableHttpsRedirection"] = "true",
            ["Security:RateLimiting:EnableRateLimiting"] = "true",
            ["Security:RateLimiting:PermitLimit"] = "100",
            ["Security:RateLimiting:WindowSeconds"] = "60",

            // Session 配置
            ["Session:TimeoutMinutes"] = "45",

            // DefaultPassword 配置
            ["DefaultPassword:Password"] = "Lybt@123",

            // SystemAdmin 配置
            ["SystemAdmin:DefaultUsername"] = "admin",

            // Logging 配置
            ["Logging:LogLevel:Default"] = "Information",
            ["Logging:EnableRequestLogging"] = "true",

            // MemoryCache 配置
            ["MemoryCache:SlidingExpirationMinutes"] = "30",
            ["MemoryCache:AbsoluteExpirationMinutes"] = "60",

            // LocalJwt 配置
            ["LocalJwt:SecretKey"] = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
            ["LocalJwt:Issuer"] = "LYBT-LocalWebAPI",
            ["LocalJwt:Audience"] = "LYBT-Desktop",
            ["LocalJwt:ExpirationMinutes"] = "60",

            // Cors 配置
            ["Cors:AllowedOrigins:0"] = "https://localhost:5001",
            ["Cors:AllowCredentials"] = "true",

            // DesktopUpdate 配置
            ["DesktopUpdate:Enabled"] = "true",
            ["DesktopUpdate:CheckIntervalMinutes"] = "60",

            // AppInfo 配置
            ["App:Name"] = "LYBTZYZS",
            ["App:Version"] = "1.0.0",
            ["App:Environment"] = "Development"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    private static IConfiguration CreateClientConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            // Jwt 配置 (客户端需要用于本地Token验证)
            ["Jwt:SecretKey"] = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
            ["Jwt:Issuer"] = "LYBT.WebAPI",
            ["Jwt:Audience"] = "LYBT.Client",
            ["Jwt:AccessTokenExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "14",

            // ApiClient 配置
            ["ApiClient:BaseUrl"] = "https://localhost:5001/",
            ["ApiClient:TimeoutSeconds"] = "30",
            ["ApiClient:IgnoreSslErrors"] = "true",

            // ClientSession 配置
            ["ClientSession:InactivityTimeoutMinutes"] = "20",
            ["ClientSession:WarningBeforeTimeoutMinutes"] = "3",
            ["ClientSession:ActivityCheckIntervalSeconds"] = "30",

            // ClinicSettings 配置
            ["ClinicSettings:ClinicName"] = "测试诊所",

            // FeatureToggle 配置
            ["FeatureToggles:ConsultationCreate"] = "true",
            ["FeatureToggles:PrescriptionCreate"] = "false",
            ["FeatureToggles:DuplicateHerbMergeStrategy"] = "Max",
            ["FeatureToggles:OverwriteConflicts"] = "false",

            // OfflineMode 配置
            ["OfflineMode:Enabled"] = "true",
            ["OfflineMode:LocalApiBaseUrl"] = "http://localhost:5300",
            ["OfflineMode:AutoSwitchToLocal"] = "true",
            ["OfflineMode:HealthCheckIntervalSeconds"] = "30",
            ["OfflineMode:RetryCountBeforeSwitch"] = "3"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    #endregion
}
