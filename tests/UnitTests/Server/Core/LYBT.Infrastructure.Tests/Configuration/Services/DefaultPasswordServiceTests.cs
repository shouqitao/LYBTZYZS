using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Configuration.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;

namespace LYBT.Infrastructure.Tests.Configuration.Services
{
    public class DefaultPasswordServiceTests
    {
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<IOptions<DefaultPasswordOptions>> _mockOptions;
        private readonly DefaultPasswordOptions _options;
        private readonly DefaultPasswordService _service;

        public DefaultPasswordServiceTests()
        {
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockOptions = new Mock<IOptions<DefaultPasswordOptions>>();

            _options = new DefaultPasswordOptions
            {
                SystemAdmin = "admin123",
                NewUser = "newuser123",
                EnableInDevelopment = true,
                AllowInProduction = false,
                OnlyWhenDatabaseEmpty = true,
                ExpiryDays = 30
            };

            _mockOptions.Setup(x => x.Value).Returns(_options);
            _service = new DefaultPasswordService(_mockOptions.Object, _mockEnvironment.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_CreateInstance_When_ValidParametersProvided()
        {
            // Act & Assert
            _service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_OptionsIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DefaultPasswordService(null, _mockEnvironment.Object));
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_EnvironmentIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DefaultPasswordService(_mockOptions.Object, null));
        }

        #endregion

        #region GetSystemAdminPassword Tests

        [Fact]
        public void GetSystemAdminPassword_Should_ReturnPassword_When_AllowedInDevelopment()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);

            // Act
            var result = _service.GetSystemAdminPassword();

