using FluentAssertions;
using LYBT.Shared.Utilities.Extensions.ServiceCollection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using ServiceCollection = Microsoft.Extensions.DependencyInjection.ServiceCollection;

namespace LYBT.Shared.Utilities.Tests.Extensions.ServiceCollection
{
    /// <summary>
    /// AuthenticationExtensions扩展方法单元测试
    /// </summary>
    public class AuthenticationExtensionsTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IConfigurationSection> _mockJwtSection;
        private readonly IServiceCollection _services;

        public AuthenticationExtensionsTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockJwtSection = new Mock<IConfigurationSection>();
            _services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

            // 设置默认的JWT配置节
            _mockConfiguration.Setup(x => x.GetSection("JwtOptions")).Returns(_mockJwtSection.Object);
        }

        [Fact]
        public void AddJwtBearerAuthentication_WithValidSecret_ShouldAddAuthenticationServices()
        {
            // Arrange
            Environment.SetEnvironmentVariable("JWT_SECRET", "test-secret-key-12345678901234567890");
            _mockJwtSection.Setup(x => x["Issuer"]).Returns("Test-Issuer");
            _mockJwtSection.Setup(x => x["Audience"]).Returns("Test-Audience");

            try
            {
                // Act
                var result = _services.AddJwtBearerAuthentication(_mockConfiguration.Object, "Development");

                // Assert
                result.Should().BeSameAs(_services);
                _services.Should().NotBeEmpty();
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("JWT_SECRET", null);
            }
        }

        [Fact]
        public void AddJwtBearerAuthentication_WithoutSecret_ShouldThrowException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("JWT_SECRET", null);
            _mockConfiguration.Setup(x => x["JwtOptions:Secret"]).Returns((string?)null);

            // Act & Assert
            var act = () => _services.AddJwtBearerAuthentication(_mockConfiguration.Object, "Development");
            act.Should().Throw<InvalidOperationException>().WithMessage("JWT密钥未配置");
        }

        [Fact]
        public void AddJwtBearerAuthentication_WithoutSecretInProduction_ShouldThrowSpecificException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("JWT_SECRET", null);
            _mockConfiguration.Setup(x => x["JwtOptions:Secret"]).Returns((string?)null);

            // Act & Assert
            var act = () => _services.AddJwtBearerAuthentication(_mockConfiguration.Object, "Production");
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("生产环境必须通过JWT_SECRET环境变量或JwtOptions:Secret配置JWT密钥");
        }
    }
}