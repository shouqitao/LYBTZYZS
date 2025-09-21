using FluentAssertions;
using LYBT.WebAPI.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace LYBT.WebAPI.UnitTests.Extensions;

/// <summary>
/// 简化版扩展方法测试 - 专注于核心功能验证
/// </summary>
public class SimpleExtensionTests
{
    [Fact]
    public void ServiceCollectionExtension_AddAllModules_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var action = () => services.AddAllModules();

        // Assert
        action.Should().NotThrow("AddAllModules应该能够正常执行");
    }

    [Fact]
    public void ServiceCollectionExtension_AddAllModules_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddAllModules();

        // Assert
        result.Should().BeSameAs(services, "应该返回原始的ServiceCollection实例");
    }

    [Fact]
    public void ProductionConfigValidationFilter_Constructor_ShouldNotThrow()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns("Server=test;Database=test;");
        mockConfig.Setup(c => c["JwtOptions:Secret"])
            .Returns("TestSecretKeyThatIsLongEnoughForValidation123456789");

        // Act
        var action = () => new ProductionConfigValidationFilter(mockConfig.Object);

        // Assert
        action.Should().NotThrow("ProductionConfigValidationFilter构造函数应该能够正常执行");
    }

    [Fact]
    public void ApiVersioningConfiguration_ConfigureApiVersioning_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var action = () => services.ConfigureApiVersioning();

        // Assert
        action.Should().NotThrow("ConfigureApiVersioning应该能够正常执行");
    }

    [Fact]
    public void PerformanceOptimization_ConfigurePerformanceOptimizations_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c.GetValue<int>("Performance:MinWorkerThreads", 50)).Returns(50);
        mockConfig.Setup(c => c.GetValue<int>("Performance:MinIoThreads", 50)).Returns(50);

        // Act
        var action = () => services.ConfigurePerformanceOptimizations(mockConfig.Object);

        // Assert
        action.Should().NotThrow("ConfigurePerformanceOptimizations应该能够正常执行");
    }

    [Fact]
    public void UnifiedServiceRegistration_RegisterAllApplicationServices_WithBasicConfig_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConfig = new Mock<IConfiguration>();
        var mockEnv = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

        // 设置基本配置
        mockConfig.Setup(c => c.GetConnectionString("DefaultConnection"))
            .Returns("Server=localhost;Database=TestDB;Trusted_Connection=true;");

        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(s => s["Secret"]).Returns("TestSecretKeyForJWTAuthentication_AtLeast32Characters");
        jwtSection.Setup(s => s["Issuer"]).Returns("TestIssuer");
        jwtSection.Setup(s => s["Audience"]).Returns("TestAudience");
        mockConfig.Setup(c => c.GetSection("JwtOptions")).Returns(jwtSection.Object);

        // 设置其他必需的配置节
        var emptySection = new Mock<IConfigurationSection>();
        emptySection.Setup(s => s.GetChildren()).Returns(new List<IConfigurationSection>());
        mockConfig.Setup(c => c.GetSection(It.IsAny<string>())).Returns(emptySection.Object);

        mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

        // Act
        var action = () => services.RegisterAllApplicationServices(mockConfig.Object, mockEnv.Object);

        // Assert
        action.Should().NotThrow("RegisterAllApplicationServices应该能够正常执行");
    }

    [Fact]
    public void ServiceMethods_ShouldReturnSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConfig = new Mock<IConfiguration>();
        var mockEnv = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

        // 设置最小配置
        mockConfig.Setup(c => c.GetConnectionString("DefaultConnection")).Returns("test");
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(s => s["Secret"]).Returns("TestSecretKeyForJWTAuthentication_AtLeast32Characters");
        mockConfig.Setup(c => c.GetSection("JwtOptions")).Returns(jwtSection.Object);

        var emptySection = new Mock<IConfigurationSection>();
        mockConfig.Setup(c => c.GetSection(It.IsAny<string>())).Returns(emptySection.Object);

        mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

        // Act & Assert
        services.AddAllModules().Should().BeSameAs(services);
        services.ConfigureApiVersioning().Should().BeSameAs(services);
        services.ConfigurePerformanceOptimizations(mockConfig.Object).Should().BeSameAs(services);
    }
}