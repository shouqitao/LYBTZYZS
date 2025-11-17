using LYBT.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace LYBT.Module.Auth.Tests.Common.TestBase;

/// <summary>
/// Service层测试基类
/// 提供统一的配置、Mock、DI设置
/// 基于JWT测试修复的成功经验，解决配置和依赖注入问题
/// </summary>
public abstract class BaseServiceTest<TService> where TService : class
{
    protected readonly Mock<IOptions<LybtOptions>> _mockOptions;
    protected readonly IConfiguration _configuration;
    protected readonly IServiceProvider _serviceProvider;
    protected readonly TService _sut;
    protected readonly ILogger<TService> _logger;

    protected BaseServiceTest()
    {
        _mockOptions = CreateMockOptions();
        _configuration = CreateInMemoryConfiguration();
        _serviceProvider = BuildServiceProvider();
        _sut = _serviceProvider.GetRequiredService<TService>();
        _logger = _serviceProvider.GetRequiredService<ILogger<TService>>();
    }

    /// <summary>
    /// 创建Mock的LybtOptions
    /// </summary>
    protected virtual Mock<IOptions<LybtOptions>> CreateMockOptions()
    {
        var mockOptions = new Mock<IOptions<LybtOptions>>();
        mockOptions.Setup(o => o.Value).Returns(CreateTestOptions());
        return mockOptions;
    }

    /// <summary>
    /// 创建测试用的LybtOptions
    /// 子类可以重写以提供特定的配置
    /// </summary>
    protected virtual LybtOptions CreateTestOptions()
    {
        return new LybtOptions
        {
            Jwt = new JwtConfiguration
            {
                SecretKey = "ThisIsAVeryStrongSecretKeyForTesting123456789012345678901234567890",
                Issuer = "LYBT-Test",
                Audience = "LYBT-TestUsers",
                AccessTokenExpirationMinutes = 30,
                RefreshTokenExpirationDays = 7
            }
        };
    }

    /// <summary>
    /// 创建内存配置
    /// 解决ConfigurationBinder.GetValue扩展方法无法mock的问题
    /// </summary>
    protected virtual IConfiguration CreateInMemoryConfiguration()
    {
        var configData = new Dictionary<string, string>
        {
            // JWT配置
            ["Lybt:Jwt:SecretKey"] = "ThisIsAVeryStrongSecretKeyForTesting123456789012345678901234567890",
            ["Lybt:Jwt:Issuer"] = "LYBT-Test",
            ["Lybt:Jwt:Audience"] = "LYBT-TestUsers",
            ["Lybt:Jwt:AccessTokenExpirationMinutes"] = "30",
            ["Lybt:Jwt:RefreshTokenExpirationDays"] = "7",

            // 数据库配置
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=LYBT_Test;Trusted_Connection=true;",

            // 日志配置
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning"
        };

        return new TestConfiguration.InMemoryConfiguration(configData);
    }

    /// <summary>
    /// 构建服务提供者
    /// 子类必须实现RegisterTestServices方法来注册特定服务
    /// </summary>
    protected virtual IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // 注册基础服务
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(_mockOptions.Object);
        services.AddSingleton(_configuration);
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // 注册特定测试的服务
        RegisterTestServices(services);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 注册测试特定的服务
    /// 子类必须实现此方法来注册Mock和实际服务
    /// </summary>
    protected abstract void RegisterTestServices(IServiceCollection services);

    /// <summary>
    /// 创建Mock对象
    /// </summary>
    protected Mock<T> CreateMock<T>() where T : class
    {
        return new Mock<T>();
    }

    /// <summary>
    /// 创建带配置的Mock对象
    /// </summary>
    protected Mock<T> CreateMock<T>(Action<Mock<T>> setup) where T : class
    {
        var mock = new Mock<T>();
        setup(mock);
        return mock;
    }
}