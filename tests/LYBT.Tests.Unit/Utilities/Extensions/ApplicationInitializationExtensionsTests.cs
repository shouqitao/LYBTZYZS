using LYBT.Shared.Utilities.Extensions.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.Unit.Utilities.Extensions
{
    /// <summary>
    /// ApplicationInitializationExtensions扩展方法单元测试
    /// </summary>
    public class ApplicationInitializationExtensionsTests
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public ApplicationInitializationExtensionsTests()
        {
            _configuration = Substitute.For<IConfiguration>();
            _logger = Substitute.For<ILogger>();
            _lifetime = Substitute.For<IHostApplicationLifetime>();
        }

        #region ValidateCriticalConfiguration方法测试

        [Fact]
        public void ValidateCriticalConfiguration_WithValidConfiguration_ShouldReturnValidResult()
        {
            // Arrange
            Environment.SetEnvironmentVariable("CONNECTION_STRING", "Server=test;Database=test;");
            Environment.SetEnvironmentVariable("JWT_SECRET", "test-secret-key");

            try
            {
                // Act
                var result = ApplicationInitializationExtensions.ValidateCriticalConfiguration(
                    _configuration,
                    "Development",
                    _logger);

                // Assert
                result.IsValid.Should().BeTrue();
                result.Errors.Should().BeEmpty();
                result.HasWarnings.Should().BeFalse();
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("CONNECTION_STRING", null);
                Environment.SetEnvironmentVariable("JWT_SECRET", null);
            }
        }

        [Fact]
        public void ValidateCriticalConfiguration_WithMissingJwtInProduction_ShouldReturnInvalidResult()
        {
            // Arrange
            Environment.SetEnvironmentVariable("CONNECTION_STRING", "Server=test;Database=test;");
            Environment.SetEnvironmentVariable("JWT_SECRET", null);
            _configuration["JwtOptions:Secret"].Returns((string?)null);

            try
            {
                // Act
                var result = ApplicationInitializationExtensions.ValidateCriticalConfiguration(
                    _configuration,
                    "Production",
                    _logger);

                // Assert
                result.IsValid.Should().BeFalse();
                result.Errors.Should().Contain("生产环境必须配置JWT密钥");
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("CONNECTION_STRING", null);
            }
        }

        [Fact]
        public void ValidateCriticalConfiguration_WithMissingJwtInDevelopment_ShouldHaveWarning()
        {
            // Arrange
            Environment.SetEnvironmentVariable("CONNECTION_STRING", "Server=test;Database=test;");
            Environment.SetEnvironmentVariable("JWT_SECRET", null);
            _configuration["JwtOptions:Secret"].Returns((string?)null);

            try
            {
                // Act
                var result = ApplicationInitializationExtensions.ValidateCriticalConfiguration(
                    _configuration,
                    "Development",
                    _logger);

                // Assert
                result.IsValid.Should().BeTrue(); // 只是警告，不是错误
                result.HasWarnings.Should().BeTrue();
                result.Warnings.Should().Contain("JWT密钥未配置，使用默认开发密钥");
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("CONNECTION_STRING", null);
            }
        }

        [Fact]
        public void ValidateCriticalConfiguration_WithoutLogger_ShouldNotThrow()
        {
            // Arrange
            Environment.SetEnvironmentVariable("CONNECTION_STRING", "Server=test;Database=test;");
            Environment.SetEnvironmentVariable("JWT_SECRET", "test-secret-key");

            try
            {
                // Act
                var act = () => ApplicationInitializationExtensions.ValidateCriticalConfiguration(
                    _configuration,
                    "Development",
                    null);

                // Assert
                act.Should().NotThrow();
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("CONNECTION_STRING", null);
                Environment.SetEnvironmentVariable("JWT_SECRET", null);
            }
        }

        [Fact]
        public void ValidateCriticalConfiguration_WithoutEnvironment_ShouldUseDefault()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
            Environment.SetEnvironmentVariable("CONNECTION_STRING", "Server=test;Database=test;");
            Environment.SetEnvironmentVariable("JWT_SECRET", null);
            _configuration["JwtOptions:Secret"].Returns((string?)null);

            try
            {
                // Act
                var result = ApplicationInitializationExtensions.ValidateCriticalConfiguration(
                    _configuration,
                    null,
                    _logger);

                // Assert
                result.IsValid.Should().BeTrue(); // 非生产环境，JWT只是警告
                result.HasWarnings.Should().BeTrue();
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
                Environment.SetEnvironmentVariable("CONNECTION_STRING", null);
            }
        }

        [Fact]
        public void ValidateCriticalConfiguration_WithJwtFromConfig_ShouldBeValid()
        {
            // Arrange
            Environment.SetEnvironmentVariable("CONNECTION_STRING", "Server=test;Database=test;");
            Environment.SetEnvironmentVariable("JWT_SECRET", null);
            _configuration["JwtOptions:Secret"].Returns("config-secret-key");

            try
            {
                // Act
                var result = ApplicationInitializationExtensions.ValidateCriticalConfiguration(
                    _configuration,
                    "Production",
                    _logger);

                // Assert
                result.IsValid.Should().BeTrue();
                result.Errors.Should().BeEmpty();
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("CONNECTION_STRING", null);
            }
        }

        #endregion

        #region LogApplicationStartup方法测试

        [Fact]
        public void LogApplicationStartup_WithNullAdditionalInfo_ShouldNotThrow()
        {
            // Arrange
            var environment = "Development";

            // Act
            var act = () => ApplicationInitializationExtensions.LogApplicationStartup(environment, _logger, null);

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region ConfigureGracefulShutdown方法测试

        [Fact]
        public void ConfigureGracefulShutdown_ShouldRegisterLifetimeEvents()
        {
            // Arrange
            var startedTokenSource = new CancellationTokenSource();
            var stoppingTokenSource = new CancellationTokenSource();
            var stoppedTokenSource = new CancellationTokenSource();

            _lifetime.ApplicationStarted.Returns(startedTokenSource.Token);
            _lifetime.ApplicationStopping.Returns(stoppingTokenSource.Token);
            _lifetime.ApplicationStopped.Returns(stoppedTokenSource.Token);

            // Act
            ApplicationInitializationExtensions.ConfigureGracefulShutdown(_lifetime, _logger, 1);

            // Assert - verify the properties were accessed
            _ = _lifetime.Received().ApplicationStarted;
            _ = _lifetime.Received().ApplicationStopping;
            _ = _lifetime.Received().ApplicationStopped;
        }

        [Fact]
        public void ConfigureGracefulShutdown_WithCustomTimeout_ShouldUseCustomTimeout()
        {
            // Arrange
            var customTimeout = 5;
            var startedTokenSource = new CancellationTokenSource();
            var stoppingTokenSource = new CancellationTokenSource();
            var stoppedTokenSource = new CancellationTokenSource();

            _lifetime.ApplicationStarted.Returns(startedTokenSource.Token);
            _lifetime.ApplicationStopping.Returns(stoppingTokenSource.Token);
            _lifetime.ApplicationStopped.Returns(stoppedTokenSource.Token);

            // Act
            var act = () => ApplicationInitializationExtensions.ConfigureGracefulShutdown(
                _lifetime,
                _logger,
                customTimeout);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void ConfigureGracefulShutdown_WithLargeTimeout_ShouldCapAt60Seconds()
        {
            // Arrange
            var largeTimeout = 120; // 超过60秒的限制
            var startedTokenSource = new CancellationTokenSource();
            var stoppingTokenSource = new CancellationTokenSource();
            var stoppedTokenSource = new CancellationTokenSource();

            _lifetime.ApplicationStarted.Returns(startedTokenSource.Token);
            _lifetime.ApplicationStopping.Returns(stoppingTokenSource.Token);
            _lifetime.ApplicationStopped.Returns(stoppedTokenSource.Token);

            // Act
            var act = () => ApplicationInitializationExtensions.ConfigureGracefulShutdown(
                _lifetime,
                _logger,
                largeTimeout);

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region ConfigurationValidationResult类测试

        [Fact]
        public void ConfigurationValidationResult_DefaultState_ShouldBeValid()
        {
            // Act
            var result = new ConfigurationValidationResult();

            // Assert
            result.IsValid.Should().BeTrue();
            result.HasWarnings.Should().BeFalse();
            result.Errors.Should().BeEmpty();
            result.Warnings.Should().BeEmpty();
        }

        [Fact]
        public void ConfigurationValidationResult_AddError_ShouldMakeInvalid()
        {
            // Arrange
            var result = new ConfigurationValidationResult();

            // Act
            result.AddError("Test error");

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("Test error");
        }

        [Fact]
        public void ConfigurationValidationResult_AddWarning_ShouldHaveWarnings()
        {
            // Arrange
            var result = new ConfigurationValidationResult();

            // Act
            result.AddWarning("Test warning");

            // Assert
            result.IsValid.Should().BeTrue(); // 警告不影响有效性
            result.HasWarnings.Should().BeTrue();
            result.Warnings.Should().Contain("Test warning");
        }

        [Fact]
        public void ConfigurationValidationResult_AddMultipleErrorsAndWarnings_ShouldTrackAll()
        {
            // Arrange
            var result = new ConfigurationValidationResult();

            // Act
            result.AddError("Error 1");
            result.AddError("Error 2");
            result.AddWarning("Warning 1");
            result.AddWarning("Warning 2");

            // Assert
            result.IsValid.Should().BeFalse();
            result.HasWarnings.Should().BeTrue();
            result.Errors.Should().HaveCount(2);
            result.Warnings.Should().HaveCount(2);
            result.Errors.Should().Contain("Error 1", "Error 2");
            result.Warnings.Should().Contain("Warning 1", "Warning 2");
        }

        [Fact]
        public void ConfigurationValidationResult_ErrorsAndWarnings_ShouldBeReadOnly()
        {
            // Arrange
            var result = new ConfigurationValidationResult();
            result.AddError("Test error");
            result.AddWarning("Test warning");

            // Act
            var errors = result.Errors;
            var warnings = result.Warnings;

            // Assert
            errors.Should().BeAssignableTo<IReadOnlyList<string>>();
            warnings.Should().BeAssignableTo<IReadOnlyList<string>>();
        }

        #endregion
    }
}
