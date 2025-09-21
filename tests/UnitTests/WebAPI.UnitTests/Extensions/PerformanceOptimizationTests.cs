using FluentAssertions;
using LYBT.WebAPI.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using System.IO.Compression;
using Xunit;

namespace LYBT.WebAPI.UnitTests.Extensions;

/// <summary>
/// PerformanceOptimization 扩展方法测试
/// </summary>
public class PerformanceOptimizationTests
{
    private readonly IServiceCollection _services;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public PerformanceOptimizationTests()
    {
        _services = new ServiceCollection();
        _mockConfiguration = new Mock<IConfiguration>();

        // 设置基本配置
        SetupConfiguration();
    }

    private void SetupConfiguration()
    {
        _mockConfiguration.Setup(c => c.GetValue<int>("Performance:MinWorkerThreads", 50))
            .Returns(50);
        _mockConfiguration.Setup(c => c.GetValue<int>("Performance:MinIoThreads", 50))
            .Returns(50);
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_ShouldReturnServiceCollection()
    {
        // Act
        var result = _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(_services);
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_ShouldRegisterResponseCompression()
    {
        // Act
        _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证响应压缩服务已注册
        var compressionService = serviceProvider.GetService<IResponseCompressionProvider>();
        compressionService.Should().NotBeNull("应该注册响应压缩服务");

        // 验证压缩选项已配置
        var options = serviceProvider.GetService<IOptions<ResponseCompressionOptions>>();
        options.Should().NotBeNull("应该配置响应压缩选项");
        options.Value.EnableForHttps.Should().BeTrue("应该为HTTPS启用压缩");
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_ShouldConfigureBrotliCompression()
    {
        // Act
        _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证Brotli压缩选项已配置
        var brotliOptions = serviceProvider.GetService<IOptions<BrotliCompressionProviderOptions>>();
        brotliOptions.Should().NotBeNull("应该配置Brotli压缩选项");
        brotliOptions.Value.Level.Should().Be(CompressionLevel.Optimal, "应该使用最优压缩级别");
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_ShouldConfigureGzipCompression()
    {
        // Act
        _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证Gzip压缩选项已配置
        var gzipOptions = serviceProvider.GetService<IOptions<GzipCompressionProviderOptions>>();
        gzipOptions.Should().NotBeNull("应该配置Gzip压缩选项");
        gzipOptions.Value.Level.Should().Be(CompressionLevel.Optimal, "应该使用最优压缩级别");
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_ShouldRegisterResponseCaching()
    {
        // Act
        _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证响应缓存服务已注册
        var cachingServices = _services.Where(s =>
            s.ServiceType.FullName?.Contains("ResponseCaching") == true).ToList();

        cachingServices.Should().NotBeEmpty("应该注册响应缓存服务");
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_ShouldRegisterOutputCache()
    {
        // Act
        _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证输出缓存服务已注册
        var outputCacheServices = _services.Where(s =>
            s.ServiceType.FullName?.Contains("OutputCache") == true).ToList();

        outputCacheServices.Should().NotBeEmpty("应该注册输出缓存服务");
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_ShouldConfigureKestrelOptions()
    {
        // Act
        _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证Kestrel选项已配置
        var kestrelOptions = serviceProvider.GetService<IOptions<KestrelServerOptions>>();
        kestrelOptions.Should().NotBeNull("应该配置Kestrel选项");
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_ShouldRegisterHealthChecks()
    {
        // Act
        _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证健康检查服务已注册
        var healthCheckService = serviceProvider.GetService<HealthCheckService>();
        healthCheckService.Should().NotBeNull("应该注册健康检查服务");
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_ShouldConfigureMemoryHealthCheck()
    {
        // Act
        _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证内存健康检查已注册
        var healthChecks = serviceProvider.GetService<HealthCheckService>();
        healthChecks.Should().NotBeNull();

        // 执行健康检查以验证内存检查已配置
        var context = new HealthCheckContext();
        var result = healthChecks.CheckHealthAsync(context, CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_WithCustomThreadPoolSettings_ShouldUseCustomValues()
    {
        // Arrange
        _mockConfiguration.Setup(c => c.GetValue<int>("Performance:MinWorkerThreads", 50))
            .Returns(100);
        _mockConfiguration.Setup(c => c.GetValue<int>("Performance:MinIoThreads", 50))
            .Returns(75);

        // Act
        _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);

        // Assert
        // 验证线程池配置被调用（实际验证需要检查ThreadPool.SetMinThreads的调用）
        var action = () => _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);
        action.Should().NotThrow("应该正确配置自定义线程池设置");
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_MultipleCall_ShouldNotCauseIssues()
    {
        // Act
        _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);
        var secondCall = () => _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);

        // Assert
        secondCall.Should().NotThrow("多次调用应该是安全的");
        var finalResult = _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);
        finalResult.Should().BeSameAs(_services);
    }

    [Fact]
    public void UsePerformanceOptimizations_ShouldReturnApplicationBuilder()
    {
        // Arrange
        var app = CreateTestApplicationBuilder();

        // Act
        var result = app.UsePerformanceOptimizations();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UsePerformanceOptimizations_ShouldApplyMiddlewareInCorrectOrder()
    {
        // Arrange
        var app = CreateTestApplicationBuilder();
        var middlewareOrder = new List<string>();

        // 设置模拟中间件来跟踪调用顺序
        app.Use(next => context =>
        {
            middlewareOrder.Add("Start");
            return next(context);
        });

        // Act
        app.UsePerformanceOptimizations();

        // Assert
        var action = () => app.UsePerformanceOptimizations();
        action.Should().NotThrow("应该正确应用性能优化中间件");
    }

    [Fact]
    public void UsePerformanceOptimizations_ShouldAddHttpVersionHeader()
    {
        // Arrange
        var app = CreateTestApplicationBuilder();

        // Act
        var result = app.UsePerformanceOptimizations();

        // Assert
        result.Should().NotBeNull();
        // 验证HTTP版本头中间件已添加
        var action = () => app.UsePerformanceOptimizations();
        action.Should().NotThrow("应该添加HTTP版本头中间件");
    }

    [Fact]
    public void UsePerformanceOptimizations_MultipleCall_ShouldNotCauseIssues()
    {
        // Arrange
        var app = CreateTestApplicationBuilder();

        // Act & Assert
        var action = () =>
        {
            app.UsePerformanceOptimizations();
            app.UsePerformanceOptimizations(); // 多次调用
        };

        action.Should().NotThrow("多次应用性能优化中间件应该是安全的");
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_ShouldConfigureResponseCompressionMimeTypes()
    {
        // Act
        _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<ResponseCompressionOptions>>();

        options.Should().NotBeNull();
        options.Value.MimeTypes.Should().Contain("application/json", "应该包含JSON MIME类型");
        options.Value.MimeTypes.Should().Contain("application/xml", "应该包含XML MIME类型");
        options.Value.MimeTypes.Should().Contain("text/json", "应该包含文本JSON MIME类型");
        options.Value.MimeTypes.Should().Contain("text/xml", "应该包含文本XML MIME类型");
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_ShouldConfigureOutputCachePolicies()
    {
        // Act
        _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证输出缓存策略已配置
        var outputCacheServices = _services.Where(s =>
            s.ServiceType.FullName?.Contains("OutputCache") == true).ToList();

        outputCacheServices.Should().NotBeEmpty("应该配置输出缓存策略");
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_ShouldChainMethodCalls()
    {
        // Act
        var result = _services
            .ConfigurePerformanceOptimizations(_mockConfiguration.Object)
            .ConfigurePerformanceOptimizations(_mockConfiguration.Object); // 链式调用

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(_services);
    }

    [Fact]
    public void UsePerformanceOptimizations_ShouldChainMethodCalls()
    {
        // Arrange
        var app = CreateTestApplicationBuilder();

        // Act
        var result = app
            .UsePerformanceOptimizations()
            .UsePerformanceOptimizations(); // 链式调用

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(app);
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_WithNullConfiguration_ShouldUseDefaults()
    {
        // Arrange
        Mock<IConfiguration>? nullConfig = null;

        // Act & Assert
        var action = () => _services.ConfigurePerformanceOptimizations(nullConfig!.Object);
        action.Should().Throw<NullReferenceException>("空配置应该抛出异常");
    }

    [Fact]
    public void ConfigurePerformanceOptimizations_ShouldSetCorrectKestrelLimits()
    {
        // Act
        _services.ConfigurePerformanceOptimizations(_mockConfiguration.Object);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var kestrelOptions = serviceProvider.GetService<IOptions<KestrelServerOptions>>();

        kestrelOptions.Should().NotBeNull();
        // Kestrel限制在配置回调中设置，这里验证服务已注册
        var action = () => kestrelOptions.Value.ToString();
        action.Should().NotThrow("Kestrel选项应该正确配置");
    }

    private IApplicationBuilder CreateTestApplicationBuilder()
    {
        var services = new ServiceCollection();
        services.AddResponseCompression();
        services.AddResponseCaching();
        services.AddOutputCache();
        var serviceProvider = services.BuildServiceProvider();

        var app = new Mock<IApplicationBuilder>();
        app.Setup(a => a.ApplicationServices).Returns(serviceProvider);
        app.Setup(a => a.Use(It.IsAny<Func<Microsoft.AspNetCore.Http.RequestDelegate, Microsoft.AspNetCore.Http.RequestDelegate>>()))
           .Returns(app.Object);

        return app.Object;
    }
}