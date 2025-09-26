using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using LYBT.Infrastructure.Security;
using LYBT.Infrastructure.Configuration.Options;

namespace Infrastructure.UnitTests.Security;

public class KeyManagementServiceFactoryTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<KeyManagementService>> _mockLogger;
    private readonly Mock<IOptions<JwtOptions>> _mockJwtOptions;
    private readonly KeyManagementServiceFactory _factory;

    public KeyManagementServiceFactoryTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<KeyManagementService>>();
        _mockJwtOptions = new Mock<IOptions<JwtOptions>>();
        
        _factory = new KeyManagementServiceFactory(_mockServiceProvider.Object);
    }

    [Fact]
    public void Constructor_WithValidServiceProvider_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => new KeyManagementServiceFactory(_mockServiceProvider.Object);
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new KeyManagementServiceFactory(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public void CreateKeyManagementService_WithValidDependencies_ShouldReturnKeyManagementService()
    {
        // Arrange
        var jwtOptions = new JwtOptions
        {
            Secret = "test-secret-key-that-is-long-enough-for-jwt",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpireMinutes = 60
        };
        
        _mockJwtOptions.Setup(x => x.Value).Returns(jwtOptions);
        
        _mockServiceProvider.Setup(x => x.GetService(typeof(ILogger<KeyManagementService>)))
            .Returns(_mockLogger.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IOptions<JwtOptions>)))
            .Returns(_mockJwtOptions.Object);

        // Act
        var result = _factory.CreateKeyManagementService();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<KeyManagementService>();
        
        _mockServiceProvider.Verify(x => x.GetService(typeof(ILogger<KeyManagementService>)), Times.Once);
        _mockServiceProvider.Verify(x => x.GetService(typeof(IOptions<JwtOptions>)), Times.Once);
    }

    [Fact]
    public void CreateKeyManagementService_WithMissingLogger_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockServiceProvider.Setup(x => x.GetService(typeof(ILogger<KeyManagementService>)))
            .Returns(null);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IOptions<JwtOptions>)))
            .Returns(_mockJwtOptions.Object);

        // Act & Assert
        var action = () => _factory.CreateKeyManagementService();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("无法从服务容器中获取 ILogger<KeyManagementService> 服务");
    }

    [Fact]
    public void CreateKeyManagementService_WithMissingJwtOptions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockServiceProvider.Setup(x => x.GetService(typeof(ILogger<KeyManagementService>)))
            .Returns(_mockLogger.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IOptions<JwtOptions>)))
            .Returns(null);

        // Act & Assert
        var action = () => _factory.CreateKeyManagementService();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("无法从服务容器中获取 IOptions<JwtOptions> 服务");
    }

    [Fact]
    public void CreateKeyManagementService_CalledMultipleTimes_ShouldReturnNewInstancesEachTime()
    {
        // Arrange
        var jwtOptions = new JwtOptions
        {
            Secret = "test-secret-key-that-is-long-enough-for-jwt",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpireMinutes = 60
        };
        
        _mockJwtOptions.Setup(x => x.Value).Returns(jwtOptions);
        
        _mockServiceProvider.Setup(x => x.GetService(typeof(ILogger<KeyManagementService>)))
            .Returns(_mockLogger.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IOptions<JwtOptions>)))
            .Returns(_mockJwtOptions.Object);

        // Act
        var result1 = _factory.CreateKeyManagementService();
        var result2 = _factory.CreateKeyManagementService();

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Should().NotBeSameAs(result2);
    }

    [Fact]
    public void CreateKeyManagementService_WithNullServiceProviderReturns_ShouldThrowInvalidOperationException()
    {
        // Arrange - 模拟GetService返回null的情况
        _mockServiceProvider.Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns(null);

        // Act & Assert
        var action = () => _factory.CreateKeyManagementService();
        action.Should().Throw<InvalidOperationException>()
            .And.Message.Should().Contain("无法从服务容器中获取");
    }
}