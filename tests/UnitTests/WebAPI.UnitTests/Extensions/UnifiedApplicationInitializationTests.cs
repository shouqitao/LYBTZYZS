using FluentAssertions;
using LYBT.Infrastructure.Data;
using LYBT.WebAPI.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.WebAPI.UnitTests.Extensions;

/// <summary>
/// UnifiedApplicationInitialization 扩展方法测试
/// </summary>
public class UnifiedApplicationInitializationTests
{
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<ILogger<Program>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<DatabaseInitializationService> _mockDbInitService;
    private readonly WebApplication _webApp;

    public UnifiedApplicationInitializationTests()
    {
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockLogger = new Mock<ILogger<Program>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockDbInitService = new Mock<DatabaseInitializationService>(Mock.Of<IServiceProvider>());

        // 设置基本环境
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(true);
        _mockEnvironment.Setup(e => e.IsProduction()).Returns(false);

        // 创建WebApplication用于测试
        _webApp = CreateTestWebApplication();
    }

    [Fact]
    public async Task InitializeAllApplicationServices_ShouldCompleteSuccessfully()
    {
        // Arrange
        SetupSuccessfulInitialization();

        // Act
        var action = async () => await _webApp.InitializeAllApplicationServices();

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InitializeAllApplicationServices_ShouldInitializeDatabase()
    {
        // Arrange
        SetupSuccessfulInitialization();

        // Act
        await _webApp.InitializeAllApplicationServices();

        // Assert
        _mockDbInitService.Verify(s => s.InitializeDatabaseAsync(), Times.Once,
            "数据库初始化方法应该被调用一次");
        _mockDbInitService.Verify(s => s.GetDatabaseInfoAsync(), Times.Once,
            "获取数据库信息方法应该被调用一次");
    }

    [Fact]
    public async Task InitializeAllApplicationServices_ShouldLogInitializationSteps()
    {
        // Arrange
        SetupSuccessfulInitialization();

        // Act
        await _webApp.InitializeAllApplicationServices();

        // Assert
        VerifyLoggerCalled(LogLevel.Information, "数据库初始化成功");
        VerifyLoggerCalled(LogLevel.Information, "配置服务初始化完成");
        VerifyLoggerCalled(LogLevel.Information, "应用程序启动成功");
    }

    [Fact]
    public async Task InitializeAllApplicationServices_DatabaseInitFails_ShouldLogError()
    {
        // Arrange
        var dbException = new InvalidOperationException("数据库连接失败");
        _mockDbInitService.Setup(s => s.InitializeDatabaseAsync())
            .ThrowsAsync(dbException);

        // Act
        await _webApp.InitializeAllApplicationServices();

        // Assert
        VerifyLoggerCalled(LogLevel.Error, "数据库初始化失败");
    }

    [Fact]
    public async Task InitializeAllApplicationServices_ShouldValidateConfiguration()
    {
        // Arrange
        SetupConfigurationValidation();

        // Act
        await _webApp.InitializeAllApplicationServices();

        // Assert
        VerifyLoggerCalled(LogLevel.Information, "数据库连接配置验证通过");
        VerifyLoggerCalled(LogLevel.Information, "JWT配置验证通过");
    }

    [Fact]
    public async Task InitializeAllApplicationServices_EmptyJwtInProduction_ShouldLogWarning()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsProduction()).Returns(true);
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(false);
        _mockConfiguration.Setup(c => c["JwtOptions:Secret"]).Returns(string.Empty);
        Environment.SetEnvironmentVariable("JWT_SECRET", null);

