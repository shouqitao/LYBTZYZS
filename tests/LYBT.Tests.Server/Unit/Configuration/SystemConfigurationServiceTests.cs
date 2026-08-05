using FluentAssertions;
using LYBT.Infrastructure.Configuration.Services;
using LYBT.Infrastructure.Configuration.Stores;
using LYBT.Infrastructure.Configuration.Validation;
using LYBT.Shared.Configuration.Extensions;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// SystemConfigurationService 写入行为测试（临时文件，不依赖 DB）
/// </summary>
public class SystemConfigurationServiceTests : IDisposable
{
    private readonly string _tempDir;

    public SystemConfigurationServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "lybt-config-svc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static Dictionary<string, string?> BuildBaseline() => new()
    {
        ["App:Name"] = "LYBTZYZS",
        ["App:Version"] = "1.0.0",
        ["App:Environment"] = "Development",
        ["Session:TimeoutMinutes"] = "120",
        ["Jwt:SecretKey"] = "ThisIsASecretKeyThatIsAtLeast32CharsLong!!",
        ["Jwt:Issuer"] = "LYBT.WebAPI",
        ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=LYBTDB;",
        ["DefaultPasswords:SysAdminPassword"] = "Admin@123456"
    };

    private (SystemConfigurationService Service, string OverridePath) CreateService()
    {
        var baseline = BuildBaseline();
        var overridePath = Path.Combine(_tempDir, "runtime-overrides.json");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(baseline)
            .AddJsonFile(overridePath, optional: true, reloadOnChange: false)
            .Build();

        var store = new JsonFileConfigurationStore(overridePath, baseline);
        var validator = new ProductionConfigurationValidator(configuration);
        var service = new SystemConfigurationService(
            configuration,
            store,
            validator,
            NullLogger<SystemConfigurationService>.Instance);

        return (service, overridePath);
    }

    [Fact]
    public async Task SetValue_ThenGetValue_ReturnsUpdatedValue()
    {
        // Arrange
        var (service, _) = CreateService();

        // Act
        var setResult = await service.SetValueAsync("Session:TimeoutMinutes", "60");
        var getResult = await service.GetValueAsync("Session:TimeoutMinutes");

        // Assert
        setResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Data.Should().Be("60");
    }

    [Fact]
    public async Task SetValue_SensitiveKey_JwtSecretKey_IsRejected()
    {
        // Arrange
        var (service, _) = CreateService();

        // Act
        var result = await service.SetValueAsync("Jwt:SecretKey", "HackedSecretKeyValue");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("白名单");
    }

    [Fact]
    public async Task SetValue_SensitiveKey_ConnectionString_IsRejected()
    {
        // Arrange
        var (service, _) = CreateService();

        // Act
        var result = await service.SetValueAsync("ConnectionStrings:DefaultConnection", "Server=hacked;");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task SetValue_SensitiveSection_DefaultPasswords_IsRejected()
    {
        // Arrange
        var (service, _) = CreateService();

        // Act
        var result = await service.SetValueAsync("DefaultPasswords:SysAdminPassword", "Hacked@123");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task SetValue_UnknownSection_IsRejected()
    {
        // Arrange
        var (service, _) = CreateService();

        // Act
        var result = await service.SetValueAsync("Unknown:Section", "value");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task SetValue_EmptyKey_IsRejected()
    {
        // Arrange
        var (service, _) = CreateService();

        // Act
        var result = await service.SetValueAsync("  ", "value");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateConfiguration_BatchValidKeys_Succeeds()
    {
        // Arrange
        var (service, _) = CreateService();
        var settings = new Dictionary<string, string>
        {
            ["Session:TimeoutMinutes"] = "45",
            ["App:Environment"] = "Production"
        };

        // Act
        var result = await service.UpdateConfigurationAsync(settings);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await service.GetValueAsync("Session:TimeoutMinutes")).Data.Should().Be("45");
        (await service.GetValueAsync("App:Environment")).Data.Should().Be("Production");
    }

    [Fact]
    public async Task SetValue_TriggersOptionsMonitorHotReload()
    {
        // Arrange — 模拟 Program.cs 注册：Options 绑定 + Store + Service
        var baseline = BuildBaseline();
        var overridePath = Path.Combine(_tempDir, "runtime-overrides.json");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(baseline)
            .AddJsonFile(overridePath, optional: true, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IConfigurationRoot>((IConfigurationRoot)configuration);
        services.AddLybtServerConfiguration(configuration);
        services.AddSingleton<ProductionConfigurationValidator>();
        services.AddSingleton<IConfigurationStore>(new JsonFileConfigurationStore(overridePath, baseline));
        services.AddScoped<ISystemConfigurationService, SystemConfigurationService>();
        await using var provider = services.BuildServiceProvider();

        var monitor = provider.GetRequiredService<IOptionsMonitor<SessionOptions>>();
        var service = provider.GetRequiredService<ISystemConfigurationService>();
        monitor.CurrentValue.TimeoutMinutes.Should().Be(120); // 基线默认

        // Act
        var result = await service.SetValueAsync("Session:TimeoutMinutes", "45");

        // Assert — Reload() 后 IOptionsMonitor 已热更新
        result.IsSuccess.Should().BeTrue();
        monitor.CurrentValue.TimeoutMinutes.Should().Be(45);
    }

    [Fact]
    public async Task UpdateConfiguration_ContainsSensitiveKey_RejectsWholeBatch()
    {
        // Arrange
        var (service, _) = CreateService();
        var settings = new Dictionary<string, string>
        {
            ["Session:TimeoutMinutes"] = "45",
            ["Jwt:SecretKey"] = "Hacked"
        };

        // Act
        var result = await service.UpdateConfigurationAsync(settings);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Jwt:SecretKey");
        // 批量失败时不应有任何键被写入
        (await service.GetValueAsync("Session:TimeoutMinutes")).Data.Should().Be("120");
    }
}