            // Assert
            result.Should().Be("admin123");
        }

        [Fact]
        public void GetSystemAdminPassword_Should_ReturnNull_When_ProductionEnvironment()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);

            // Act
            var result = _service.GetSystemAdminPassword();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetSystemAdminPassword_Should_ReturnNull_When_NotAllowedInDevelopment()
        {
            // Arrange
            _options.EnableInDevelopment = false;
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);

            // Act
            var result = _service.GetSystemAdminPassword();

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetNewUserPassword Tests

        [Fact]
        public void GetNewUserPassword_Should_ReturnPassword_When_AllowedInDevelopment()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);

            // Act
            var result = _service.GetNewUserPassword();

            // Assert
            result.Should().Be("newuser123");
        }

        [Fact]
        public void GetNewUserPassword_Should_ReturnNull_When_ProductionEnvironment()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);

            // Act
            var result = _service.GetNewUserPassword();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetNewUserPassword_Should_ReturnNull_When_NotAllowedInDevelopment()
        {
            // Arrange
            _options.EnableInDevelopment = false;
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);

            // Act
            var result = _service.GetNewUserPassword();

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region IsDefaultPasswordAllowed Tests

        [Fact]
        public void IsDefaultPasswordAllowed_Should_ReturnFalse_When_ProductionEnvironment()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);

            // Act
            var result = _service.IsDefaultPasswordAllowed();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsDefaultPasswordAllowed_Should_ReturnTrue_When_DevelopmentAndEnabled()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
            _options.EnableInDevelopment = true;

            // Act
            var result = _service.IsDefaultPasswordAllowed();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsDefaultPasswordAllowed_Should_ReturnFalse_When_DevelopmentButDisabled()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
            _options.EnableInDevelopment = false;

            // Act
            var result = _service.IsDefaultPasswordAllowed();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsDefaultPasswordAllowed_Should_ReturnFalse_When_StagingEnvironment()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Staging);

            // Act
            var result = _service.IsDefaultPasswordAllowed();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsDefaultPasswordAllowed_Should_ReturnFalse_When_UnknownEnvironment()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Unknown");

            // Act
            var result = _service.IsDefaultPasswordAllowed();

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region IsDefaultPasswordAvailable Tests

        [Fact]
        public void IsDefaultPasswordAvailable_Should_ReturnFalse_When_NotAllowed()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);

            // Act
            var result = _service.IsDefaultPasswordAvailable(true);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsDefaultPasswordAvailable_Should_ReturnTrue_When_AllowedAndDatabaseEmpty()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
            _options.OnlyWhenDatabaseEmpty = true;

            // Act
            var result = _service.IsDefaultPasswordAvailable(true);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsDefaultPasswordAvailable_Should_ReturnFalse_When_AllowedButDatabaseNotEmpty()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
            _options.OnlyWhenDatabaseEmpty = true;

            // Act
            var result = _service.IsDefaultPasswordAvailable(false);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsDefaultPasswordAvailable_Should_ReturnTrue_When_AllowedAndNotRestrictedToEmptyDatabase()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
            _options.OnlyWhenDatabaseEmpty = false;

            // Act
            var result = _service.IsDefaultPasswordAvailable(false);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region GetConfigurationSummary Tests

        [Fact]
        public void GetConfigurationSummary_Should_ReturnCorrectSummary_When_DevelopmentEnvironment()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);

            // Act
            var result = _service.GetConfigurationSummary();

            // Assert
            result.Should().NotBeNull();
            result.IsProduction.Should().BeFalse();
            result.IsDevelopment.Should().BeTrue();
            result.IsDefaultPasswordAllowed.Should().BeTrue();
            result.EnableInDevelopment.Should().Be(_options.EnableInDevelopment);
            result.AllowInProduction.Should().Be(_options.AllowInProduction);
            result.OnlyWhenDatabaseEmpty.Should().Be(_options.OnlyWhenDatabaseEmpty);
            result.ExpiryDays.Should().Be(_options.ExpiryDays);
        }

        [Fact]
        public void GetConfigurationSummary_Should_ReturnCorrectSummary_When_ProductionEnvironment()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);

            // Act
            var result = _service.GetConfigurationSummary();

            // Assert
            result.Should().NotBeNull();
            result.IsProduction.Should().BeTrue();
            result.IsDevelopment.Should().BeFalse();
            result.IsDefaultPasswordAllowed.Should().BeFalse();
            result.EnableInDevelopment.Should().Be(_options.EnableInDevelopment);
            result.AllowInProduction.Should().Be(_options.AllowInProduction);
            result.OnlyWhenDatabaseEmpty.Should().Be(_options.OnlyWhenDatabaseEmpty);
            result.ExpiryDays.Should().Be(_options.ExpiryDays);
        }

        [Fact]
        public void GetConfigurationSummary_Should_AlwaysReturnNonNull_When_Called()
        {
            // Arrange
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Unknown");

            // Act
            var result = _service.GetConfigurationSummary();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<DefaultPasswordSummary>();
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public void Service_Should_HandleNullPasswordOptions_When_OptionsValueIsNull()
        {
            // Arrange
            _mockOptions.Setup(x => x.Value).Returns((DefaultPasswordOptions)null);

            // Act & Assert
            Assert.Throws<NullReferenceException>(() =>
                new DefaultPasswordService(_mockOptions.Object, _mockEnvironment.Object));
        }

        [Fact]
        public void Service_Should_HandleEmptyPasswordsInOptions_When_PasswordsAreEmpty()
        {
            // Arrange
            _options.SystemAdmin = "";
            _options.NewUser = "";
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);

            // Act
            var adminPassword = _service.GetSystemAdminPassword();
            var userPassword = _service.GetNewUserPassword();

            // Assert
            adminPassword.Should().Be("");
            userPassword.Should().Be("");
        }

        [Fact]
        public void Service_Should_HandleNullPasswordsInOptions_When_PasswordsAreNull()
        {
            // Arrange
            _options.SystemAdmin = null;
            _options.NewUser = null;
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);

            // Act
            var adminPassword = _service.GetSystemAdminPassword();
            var userPassword = _service.GetNewUserPassword();

            // Assert
            adminPassword.Should().BeNull();
            userPassword.Should().BeNull();
        }

        #endregion
    }

    public class DefaultPasswordSummaryTests
    {
        [Fact]
        public void DefaultPasswordSummary_Should_BeInitializable_When_Created()
        {
            // Act
            var summary = new DefaultPasswordSummary();

            // Assert
            summary.Should().NotBeNull();
            summary.IsProduction.Should().BeFalse();
            summary.IsDevelopment.Should().BeFalse();
            summary.IsDefaultPasswordAllowed.Should().BeFalse();
            summary.EnableInDevelopment.Should().BeFalse();
            summary.AllowInProduction.Should().BeFalse();
            summary.OnlyWhenDatabaseEmpty.Should().BeFalse();
            summary.ExpiryDays.Should().Be(0);
        }

        [Fact]
        public void DefaultPasswordSummary_Should_BeSettable_When_PropertiesAssigned()
        {
            // Arrange
            var summary = new DefaultPasswordSummary();

            // Act
            summary.IsProduction = true;
            summary.IsDevelopment = false;
            summary.IsDefaultPasswordAllowed = true;
            summary.EnableInDevelopment = true;
            summary.AllowInProduction = false;
            summary.OnlyWhenDatabaseEmpty = true;
            summary.ExpiryDays = 30;

            // Assert
            summary.IsProduction.Should().BeTrue();
            summary.IsDevelopment.Should().BeFalse();
            summary.IsDefaultPasswordAllowed.Should().BeTrue();
            summary.EnableInDevelopment.Should().BeTrue();
            summary.AllowInProduction.Should().BeFalse();
            summary.OnlyWhenDatabaseEmpty.Should().BeTrue();
            summary.ExpiryDays.Should().Be(30);
        }
    }
}