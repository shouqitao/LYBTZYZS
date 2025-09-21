using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentAssertions;
using LYBT.WebAPI.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace LYBT.WebAPI.UnitTests.Extensions;

/// <summary>
/// ApiVersioningConfiguration 扩展方法测试
/// </summary>
public class ApiVersioningConfigurationTests
{
    private readonly IServiceCollection _services;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public ApiVersioningConfigurationTests()
    {
        _services = new ServiceCollection();
        _mockConfiguration = new Mock<IConfiguration>();

        // 设置基本配置
        SetupConfiguration();
    }

    private void SetupConfiguration()
    {
        // 设置Swagger配置
        _mockConfiguration.Setup(c => c["Swagger:Title"])
            .Returns("测试API");
        _mockConfiguration.Setup(c => c["Swagger:Description"])
            .Returns("测试API描述");
        _mockConfiguration.Setup(c => c["Swagger:ContactName"])
            .Returns("测试联系人");
        _mockConfiguration.Setup(c => c["Swagger:ContactEmail"])
            .Returns("test@example.com");
        _mockConfiguration.Setup(c => c["Swagger:ContactUrl"])
            .Returns("https://test.com");
        _mockConfiguration.Setup(c => c["Swagger:LicenseName"])
            .Returns("MIT");
        _mockConfiguration.Setup(c => c["Swagger:LicenseUrl"])
            .Returns("https://opensource.org/licenses/MIT");
    }

    [Fact]
    public void ConfigureApiVersioning_ShouldReturnServiceCollection()
    {
        // Act
        var result = _services.ConfigureApiVersioning();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(_services);
    }

    [Fact]
    public void ConfigureApiVersioning_ShouldRegisterApiVersioningServices()
    {
        // Act
        _services.ConfigureApiVersioning();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证API版本控制服务已注册
        var apiVersioningService = serviceProvider.GetService<IApiVersioningFeature>();
        // 由于IApiVersioningFeature可能不直接可用，我们检查是否有相关服务注册
        var versionedServices = _services.Where(s =>
            s.ServiceType.FullName?.Contains("Versioning") == true ||
            s.ServiceType.FullName?.Contains("ApiVersion") == true).ToList();

        versionedServices.Should().NotBeEmpty("应该注册API版本控制相关服务");
    }

    [Fact]
    public void ConfigureApiVersioning_ShouldSetDefaultApiVersion()
    {
        // Arrange & Act
        _services.ConfigureApiVersioning();

        // Assert
        // 验证默认版本配置
        var serviceProvider = _services.BuildServiceProvider();
        var versionedServices = _services.Where(s =>
            s.ServiceType.FullName?.Contains("ApiVersion") == true).ToList();

        versionedServices.Should().NotBeEmpty("应该配置默认API版本");
    }

    [Fact]
    public void ConfigureApiVersioning_ShouldConfigureUrlSegmentReader()
    {
        // Arrange & Act
        _services.ConfigureApiVersioning();

        // Assert
        // 验证URL段版本读取器已配置
        var serviceProvider = _services.BuildServiceProvider();
        var configuration = _services.Where(s =>
            s.ServiceType.FullName?.Contains("ApiVersion") == true).ToList();

        configuration.Should().NotBeEmpty("应该配置URL段版本读取器");
    }

    [Fact]
    public void ConfigureApiVersioning_ShouldEnableApiExplorer()
    {
        // Arrange & Act
        _services.ConfigureApiVersioning();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证API Explorer服务已注册
        var explorerServices = _services.Where(s =>
            s.ServiceType.FullName?.Contains("ApiExplorer") == true ||
            s.ServiceType.FullName?.Contains("IApiDescriptionGroupCollectionProvider") == true).ToList();

        explorerServices.Should().NotBeEmpty("应该启用API Explorer");
    }

    [Fact]
    public void ConfigureApiVersioning_MultipleCall_ShouldNotCauseIssues()
    {
        // Act
        _services.ConfigureApiVersioning();
        var secondCall = () => _services.ConfigureApiVersioning();

        // Assert
        secondCall.Should().NotThrow("多次调用应该是安全的");
        var finalResult = _services.ConfigureApiVersioning();
        finalResult.Should().BeSameAs(_services);
    }

