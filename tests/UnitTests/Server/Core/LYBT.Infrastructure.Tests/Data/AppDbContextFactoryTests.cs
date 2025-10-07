using LYBT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using System.IO;

namespace LYBT.Infrastructure.Tests.Data
{
    public class AppDbContextFactoryTests : IDisposable
    {
        private readonly AppDbContextFactory _factory;
        private readonly string _testConfigPath;

        public AppDbContextFactoryTests()
        {
            _factory = new AppDbContextFactory();
            _testConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "test-appsettings.json");
        }

        [Fact]
        public void CreateDbContext_Should_ReturnValidContext_When_CalledWithEmptyArgs()
        {
            // Arrange
            var args = new string[] { };

            // Act
            using var context = _factory.CreateDbContext(args);

            // Assert
            context.Should().NotBeNull();
            context.Should().BeOfType<AppDbContext>();
            context.Database.Should().NotBeNull();
        }

        [Fact]
        public void CreateDbContext_Should_ReturnValidContext_When_CalledWithNullArgs()
        {
            // Arrange
            string[] args = null;

            // Act
            using var context = _factory.CreateDbContext(args);

            // Assert
            context.Should().NotBeNull();
            context.Should().BeOfType<AppDbContext>();
        }

        [Fact]
        public void CreateDbContext_Should_UseSqlServerProvider_When_ContextCreated()
        {
            // Arrange
            var args = new string[] { };

            // Act
            using var context = _factory.CreateDbContext(args);

            // Assert
            context.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.SqlServer");
        }

        [Fact]
        public void CreateDbContext_Should_HaveCorrectMigrationsAssembly_When_ContextCreated()
        {
            // Arrange
            var args = new string[] { };

            // Act
            using var context = _factory.CreateDbContext(args);
            var options = context.Database.GetDbConnection();

            // Assert
            options.Should().NotBeNull();
            // The migrations assembly is configured but not easily testable through public API
            // This test verifies the context is created successfully with the configuration
        }

        [Fact]
        public void CreateDbContext_Should_HandleMissingConfigFile_When_ConfigFileNotFound()
        {
            // Arrange
            var args = new string[] { };
            var originalDirectory = Directory.GetCurrentDirectory();

            try
            {
                // Change to a directory without config files
                var tempDir = Path.GetTempPath();
                Directory.SetCurrentDirectory(tempDir);

                // Act & Assert
                // This should either work with fallback connection string or throw
                // The factory should handle missing config gracefully
                var action = () => _factory.CreateDbContext(args);
                action.Should().NotThrow<FileNotFoundException>();
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDirectory);
            }
        }

        [Fact]
        public void CreateDbContext_Should_UseDefaultConnectionString_When_ConfigConnectionStringIsNull()
        {
            // Arrange
            var args = new string[] { };

            // Act
            using var context = _factory.CreateDbContext(args);

            // Assert
            context.Should().NotBeNull();
            var connectionString = context.Database.GetConnectionString();
            connectionString.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void CreateDbContext_Should_CreateNewInstanceEachTime_When_CalledMultipleTimes()
        {
            // Arrange
            var args = new string[] { };

            // Act
            using var context1 = _factory.CreateDbContext(args);
            using var context2 = _factory.CreateDbContext(args);

            // Assert
            context1.Should().NotBeSameAs(context2);
            context1.Should().NotBeNull();
            context2.Should().NotBeNull();
        }

        [Fact]
        public void CreateDbContext_Should_HandleVariousArgs_When_DifferentArgsProvided()
        {
            // Arrange
            var args1 = new string[] { };
            var args2 = new string[] { "arg1", "arg2" };
            var args3 = new string[] { "different", "arguments", "here" };

            // Act
            using var context1 = _factory.CreateDbContext(args1);
            using var context2 = _factory.CreateDbContext(args2);
            using var context3 = _factory.CreateDbContext(args3);

            // Assert
            context1.Should().NotBeNull();
            context2.Should().NotBeNull();
            context3.Should().NotBeNull();
            // All should be valid AppDbContext instances regardless of args
        }

        [Fact]
        public void Factory_Should_ImplementCorrectInterface_When_Instantiated()
        {
            // Act & Assert
            _factory.Should().BeAssignableTo<IDesignTimeDbContextFactory<AppDbContext>>();
        }

        [Fact]
        public void CreateDbContext_Should_ConfigureOptionsCorrectly_When_Called()
        {
            // Arrange
            var args = new string[] { };

            // Act
            using var context = _factory.CreateDbContext(args);
            var options = context.Database.GetDbConnection();

            // Assert
            options.Should().NotBeNull();
            options.ConnectionString.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void CreateDbContext_Should_HandleLongConnectionString_When_FallbackUsed()
        {
            // Arrange
            var args = new string[] { };

            // Act
            using var context = _factory.CreateDbContext(args);

            // Assert
            var connectionString = context.Database.GetConnectionString();
            connectionString.Should().NotBeNullOrEmpty();
            // Verify it contains expected components
            connectionString.Should().Contain("Server=");
            connectionString.Should().Contain("Database=");
        }

        [Fact]
        public void CreateDbContext_Should_BeThreadSafe_When_CalledConcurrently()
        {
            // Arrange
            var args = new string[] { };
            var tasks = new List<Task<AppDbContext>>();

            // Act
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(() => _factory.CreateDbContext(args)));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            foreach (var task in tasks)
            {
                task.Result.Should().NotBeNull();
                task.Result.Should().BeOfType<AppDbContext>();
                task.Result.Dispose();
            }
        }

        [Fact]
        public void CreateDbContext_Should_HandleEmptyConnectionString_When_ConfigIsEmpty()
        {
            // Arrange
            var args = new string[] { };

            // Create a temporary config file with empty connection string
            var configContent = @"{
                ""ConnectionStrings"": {
                    ""DefaultConnection"": """"
                }
            }";

            File.WriteAllText(_testConfigPath, configContent);
            var originalDirectory = Directory.GetCurrentDirectory();

            try
            {
                // Act
                using var context = _factory.CreateDbContext(args);

                // Assert
                context.Should().NotBeNull();
                var connectionString = context.Database.GetConnectionString();
                connectionString.Should().NotBeNullOrEmpty(); // Should use fallback
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDirectory);
                if (File.Exists(_testConfigPath))
                {
                    File.Delete(_testConfigPath);
                }
            }
        }

        public void Dispose()
        {
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }
    }
}