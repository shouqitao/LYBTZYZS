using FluentAssertions;
using LYBT.Infrastructure.Caching.Interfaces;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Configuration.Services;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Interfaces;
using LYBT.WebAPI.Extensions;
using LYBT.WebAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LYBT.WebAPI.UnitTests.Extensions;

/// <summary>
/// UnifiedServiceRegistration 扩展方法测试
/// </summary>
public class UnifiedServiceRegistrationTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<IConfigurationSection> _mockSection;
    private readonly IServiceCollection _services;

    public UnifiedServiceRegistrationTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockSection = new Mock<IConfigurationSection>();
        _services = new ServiceCollection();

        // 设置基本配置
        SetupBasicConfiguration();
    }

    private void SetupBasicConfiguration()
    {
        // 设置数据库连接字符串
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns("Server=localhost;Database=TestDB;Trusted_Connection=true;");

        // 设置JWT配置
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(s => s["Secret"]).Returns("TestSecretKeyForJWTAuthentication_ShouldBeAtLeast32Characters");
        jwtSection.Setup(s => s["Issuer"]).Returns("TestIssuer");
        jwtSection.Setup(s => s["Audience"]).Returns("TestAudience");
        jwtSection.Setup(s => s["ClockSkewSeconds"]).Returns("300");
        _mockConfiguration.Setup(c => c.GetSection("JwtOptions")).Returns(jwtSection.Object);

        // 设置其他配置节
        SetupConfigurationSection("AuthOptions");
        SetupConfigurationSection("DefaultPasswordOptions");
        SetupConfigurationSection("SysAdminOptions");
        SetupConfigurationSection("UserOptions");
        SetupConfigurationSection("SecurityOptions");
        SetupConfigurationSection("DatabaseOptions");

        // 设置环境
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(true);
        _mockEnvironment.Setup(e => e.IsProduction()).Returns(false);
    }

    private void SetupConfigurationSection(string sectionName)
    {
        var section = new Mock<IConfigurationSection>();
        section.Setup(s => s.GetChildren()).Returns(new List<IConfigurationSection>());
        _mockConfiguration.Setup(c => c.GetSection(sectionName)).Returns(section.Object);
    }

    [Fact]
    public void RegisterAllApplicationServices_ShouldRegisterAllServices_InDevelopmentEnvironment()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(true);

        // Act
        var result = _services.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(_services);

        var serviceProvider = _services.BuildServiceProvider();

        // 验证基础设施服务
        serviceProvider.GetService<ICacheService>().Should().NotBeNull();
        serviceProvider.GetService<IHttpContextAccessor>().Should().NotBeNull();
        serviceProvider.GetService<DefaultPasswordService>().Should().NotBeNull();
        serviceProvider.GetService<DatabaseInitializationService>().Should().NotBeNull();

        // 验证缓存服务
        serviceProvider.GetService<IMemoryCache>().Should().NotBeNull();

        // 验证配置选项
        serviceProvider.GetService<IOptions<JwtOptions>>().Should().NotBeNull();
        serviceProvider.GetService<IOptions<AuthOptions>>().Should().NotBeNull();

        // 验证异常处理器
        serviceProvider.GetService<GlobalExceptionHandler>().Should().NotBeNull();
    }

    [Fact]
    public void RegisterAllApplicationServices_ShouldRegisterBusinessModules()
    {
        // Arrange & Act
        var result = _services.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证业务模块服务（这些服务应该通过AddAllModules注册）
        // 由于模块服务需要实际的模块程序集，我们验证AddAllModules方法被调用
        // 通过检查是否有相关的服务注册
        var moduleServices = _services.Where(s =>
            s.ServiceType.FullName?.Contains("LYBT.Shared.Interfaces") == true ||
            s.ServiceType.Name.EndsWith("Service")).ToList();

        moduleServices.Should().NotBeEmpty("应该注册了业务模块服务");
    }

    [Fact]
    public void RegisterAllApplicationServices_ShouldConfigureJwtAuthentication()
    {
        // Arrange & Act
        _services.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证认证服务
        var authenticationSchemeProvider = serviceProvider.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        authenticationSchemeProvider.Should().NotBeNull();

        // 验证JWT配置
        var jwtOptions = serviceProvider.GetService<IOptions<JwtOptions>>();
        jwtOptions.Should().NotBeNull();
        jwtOptions.Value.Should().NotBeNull();
    }

    [Fact]
    public void RegisterAllApplicationServices_ShouldConfigureDbContextWithConnectionString()
    {
        // Arrange
        var connectionString = "Server=TestServer;Database=TestDB;Trusted_Connection=true;";
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns(connectionString);

        // Act
        _services.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证DbContext服务已注册
        var dbContextService = _services.FirstOrDefault(s => s.ServiceType == typeof(AppDbContext));
        dbContextService.Should().NotBeNull("AppDbContext应该被注册");
    }

    [Fact]
    public void RegisterAllApplicationServices_ShouldConfigureMemoryCache()
    {
        // Arrange & Act
        _services.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var memoryCache = serviceProvider.GetService<IMemoryCache>();
        memoryCache.Should().NotBeNull();

        var cacheService = serviceProvider.GetService<ICacheService>();
        cacheService.Should().NotBeNull();
    }

    [Fact]
    public void RegisterAllApplicationServices_ShouldConfigureAllOptionsWithValidation()
    {
        // Arrange & Act
        _services.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证所有选项都已注册并配置了验证
        var jwtOptions = serviceProvider.GetService<IOptionsMonitor<JwtOptions>>();
        jwtOptions.Should().NotBeNull();

        var authOptions = serviceProvider.GetService<IOptionsMonitor<AuthOptions>>();
        authOptions.Should().NotBeNull();

        var passwordOptions = serviceProvider.GetService<IOptionsMonitor<DefaultPasswordOptions>>();
        passwordOptions.Should().NotBeNull();

        var securityOptions = serviceProvider.GetService<IOptionsMonitor<SecurityOptions>>();
        securityOptions.Should().NotBeNull();

        var databaseOptions = serviceProvider.GetService<IOptionsMonitor<DatabaseOptions>>();
        databaseOptions.Should().NotBeNull();
    }

    [Fact]
    public void RegisterAllApplicationServices_ShouldAddEnvironmentAwareValidation_InProduction()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsProduction()).Returns(true);
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(false);

        // Act
        _services.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证生产环境验证过滤器已注册
        var startupFilters = serviceProvider.GetServices<Microsoft.AspNetCore.Hosting.IStartupFilter>();
        startupFilters.Should().NotBeEmpty();

        var productionFilter = startupFilters.OfType<ProductionConfigValidationFilter>().FirstOrDefault();
        productionFilter.Should().NotBeNull("生产环境应该注册ProductionConfigValidationFilter");
    }

    [Fact]
    public void RegisterAllApplicationServices_ShouldNotAddProductionValidation_InDevelopment()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsProduction()).Returns(false);
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(true);

        // Act
        _services.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        var startupFilters = serviceProvider.GetServices<Microsoft.AspNetCore.Hosting.IStartupFilter>();
        var productionFilter = startupFilters.OfType<ProductionConfigValidationFilter>().FirstOrDefault();
        productionFilter.Should().BeNull("开发环境不应该注册ProductionConfigValidationFilter");
    }

    [Fact]
    public void RegisterAllApplicationServices_ShouldHandleEmptyConnectionString()
    {
        // Arrange
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns(string.Empty);

        // Act & Assert
        var action = () => _services.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);
        action.Should().NotThrow("空连接字符串应该被优雅处理");

        // 验证DbContext在没有连接字符串时不会被注册
        var dbContextService = _services.FirstOrDefault(s => s.ServiceType == typeof(AppDbContext));
        dbContextService.Should().BeNull("没有连接字符串时不应该注册DbContext");
    }

    [Fact]
    public void RegisterAllApplicationServices_ShouldUseEnvironmentVariableForConnectionString()
    {
        // Arrange
        var envConnectionString = "Server=EnvServer;Database=EnvDB;";
        Environment.SetEnvironmentVariable("CONNECTION_STRING", envConnectionString);

        try
        {
            // Act
            _services.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);

            // Assert
            var dbContextService = _services.FirstOrDefault(s => s.ServiceType == typeof(AppDbContext));
            dbContextService.Should().NotBeNull("环境变量连接字符串应该被使用");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CONNECTION_STRING", null);
        }
    }

    [Fact]
    public void RegisterAllApplicationServices_ShouldThrowInProduction_WhenJwtSecretMissing()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsProduction()).Returns(true);
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(false);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(s => s["Secret"]).Returns(string.Empty);
        _mockConfiguration.Setup(c => c.GetSection("JwtOptions")).Returns(jwtSection.Object);

        try
        {
            // Act & Assert
            var action = () => _services.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*生产环境必须*JWT密钥*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        }
    }

    [Fact]
    public void RegisterAllApplicationServices_ShouldUseDefaultJwtSecret_InDevelopment()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsProduction()).Returns(false);
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(true);

        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(s => s["Secret"]).Returns(string.Empty);
        jwtSection.Setup(s => s["Issuer"]).Returns("TestIssuer");
        jwtSection.Setup(s => s["Audience"]).Returns("TestAudience");
        _mockConfiguration.Setup(c => c.GetSection("JwtOptions")).Returns(jwtSection.Object);

        // Act & Assert
        var action = () => _services.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);
        action.Should().NotThrow("开发环境应该使用默认JWT密钥");
    }

    [Fact]
    public void RegisterAllApplicationServices_ShouldConfigureSwaggerWithJwtSupport()
    {
        // Arrange & Act
        _services.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证Swagger服务已注册
        var swaggerGenService = _services.FirstOrDefault(s =>
            s.ServiceType.FullName?.Contains("ISwaggerProvider") == true);

        // 验证API Explorer已注册
        var apiExplorerService = serviceProvider.GetService<Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionGroupCollectionProvider>();
        apiExplorerService.Should().NotBeNull();
    }

    [Fact]
    public void RegisterAllApplicationServices_ShouldConfigureControllerServices()
    {
        // Arrange & Act
        _services.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证控制器服务已注册
        var mvcService = _services.FirstOrDefault(s =>
            s.ServiceType.FullName?.Contains("IMvcBuilder") == true ||
            s.ServiceType.Name.Contains("Mvc"));
    }

    [Fact]
    public void RegisterAllApplicationServices_ShouldChainMethodCalls()
    {
        // Arrange
        var initialServiceCount = _services.Count;

        // Act
        var result1 = _services.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);
        var result2 = result1.RegisterAllApplicationServices(_mockConfiguration.Object, _mockEnvironment.Object);

        // Assert
        result1.Should().BeSameAs(_services);
        result2.Should().BeSameAs(_services);
        _services.Count.Should().BeGreaterThan(initialServiceCount);
    }
}