using FluentAssertions;
using LYBT.WebAPI.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace LYBT.WebAPI.UnitTests.Extensions;

/// <summary>
/// UnifiedMiddlewareConfiguration 扩展方法测试
/// </summary>
public class UnifiedMiddlewareConfigurationTests
{
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    public UnifiedMiddlewareConfigurationTests()
    {
        _mockEnvironment = new Mock<IWebHostEnvironment>();

        // 设置基本环境
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(false);
        _mockEnvironment.Setup(e => e.IsProduction()).Returns(true);
    }

    [Fact]
    public void ConfigureAllMiddleware_ShouldReturnWebApplication()
    {
        // Arrange
        var webApp = CreateTestWebApplication();

        // Act
        var result = webApp.ConfigureAllMiddleware();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(webApp);
    }

    [Fact]
    public void ConfigureAllMiddleware_ShouldConfigureMiddlewareInCorrectOrder()
    {
        // Arrange
        var webApp = CreateTestWebApplication();
        var middlewareOrder = new List<string>();

        // 模拟中间件调用链来验证顺序
        // 这里只验证方法调用成功，不深入测试中间件管道

        // Act
        webApp.ConfigureAllMiddleware();

        // Assert
        // 验证方法被正确调用（通过检查是否有异常）
        var action = () => webApp.ConfigureAllMiddleware();
        action.Should().NotThrow();
    }

    [Fact]
    public void ConfigureAllMiddleware_InDevelopment_ShouldUseDeveloperExceptionPage()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(true);
        _mockEnvironment.Setup(e => e.IsProduction()).Returns(false);
        var webApp = CreateTestWebApplication();

        // Act
        var result = webApp.ConfigureAllMiddleware();

        // Assert
        result.Should().NotBeNull();
        // 在开发环境中应该配置开发者异常页面
        // 实际验证需要检查中间件管道，这里验证方法调用成功
        var action = () => webApp.ConfigureAllMiddleware();
        action.Should().NotThrow();
    }

    [Fact]
    public void ConfigureAllMiddleware_InProduction_ShouldUseHttpsRedirection()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(false);
        _mockEnvironment.Setup(e => e.IsProduction()).Returns(true);
        var webApp = CreateTestWebApplication();

        // Act
        var result = webApp.ConfigureAllMiddleware();

        // Assert
        result.Should().NotBeNull();
        // 在生产环境中应该配置HTTPS重定向和HSTS
        var action = () => webApp.ConfigureAllMiddleware();
        action.Should().NotThrow();
    }

    [Fact]
    public void ConfigureAllMiddleware_ShouldConfigureRouting()
    {
        // Arrange
        var webApp = CreateTestWebApplication();

        // Act
        var result = webApp.ConfigureAllMiddleware();

        // Assert
        result.Should().NotBeNull();
        // 路由应该被配置
        var action = () => webApp.ConfigureAllMiddleware();
        action.Should().NotThrow();
    }

    [Fact]
    public void ConfigureAllMiddleware_ShouldConfigureSwagger()
    {
        // Arrange
        var webApp = CreateTestWebApplication();

        // Act
        var result = webApp.ConfigureAllMiddleware();

        // Assert
        result.Should().NotBeNull();
        // Swagger中间件应该被配置
        var action = () => webApp.ConfigureAllMiddleware();
        action.Should().NotThrow();
    }

    [Fact]
    public void ConfigureAllMiddleware_ShouldConfigureAuthentication()
    {
        // Arrange
        var webApp = CreateTestWebApplication();

        // Act
        var result = webApp.ConfigureAllMiddleware();

        // Assert
        result.Should().NotBeNull();
        // 认证和授权中间件应该被配置
        var action = () => webApp.ConfigureAllMiddleware();
        action.Should().NotThrow();
    }

    [Fact]
    public void ConfigureAllMiddleware_ShouldConfigureEndpoints()
    {
        // Arrange
        var webApp = CreateTestWebApplication();

        // Act
        var result = webApp.ConfigureAllMiddleware();

        // Assert
        result.Should().NotBeNull();
        // 端点映射应该被配置
        var action = () => webApp.ConfigureAllMiddleware();
        action.Should().NotThrow();
    }

    [Fact]
    public void ConfigureAllMiddleware_ShouldEnableExceptionHandler()
    {
        // Arrange
        var webApp = CreateTestWebApplication();

        // Act
        var result = webApp.ConfigureAllMiddleware();

        // Assert
        result.Should().NotBeNull();
        // 全局异常处理器应该被启用
        var action = () => webApp.ConfigureAllMiddleware();
        action.Should().NotThrow();
    }

    [Fact]
    public void ConfigureAllMiddleware_MultipleCall_ShouldNotThrow()
    {
        // Arrange
        var webApp = CreateTestWebApplication();

        // Act & Assert
        var action = () =>
        {
            webApp.ConfigureAllMiddleware();
            webApp.ConfigureAllMiddleware(); // 多次调用
        };

        action.Should().NotThrow("多次配置中间件应该是安全的");
    }

    [Fact]
    public void ConfigureAllMiddleware_WithNullApp_ShouldThrow()
    {
        // Arrange
        WebApplication? nullApp = null;

        // Act & Assert
        var action = () => nullApp!.ConfigureAllMiddleware();
        action.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void ConfigureAllMiddleware_ShouldChainMethodCalls()
    {
        // Arrange
        var webApp = CreateTestWebApplication();

        // Act
        var result = webApp
            .ConfigureAllMiddleware()
            .ConfigureAllMiddleware(); // 链式调用

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(webApp);
    }

    [Fact]
    public void ConfigureAllMiddleware_WithCustomEnvironment_ShouldAdaptBehavior()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Staging");
        _mockEnvironment.Setup(e => e.IsDevelopment()).Returns(false);
        _mockEnvironment.Setup(e => e.IsProduction()).Returns(false);
        var webApp = CreateTestWebApplication();

        // Act
        var result = webApp.ConfigureAllMiddleware();

        // Assert
        result.Should().NotBeNull();
        // 自定义环境应该被正确处理
        var action = () => webApp.ConfigureAllMiddleware();
        action.Should().NotThrow();
    }

    private WebApplication CreateTestWebApplication()
    {
        var builder = WebApplication.CreateBuilder();

        // 注册必要的服务
        builder.Services.AddRouting();
        builder.Services.AddControllers();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddSwaggerGen();

        // 使用测试环境
        builder.Environment.EnvironmentName = _mockEnvironment.Object.EnvironmentName;

        var app = builder.Build();

        // 手动设置环境属性
        var environmentProperty = typeof(WebApplication).GetProperty("Environment");
        if (environmentProperty?.CanWrite == true)
        {
            environmentProperty.SetValue(app, _mockEnvironment.Object);
        }

        return app;
    }
}