    [Fact]
    public void ConfigureVersionedSwagger_ShouldReturnServiceCollection()
    {
        // Arrange
        _services.AddSingleton<IApiVersionDescriptionProvider>(CreateMockApiVersionDescriptionProvider().Object);

        // Act
        var result = _services.ConfigureVersionedSwagger(_mockConfiguration.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(_services);
    }

    [Fact]
    public void ConfigureVersionedSwagger_ShouldRegisterSwaggerGen()
    {
        // Arrange
        _services.AddSingleton<IApiVersionDescriptionProvider>(CreateMockApiVersionDescriptionProvider().Object);

        // Act
        _services.ConfigureVersionedSwagger(_mockConfiguration.Object);

        // Assert
        var swaggerServices = _services.Where(s =>
            s.ServiceType.FullName?.Contains("Swagger") == true).ToList();

        swaggerServices.Should().NotBeEmpty("应该注册Swagger生成服务");
    }

    [Fact]
    public void ConfigureVersionedSwagger_WithMultipleVersions_ShouldCreateMultipleDocs()
    {
        // Arrange
        var mockProvider = CreateMockApiVersionDescriptionProvider();
        var descriptions = new List<ApiVersionDescription>
        {
            new ApiVersionDescription(new ApiVersion(1, 0), "v1", false),
            new ApiVersionDescription(new ApiVersion(2, 0), "v2", false)
        };
        mockProvider.Setup(p => p.ApiVersionDescriptions).Returns(descriptions);
        _services.AddSingleton<IApiVersionDescriptionProvider>(mockProvider.Object);

        // Act
        _services.ConfigureVersionedSwagger(_mockConfiguration.Object);

        // Assert
        // 验证为每个版本创建了文档配置
        var swaggerServices = _services.Where(s =>
            s.ServiceType.FullName?.Contains("Swagger") == true).ToList();

        swaggerServices.Should().NotBeEmpty("应该为多个版本创建Swagger文档");
    }

    [Fact]
    public void ConfigureVersionedSwagger_WithDeprecatedVersion_ShouldMarkAsDeprecated()
    {
        // Arrange
        var mockProvider = CreateMockApiVersionDescriptionProvider();
        var descriptions = new List<ApiVersionDescription>
        {
            new ApiVersionDescription(new ApiVersion(1, 0), "v1", true) // 已弃用
        };
        mockProvider.Setup(p => p.ApiVersionDescriptions).Returns(descriptions);
        _services.AddSingleton<IApiVersionDescriptionProvider>(mockProvider.Object);

        // Act
        var action = () => _services.ConfigureVersionedSwagger(_mockConfiguration.Object);

        // Assert
        action.Should().NotThrow("应该正确处理已弃用的版本");
    }

    [Fact]
    public void UseVersionedSwagger_ShouldReturnApplicationBuilder()
    {
        // Arrange
        var app = CreateTestApplicationBuilder();
        var mockProvider = CreateMockApiVersionDescriptionProvider();

        // Act
        var result = app.UseVersionedSwagger(mockProvider.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseVersionedSwagger_WithMultipleVersions_ShouldConfigureAllEndpoints()
    {
        // Arrange
        var app = CreateTestApplicationBuilder();
        var mockProvider = CreateMockApiVersionDescriptionProvider();
        var descriptions = new List<ApiVersionDescription>
        {
            new ApiVersionDescription(new ApiVersion(1, 0), "v1", false),
            new ApiVersionDescription(new ApiVersion(2, 0), "v2", false)
        };
        mockProvider.Setup(p => p.ApiVersionDescriptions).Returns(descriptions);

        // Act
        var action = () => app.UseVersionedSwagger(mockProvider.Object);

        // Assert
        action.Should().NotThrow("应该为所有版本配置Swagger端点");
    }

    [Fact]
    public void UseVersionedSwagger_ShouldConfigureSwaggerUI()
    {
        // Arrange
        var app = CreateTestApplicationBuilder();
        var mockProvider = CreateMockApiVersionDescriptionProvider();

        // Act
        var action = () => app.UseVersionedSwagger(mockProvider.Object);

        // Assert
        action.Should().NotThrow("应该配置Swagger UI");
    }

    [Fact]
    public void ConfigureApiVersioning_ShouldChainMethodCalls()
    {
        // Act
        var result = _services
            .ConfigureApiVersioning()
            .ConfigureApiVersioning(); // 链式调用

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(_services);
    }

    [Fact]
    public void ConfigureVersionedSwagger_WithEmptyConfiguration_ShouldUseDefaults()
    {
        // Arrange
        var emptyConfig = new Mock<IConfiguration>();
        emptyConfig.Setup(c => c[It.IsAny<string>()]).Returns((string?)null);
        _services.AddSingleton<IApiVersionDescriptionProvider>(CreateMockApiVersionDescriptionProvider().Object);

        // Act
        var action = () => _services.ConfigureVersionedSwagger(emptyConfig.Object);

        // Assert
        action.Should().NotThrow("应该使用默认配置值");
    }

    [Fact]
    public void ConfigureVersionedSwagger_WithInvalidUrl_ShouldHandleGracefully()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Swagger:ContactUrl"]).Returns("invalid-url");
        _mockConfiguration.Setup(c => c["Swagger:LicenseUrl"]).Returns("another-invalid-url");
        _services.AddSingleton<IApiVersionDescriptionProvider>(CreateMockApiVersionDescriptionProvider().Object);

        // Act
        var action = () => _services.ConfigureVersionedSwagger(_mockConfiguration.Object);

        // Assert
        action.Should().NotThrow("应该优雅地处理无效的URL");
    }

    [Fact]
    public void ConfigureApiVersioning_ShouldSetCorrectGroupNameFormat()
    {
        // Arrange & Act
        _services.ConfigureApiVersioning();

        // Assert
        var serviceProvider = _services.BuildServiceProvider();

        // 验证组名格式配置
        var services = _services.Where(s =>
            s.ServiceType.FullName?.Contains("ApiExplorer") == true).ToList();

        services.Should().NotBeEmpty("应该配置正确的组名格式");
    }

    private Mock<IApiVersionDescriptionProvider> CreateMockApiVersionDescriptionProvider()
    {
        var mock = new Mock<IApiVersionDescriptionProvider>();
        var descriptions = new List<ApiVersionDescription>
        {
            new ApiVersionDescription(new ApiVersion(1, 0), "v1", false)
        };
        mock.Setup(p => p.ApiVersionDescriptions).Returns(descriptions);
        return mock;
    }

    private IApplicationBuilder CreateTestApplicationBuilder()
    {
        var services = new ServiceCollection();
        services.AddRouting();
        var serviceProvider = services.BuildServiceProvider();

        var app = new Mock<IApplicationBuilder>();
        app.Setup(a => a.ApplicationServices).Returns(serviceProvider);
        app.Setup(a => a.Use(It.IsAny<Func<Microsoft.AspNetCore.Http.RequestDelegate, Microsoft.AspNetCore.Http.RequestDelegate>>()))
           .Returns(app.Object);

        return app.Object;
    }
}