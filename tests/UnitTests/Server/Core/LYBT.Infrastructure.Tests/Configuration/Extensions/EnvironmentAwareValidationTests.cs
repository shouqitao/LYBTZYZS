using LYBT.Infrastructure.Configuration.Extensions;
using LYBT.Infrastructure.Configuration.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;

namespace LYBT.Infrastructure.Tests.Configuration.Extensions
{
    public class EnvironmentAwareValidationTests
    {
        private readonly IServiceCollection _services;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;

        public EnvironmentAwareValidationTests()
        {
            _services = new ServiceCollection();
            _mockEnvironment = new Mock<IWebHostEnvironment>();
        }

        #region AddEnvironmentAwareValidation Tests

        [Fact]
        public void AddEnvironmentAwareValidation_Should_ReturnServiceCollection_When_Called()
        {
            // Act
            var result = _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(_services);
        }

        [Fact]
        public void AddEnvironmentAwareValidation_Should_ThrowArgumentNullException_When_ServicesIsNull()
        {
            // Arrange
            IServiceCollection nullServices = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                nullServices.AddEnvironmentAwareValidation(_mockEnvironment.Object));
        }

        [Fact]
        public void AddEnvironmentAwareValidation_Should_ThrowArgumentNullException_When_EnvironmentIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _services.AddEnvironmentAwareValidation(null));
        }

        [Fact]
        public void AddEnvironmentAwareValidation_Should_ConfigureOptionsPostConfigure_When_Called()
        {
            // Act
            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);

            // Assert
            _services.Should().NotBeEmpty();
            // Verify that options configuration services were added
            var serviceTypes = _services.Select(s => s.ServiceType).ToList();
            serviceTypes.Should().Contain(typeof(IConfigureOptions<DefaultPasswordOptions>));
        }

        #endregion

        #region DefaultPasswordOptions Validation Tests

        [Fact]
        public void ValidateDefaultPasswordOptions_Should_ThrowException_When_ProductionAndAllowInProductionTrue()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);
            var options = new DefaultPasswordOptions
            {
                AllowInProduction = true,
                SystemAdmin = "VeryLongPasswordWith16Characters!",
                NewUser = "LongPassword12!"
            };

            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);
            _services.Configure<DefaultPasswordOptions>(opt =>
            {
                opt.AllowInProduction = options.AllowInProduction;
                opt.SystemAdmin = options.SystemAdmin;
                opt.NewUser = options.NewUser;
            });

            var serviceProvider = _services.BuildServiceProvider();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<IOptions<DefaultPasswordOptions>>().Value);
        }

        [Fact]
        public void ValidateDefaultPasswordOptions_Should_ThrowException_When_ProductionAndSystemAdminPasswordTooShort()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);
            var options = new DefaultPasswordOptions
            {
                AllowInProduction = false,
                SystemAdmin = "Short123!",  // Less than 16 characters
                NewUser = "LongEnoughPassword123!"
            };

            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);
            _services.Configure<DefaultPasswordOptions>(opt =>
            {
                opt.AllowInProduction = options.AllowInProduction;
                opt.SystemAdmin = options.SystemAdmin;
                opt.NewUser = options.NewUser;
            });

            var serviceProvider = _services.BuildServiceProvider();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<IOptions<DefaultPasswordOptions>>().Value);
        }

        [Fact]
        public void ValidateDefaultPasswordOptions_Should_ThrowException_When_ProductionAndNewUserPasswordTooShort()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);
            var options = new DefaultPasswordOptions
            {
                AllowInProduction = false,
                SystemAdmin = "VeryLongPasswordWith16Characters!",
                NewUser = "Short123!" // Less than 12 characters
            };

            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);
            _services.Configure<DefaultPasswordOptions>(opt =>
            {
                opt.AllowInProduction = options.AllowInProduction;
                opt.SystemAdmin = options.SystemAdmin;
                opt.NewUser = options.NewUser;
            });

            var serviceProvider = _services.BuildServiceProvider();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<IOptions<DefaultPasswordOptions>>().Value);
        }

        [Fact]
        public void ValidateDefaultPasswordOptions_Should_ThrowException_When_ProductionAndPasswordNotComplex()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);
            var options = new DefaultPasswordOptions
            {
                AllowInProduction = false,
                SystemAdmin = "verylongpasswordwithoutspecialcharacters", // No complexity
                NewUser = "LongEnoughPassword123!"
            };

            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);
            _services.Configure<DefaultPasswordOptions>(opt =>
            {
                opt.AllowInProduction = options.AllowInProduction;
                opt.SystemAdmin = options.SystemAdmin;
                opt.NewUser = options.NewUser;
            });

            var serviceProvider = _services.BuildServiceProvider();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<IOptions<DefaultPasswordOptions>>().Value);
        }

        [Fact]
        public void ValidateDefaultPasswordOptions_Should_NotThrow_When_DevelopmentEnvironment()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
            var options = new DefaultPasswordOptions
            {
                AllowInProduction = true, // This would fail in production
                SystemAdmin = "short",    // This would fail in production
                NewUser = "short",        // This would fail in production
                EnableInDevelopment = false
            };

            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);
            _services.Configure<DefaultPasswordOptions>(opt =>
            {
                opt.AllowInProduction = options.AllowInProduction;
                opt.SystemAdmin = options.SystemAdmin;
                opt.NewUser = options.NewUser;
                opt.EnableInDevelopment = options.EnableInDevelopment;
            });

            var serviceProvider = _services.BuildServiceProvider();

            // Act & Assert
            var configuredOptions = serviceProvider.GetRequiredService<IOptions<DefaultPasswordOptions>>().Value;
            configuredOptions.Should().NotBeNull();
        }

        #endregion

        #region SecurityOptions Validation Tests

        [Fact]
        public void ValidateSecurityOptions_Should_ThrowException_When_ProductionAndHttpsNotRequired()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);

            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);
            _services.Configure<SecurityOptions>(opt =>
            {
                opt.Https = new HttpsOptions { RequireHttps = false };
                opt.SecurityHeaders = new SecurityHeadersOptions { ContentSecurityPolicy = "default-src 'self'" };
                opt.PasswordPolicy = new PasswordPolicy { MinLength = 12, RequireUppercase = true, RequireLowercase = true, RequireDigit = true, RequireSpecialChar = true };
            });

            var serviceProvider = _services.BuildServiceProvider();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value);
        }

        [Fact]
        public void ValidateSecurityOptions_Should_ThrowException_When_ProductionAndNoContentSecurityPolicy()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);

            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);
            _services.Configure<SecurityOptions>(opt =>
            {
                opt.Https = new HttpsOptions { RequireHttps = true };
                opt.SecurityHeaders = new SecurityHeadersOptions { ContentSecurityPolicy = "" }; // Empty
                opt.PasswordPolicy = new PasswordPolicy { MinLength = 12, RequireUppercase = true, RequireLowercase = true, RequireDigit = true, RequireSpecialChar = true };
            });

            var serviceProvider = _services.BuildServiceProvider();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value);
        }

        [Fact]
        public void ValidateSecurityOptions_Should_ThrowException_When_ProductionAndPasswordPolicyTooWeak()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);

            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);
            _services.Configure<SecurityOptions>(opt =>
            {
                opt.Https = new HttpsOptions { RequireHttps = true };
                opt.SecurityHeaders = new SecurityHeadersOptions { ContentSecurityPolicy = "default-src 'self'" };
                opt.PasswordPolicy = new PasswordPolicy
                {
                    MinLength = 8, // Too short for production
                    RequireUppercase = true,
                    RequireLowercase = true,
                    RequireDigit = true,
                    RequireSpecialChar = true
                };
            });

            var serviceProvider = _services.BuildServiceProvider();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value);
        }

        #endregion

        #region DatabaseOptions Validation Tests

        [Fact]
        public void ValidateDatabaseOptions_Should_ThrowException_When_ProductionAndSensitiveDataLoggingEnabled()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);

            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);
            _services.Configure<DatabaseOptions>(opt =>
            {
                opt.EnableSensitiveDataLogging = true; // Not allowed in production
                opt.EnableDetailedErrors = false;
            });

            var serviceProvider = _services.BuildServiceProvider();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value);
        }

        [Fact]
        public void ValidateDatabaseOptions_Should_ThrowException_When_ProductionAndDetailedErrorsEnabled()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);

            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);
            _services.Configure<DatabaseOptions>(opt =>
            {
                opt.EnableSensitiveDataLogging = false;
                opt.EnableDetailedErrors = true; // Not allowed in production
            });

            var serviceProvider = _services.BuildServiceProvider();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value);
        }

        [Fact]
        public void ValidateDatabaseOptions_Should_NotThrow_When_DevelopmentEnvironment()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);

            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);
            _services.Configure<DatabaseOptions>(opt =>
            {
                opt.EnableSensitiveDataLogging = true; // Allowed in development
                opt.EnableDetailedErrors = true;       // Allowed in development
            });

            var serviceProvider = _services.BuildServiceProvider();

            // Act & Assert
            var configuredOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            configuredOptions.Should().NotBeNull();
            configuredOptions.EnableSensitiveDataLogging.Should().BeTrue();
            configuredOptions.EnableDetailedErrors.Should().BeTrue();
        }

        #endregion

        #region Edge Cases and Complex Scenarios

        [Fact]
        public void AddEnvironmentAwareValidation_Should_HandleMultipleCallsGracefully_When_CalledMultipleTimes()
        {
            // Act
            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);
            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);

            // Assert
            _services.Should().NotBeEmpty();
            // Should not throw and handle multiple registrations
        }

        [Fact]
        public void ValidateOptions_Should_NotThrow_When_StagingEnvironmentWithSafeSettings()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Staging);

            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);
            _services.Configure<DefaultPasswordOptions>(opt =>
            {
                opt.AllowInProduction = false;
                opt.SystemAdmin = "VeryLongSecurePassword123!";
                opt.NewUser = "SecurePassword123!";
            });

            var serviceProvider = _services.BuildServiceProvider();

            // Act & Assert
            var configuredOptions = serviceProvider.GetRequiredService<IOptions<DefaultPasswordOptions>>().Value;
            configuredOptions.Should().NotBeNull();
        }

        [Fact]
        public void ValidateOptions_Should_HandleValidProductionConfiguration_When_AllSettingsCorrect()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);

            _services.AddEnvironmentAwareValidation(_mockEnvironment.Object);
            _services.Configure<DefaultPasswordOptions>(opt =>
            {
                opt.AllowInProduction = false;
                opt.SystemAdmin = "VeryLongSecurePassword123!@#";
                opt.NewUser = "SecurePassword123!";
            });
            _services.Configure<SecurityOptions>(opt =>
            {
                opt.Https = new HttpsOptions { RequireHttps = true };
                opt.SecurityHeaders = new SecurityHeadersOptions { ContentSecurityPolicy = "default-src 'self'" };
                opt.PasswordPolicy = new PasswordPolicy
                {
                    MinLength = 12,
                    RequireUppercase = true,
                    RequireLowercase = true,
                    RequireDigit = true,
                    RequireSpecialChar = true
                };
            });
            _services.Configure<DatabaseOptions>(opt =>
            {
                opt.EnableSensitiveDataLogging = false;
                opt.EnableDetailedErrors = false;
            });

            var serviceProvider = _services.BuildServiceProvider();

            // Act & Assert
            var passwordOptions = serviceProvider.GetRequiredService<IOptions<DefaultPasswordOptions>>().Value;
            var securityOptions = serviceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value;
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            passwordOptions.Should().NotBeNull();
            securityOptions.Should().NotBeNull();
            databaseOptions.Should().NotBeNull();
        }

        #endregion
    }

    // Helper classes to ensure all necessary options are available for testing
    public class HttpsOptions
    {
        public bool RequireHttps { get; set; }
    }

    public class SecurityHeadersOptions
    {
        public string ContentSecurityPolicy { get; set; } = string.Empty;
    }

    public class SecurityOptions
    {
        public HttpsOptions Https { get; set; } = new();
        public SecurityHeadersOptions SecurityHeaders { get; set; } = new();
        public PasswordPolicy PasswordPolicy { get; set; } = new();
    }
}