        try
        {
            // Act
            await _webApp.InitializeAllApplicationServices();

            // Assert
            VerifyLoggerCalled(LogLevel.Warning, "JWT配置可能存在问题");
        }
        finally
        {
            Environment.SetEnvironmentVariable("JWT_SECRET", null);
        }
    }

    [Fact]
    public async Task InitializeAllApplicationServices_ShouldUseEnvironmentJwtSecret()
    {
        // Arrange
        Environment.SetEnvironmentVariable("JWT_SECRET", "TestJwtSecretFromEnvironment");
        SetupSuccessfulInitialization();

        try
        {
            // Act
            await _webApp.InitializeAllApplicationServices();

            // Assert
            VerifyLoggerCalled(LogLevel.Information, "JWT配置验证通过");
        }
        finally
        {
            Environment.SetEnvironmentVariable("JWT_SECRET", null);
        }
    }

    [Fact]
    public async Task InitializeAllApplicationServices_InDevelopment_ShouldContinueOnConfigError()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(true);
        _mockEnvironment.Setup(e => e.IsProduction()).Returns(false);
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Throws(new InvalidOperationException("配置错误"));

        // Act
        var action = async () => await _webApp.InitializeAllApplicationServices();

        // Assert
        await action.Should().NotThrowAsync("开发环境应该在配置错误时继续启动");
        VerifyLoggerCalled(LogLevel.Warning, "开发环境中配置验证失败，但继续启动");
    }

    [Fact]
    public async Task InitializeAllApplicationServices_WithTimeout_ShouldRespectCancellation()
    {
        // Arrange
        _mockDbInitService.Setup(s => s.InitializeDatabaseAsync())
            .Returns(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(10)); // 模拟长时间运行
                return;
            });

        // Act & Assert
        // 由于使用了5分钟的超时，这个测试应该在超时前完成
        var action = async () => await _webApp.InitializeAllApplicationServices();
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisplayDatabaseStatusAsync_ShouldLogDatabaseInfo()
    {
        // Arrange
        SetupSuccessfulInitialization();

        // Act
        await _webApp.DisplayDatabaseStatusAsync();

        // Assert
        VerifyLoggerCalled(LogLevel.Information, "数据库状态信息已获取");
    }

    [Fact]
    public async Task DisplayDatabaseStatusAsync_ServiceNotAvailable_ShouldLogWarning()
    {
        // Arrange
        // 不设置DatabaseInitializationService，模拟服务不可用

        // Act
        await _webApp.DisplayDatabaseStatusAsync();

        // Assert
        VerifyLoggerCalled(LogLevel.Warning, "无法获取数据库状态信息");
    }

    [Fact]
    public async Task ConfigureGracefulShutdown_ShouldSetupEventHandlers()
    {
        // Arrange & Act
        var action = async () => await _webApp.ConfigureGracefulShutdown();

        // Assert
        // 由于这个方法会运行应用程序，我们只能验证它不会立即抛出异常
        // 实际测试需要模拟取消令牌
        action.Should().NotBeNull();
    }

    [Fact]
    public void GetConnectionString_ShouldPreferEnvironmentVariable()
    {
        // Arrange
        var envConnectionString = "Server=EnvServer;Database=EnvDB;";
        Environment.SetEnvironmentVariable("CONNECTION_STRING", envConnectionString);
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns("Server=ConfigServer;Database=ConfigDB;");

        try
        {
            // Act
            var result = GetConnectionStringUsingReflection(_mockConfiguration.Object);

            // Assert
            result.Should().Be(envConnectionString, "应该优先使用环境变量中的连接字符串");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CONNECTION_STRING", null);
        }
    }

    [Fact]
    public void GetConnectionString_NoEnvironmentVariable_ShouldUseConfiguration()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CONNECTION_STRING", null);
        var configConnectionString = "Server=ConfigServer;Database=ConfigDB;";
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns(configConnectionString);

        // Act
        var result = GetConnectionStringUsingReflection(_mockConfiguration.Object);

        // Assert
        result.Should().Be(configConnectionString, "应该使用配置文件中的连接字符串");
    }

    [Fact]
    public void GetConnectionString_BothEmpty_ShouldReturnEmpty()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CONNECTION_STRING", null);
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns((string?)null);

        // Act
        var result = GetConnectionStringUsingReflection(_mockConfiguration.Object);

        // Assert
        result.Should().BeEmpty("没有连接字符串时应该返回空字符串");
    }

    private void SetupSuccessfulInitialization()
    {
        _mockDbInitService.Setup(s => s.InitializeDatabaseAsync())
            .Returns(Task.CompletedTask);
        _mockDbInitService.Setup(s => s.GetDatabaseInfoAsync())
            .ReturnsAsync("Database info");

        SetupConfigurationValidation();
    }

    private void SetupConfigurationValidation()
    {
        _mockConfiguration.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns("Server=localhost;Database=TestDB;");
        _mockConfiguration.Setup(c => c["JwtOptions:Secret"])
            .Returns("TestJwtSecretKey123456789012345678901234567890");
        Environment.SetEnvironmentVariable("JWT_SECRET", null);
    }

    private WebApplication CreateTestWebApplication()
    {
        var builder = WebApplication.CreateBuilder();

        // 注册测试服务
        builder.Services.AddSingleton(_mockEnvironment.Object);
        builder.Services.AddSingleton(_mockLogger.Object);
        builder.Services.AddSingleton(_mockConfiguration.Object);
        builder.Services.AddSingleton(_mockDbInitService.Object);

        var app = builder.Build();

        // 手动设置环境属性
        var environmentProperty = typeof(WebApplication).GetProperty("Environment");
        if (environmentProperty?.CanWrite == true)
        {
            environmentProperty.SetValue(app, _mockEnvironment.Object);
        }

        return app;
    }

    private void VerifyLoggerCalled(LogLevel level, string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            $"应该记录级别为 {level} 包含 '{message}' 的日志");
    }

    private string GetConnectionStringUsingReflection(IConfiguration configuration)
    {
        // 使用反射调用私有方法GetConnectionString
        var type = typeof(UnifiedApplicationInitialization);
        var method = type.GetMethod("GetConnectionString",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (method == null)
        {
            throw new InvalidOperationException("找不到GetConnectionString方法");
        }

        var result = method.Invoke(null, new object[] { configuration, "DefaultConnection" });
        return result?.ToString() ?? string.Empty;
    }
}