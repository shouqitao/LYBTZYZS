using System;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Configuration.Services;
using LYBT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Infrastructure.Tests.Data
{
    public class DatabaseInitializationServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ILogger<DatabaseInitializationService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<DefaultPasswordService> _mockPasswordService;
        private readonly DatabaseInitializationService _service;

        public DatabaseInitializationServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _mockLogger = new Mock<ILogger<DatabaseInitializationService>>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Mock DefaultPasswordService with minimal constructor
            _mockPasswordService = new Mock<DefaultPasswordService>(Mock.Of<IConfiguration>(), Mock.Of<ILogger<DefaultPasswordService>>());

            _service = new DatabaseInitializationService(
                _context,
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockPasswordService.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_CreateInstance_When_ValidDependenciesProvided()
        {
            // Arrange & Act
            var service = new DatabaseInitializationService(
                _context,
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockPasswordService.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_DbContextIsNull()
        {
            // Act & Assert
            var action = () => new DatabaseInitializationService(
                null!,
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockPasswordService.Object);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_LoggerIsNull()
        {
            // Act & Assert
            var action = () => new DatabaseInitializationService(
                _context,
                null!,
                _mockConfiguration.Object,
                _mockPasswordService.Object);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_ConfigurationIsNull()
        {
            // Act & Assert
            var action = () => new DatabaseInitializationService(
                _context,
                _mockLogger.Object,
                null!,
                _mockPasswordService.Object);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_PasswordServiceIsNull()
        {
            // Act & Assert
            var action = () => new DatabaseInitializationService(
                _context,
                _mockLogger.Object,
                _mockConfiguration.Object,
                null!);

            action.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region GetDatabaseInfoAsync Tests

        [Fact]
        public async Task GetDatabaseInfoAsync_Should_ReturnDatabaseInfo_When_DatabaseExists()
        {
            // Arrange
            await _context.Database.EnsureCreatedAsync();

            // Act
            var result = await _service.GetDatabaseInfoAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsConnected.Should().BeTrue();
            result.DatabaseName.Should().NotBeNullOrEmpty();
            result.AppliedMigrationsCount.Should().BeGreaterOrEqualTo(0);
            result.PendingMigrationsCount.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public async Task GetDatabaseInfoAsync_Should_ReturnDefaultInfo_When_DatabaseConnectionFails()
        {
            // Arrange - Use a disposed context to simulate connection failure
            _context.Dispose();

            // Act
            var result = await _service.GetDatabaseInfoAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsConnected.Should().BeFalse();
            result.DatabaseName.Should().Be("未知");
            result.AppliedMigrationsCount.Should().Be(0);
            result.PendingMigrationsCount.Should().Be(0);
            result.LastMigration.Should().BeNull();
        }

        [Fact]
        public async Task GetDatabaseInfoAsync_Should_LogError_When_ExceptionOccurs()
        {
            // Arrange - Use a disposed context to simulate exception
            _context.Dispose();

            // Act
            var result = await _service.GetDatabaseInfoAsync();

            // Assert
            result.Should().NotBeNull();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("获取数据库信息失败")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region DatabaseInfo Class Tests

        [Fact]
        public void DatabaseInfo_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var info = new DatabaseInfo();

            // Assert
            info.IsConnected.Should().BeFalse();
            info.DatabaseName.Should().Be(string.Empty);
            info.AppliedMigrationsCount.Should().Be(0);
            info.PendingMigrationsCount.Should().Be(0);
            info.LastMigration.Should().BeNull();
        }

        [Fact]
        public void DatabaseInfo_Should_AllowPropertySettings_When_ValidValuesProvided()
        {
            // Arrange
            var info = new DatabaseInfo();

            // Act
            info.IsConnected = true;
            info.DatabaseName = "TestDB";
            info.AppliedMigrationsCount = 5;
            info.PendingMigrationsCount = 2;
            info.LastMigration = "20231201_TestMigration";

            // Assert
            info.IsConnected.Should().BeTrue();
            info.DatabaseName.Should().Be("TestDB");
            info.AppliedMigrationsCount.Should().Be(5);
            info.PendingMigrationsCount.Should().Be(2);
            info.LastMigration.Should().Be("20231201_TestMigration");
        }

        #endregion

        #region Integration Tests with InMemory Database

        [Fact]
        public async Task InitializeDatabaseAsync_Should_CompleteSuccessfully_When_DatabaseDoesNotExist()
        {
            // Arrange
            _mockPasswordService.Setup(x => x.IsDefaultPasswordAvailable(It.IsAny<bool>())).Returns(false);

            // Act
            var act = async () => await _service.InitializeDatabaseAsync();

            // Assert
            await act.Should().NotThrowAsync();

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("开始数据库初始化检查")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeDatabaseAsync_Should_LogError_And_Rethrow_When_ExceptionOccurs()
        {
            // Arrange - Dispose context to force an exception
            _context.Dispose();

            // Act
            var act = async () => await _service.InitializeDatabaseAsync();

            // Assert
            await act.Should().ThrowAsync<Exception>();

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("数据库初始化失败")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeDatabaseAsync_Should_CreateAdminSecret_When_DatabaseIsEmpty_And_DefaultPasswordAvailable()
        {
            // Arrange
            await _context.Database.EnsureCreatedAsync();

            _mockPasswordService.Setup(x => x.IsDefaultPasswordAvailable(true)).Returns(true);
            _mockPasswordService.Setup(x => x.GetSystemAdminPassword()).Returns("TestPassword123!");

            // Act
            await _service.InitializeDatabaseAsync();

            // Assert
            var adminSecretId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var adminSecret = await _context.AdminSecrets.FirstOrDefaultAsync(x => x.Id == adminSecretId);

            adminSecret.Should().NotBeNull();
            adminSecret!.PasswordHash.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task InitializeDatabaseAsync_Should_NotCreateAdminSecret_When_DefaultPasswordNotAvailable()
        {
            // Arrange
            await _context.Database.EnsureCreatedAsync();

            _mockPasswordService.Setup(x => x.IsDefaultPasswordAvailable(It.IsAny<bool>())).Returns(false);
            _mockPasswordService.Setup(x => x.GetConfigurationSummary()).Returns(new DefaultPasswordService.ConfigurationSummary
            {
                IsProduction = true,
                IsDevelopment = false,
                IsDefaultPasswordAllowed = false
            });

            // Act
            await _service.InitializeDatabaseAsync();

            // Assert
            var adminSecretId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var adminSecret = await _context.AdminSecrets.FirstOrDefaultAsync(x => x.Id == adminSecretId);

            adminSecret.Should().BeNull();
        }

        [Fact]
        public async Task InitializeDatabaseAsync_Should_NotOverwriteExistingAdminSecret_When_AlreadyExists()
        {
            // Arrange
            await _context.Database.EnsureCreatedAsync();

            var adminSecretId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var existingAdminSecret = new AdminSecretModel
            {
                Id = adminSecretId,
                PasswordHash = "ExistingPasswordHash"
            };

            _context.AdminSecrets.Add(existingAdminSecret);
            await _context.SaveChangesAsync();

            _mockPasswordService.Setup(x => x.IsDefaultPasswordAvailable(It.IsAny<bool>())).Returns(true);
            _mockPasswordService.Setup(x => x.GetSystemAdminPassword()).Returns("NewPassword123!");

            // Act
            await _service.InitializeDatabaseAsync();

            // Assert
            var adminSecret = await _context.AdminSecrets.FirstOrDefaultAsync(x => x.Id == adminSecretId);

            adminSecret.Should().NotBeNull();
            adminSecret!.PasswordHash.Should().Be("ExistingPasswordHash"); // Should not be overwritten
        }

        [Fact]
        public async Task InitializeDatabaseAsync_Should_LogWarning_And_Continue_When_AdminSecretsInitializationFails()
        {
            // Arrange
            await _context.Database.EnsureCreatedAsync();

            _mockPasswordService.Setup(x => x.IsDefaultPasswordAvailable(It.IsAny<bool>()))
                .Throws(new InvalidOperationException("Test password service exception"));

            // Act
            var act = async () => await _service.InitializeDatabaseAsync();

            // Assert
            await act.Should().NotThrowAsync(); // Should not throw, just log warning

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("初始化AdminSecrets表时出现问题")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Database Empty Check Tests

        [Fact]
        public async Task InitializeDatabaseAsync_Should_DetectEmptyDatabase_When_NoBusinessDataExists()
        {
            // Arrange
            await _context.Database.EnsureCreatedAsync();

            // Ensure database is empty (no business data)
            _context.Users.RemoveRange(_context.Users);
            _context.Patients.RemoveRange(_context.Patients);
            _context.Consultations.RemoveRange(_context.Consultations);
            await _context.SaveChangesAsync();

            _mockPasswordService.Setup(x => x.IsDefaultPasswordAvailable(true)).Returns(true);
            _mockPasswordService.Setup(x => x.GetSystemAdminPassword()).Returns("TestPassword123!");

            // Act
            await _service.InitializeDatabaseAsync();

            // Assert
            // Verify that IsDefaultPasswordAvailable was called with true (indicating empty database)
            _mockPasswordService.Verify(x => x.IsDefaultPasswordAvailable(true), Times.Once);
        }

        [Fact]
        public async Task InitializeDatabaseAsync_Should_DetectNonEmptyDatabase_When_BusinessDataExists()
        {
            // Arrange
            await _context.Database.EnsureCreatedAsync();

            // Add some business data
            _context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = "hash",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Name = "Test User"
            });
            await _context.SaveChangesAsync();

            _mockPasswordService.Setup(x => x.IsDefaultPasswordAvailable(false)).Returns(false);

            // Act
            await _service.InitializeDatabaseAsync();

            // Assert
            // Verify that IsDefaultPasswordAvailable was called with false (indicating non-empty database)
            _mockPasswordService.Verify(x => x.IsDefaultPasswordAvailable(false), Times.Once);
        }

        #endregion

        #region Logging Verification Tests

        [Fact]
        public async Task InitializeDatabaseAsync_Should_LogSuccessfulCompletion_When_AllStepsSucceed()
        {
            // Arrange
            await _context.Database.EnsureCreatedAsync();
            _mockPasswordService.Setup(x => x.IsDefaultPasswordAvailable(It.IsAny<bool>())).Returns(false);

            // Act
            await _service.InitializeDatabaseAsync();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("数据库初始化完成")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}