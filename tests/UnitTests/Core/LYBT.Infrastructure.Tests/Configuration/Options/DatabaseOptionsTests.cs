using System.ComponentModel.DataAnnotations;
using LYBT.Infrastructure.Configuration.Options;
using Xunit;
using FluentAssertions;

namespace LYBT.Infrastructure.Tests.Configuration.Options
{
    public class DatabaseOptionsTests
    {
        [Fact]
        public void DatabaseOptions_Should_HaveCorrectSectionName_When_Accessed()
        {
            // Act & Assert
            DatabaseOptions.SectionName.Should().Be("DatabaseOptions");
        }

        [Fact]
        public void DatabaseOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new DatabaseOptions();

            // Assert
            options.EnableAutoMigration.Should().BeTrue();
            options.EnableSensitiveDataLogging.Should().BeFalse();
            options.EnableDetailedErrors.Should().BeFalse();
            options.CommandTimeout.Should().Be(30);
            options.ConnectionPool.Should().NotBeNull();
            options.Monitoring.Should().NotBeNull();
            options.Backup.Should().NotBeNull();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(150)]
        [InlineData(300)]
        public void CommandTimeout_Should_BeValid_When_InValidRange(int timeout)
        {
            // Arrange
            var options = new DatabaseOptions { CommandTimeout = timeout };
            var context = new ValidationContext(options) { MemberName = nameof(DatabaseOptions.CommandTimeout) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.CommandTimeout, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(301)]
        [InlineData(-1)]
        public void CommandTimeout_Should_BeInvalid_When_OutOfRange(int timeout)
        {
            // Arrange
            var options = new DatabaseOptions { CommandTimeout = timeout };
            var context = new ValidationContext(options) { MemberName = nameof(DatabaseOptions.CommandTimeout) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.CommandTimeout, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("命令超时时间必须在1-300秒之间");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BooleanProperties_Should_BeSettable_When_ValidBooleanProvided(bool value)
        {
            // Arrange
            var options = new DatabaseOptions();

            // Act
            options.EnableAutoMigration = value;
            options.EnableSensitiveDataLogging = value;
            options.EnableDetailedErrors = value;

            // Assert
            options.EnableAutoMigration.Should().Be(value);
            options.EnableSensitiveDataLogging.Should().Be(value);
            options.EnableDetailedErrors.Should().Be(value);
        }
    }

    public class ConnectionPoolOptionsTests
    {
        [Fact]
        public void ConnectionPoolOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new ConnectionPoolOptions();

            // Assert
            options.MaxPoolSize.Should().Be(100);
            options.MinPoolSize.Should().Be(0);
            options.ConnectionLifetime.Should().Be(0);
            options.ConnectionTimeout.Should().Be(30);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(500)]
        [InlineData(1000)]
        public void MaxPoolSize_Should_BeValid_When_InValidRange(int maxPoolSize)
        {
            // Arrange
            var options = new ConnectionPoolOptions { MaxPoolSize = maxPoolSize };
            var context = new ValidationContext(options) { MemberName = nameof(ConnectionPoolOptions.MaxPoolSize) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.MaxPoolSize, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1001)]
        [InlineData(-1)]
        public void MaxPoolSize_Should_BeInvalid_When_OutOfRange(int maxPoolSize)
        {
            // Arrange
            var options = new ConnectionPoolOptions { MaxPoolSize = maxPoolSize };
            var context = new ValidationContext(options) { MemberName = nameof(ConnectionPoolOptions.MaxPoolSize) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.MaxPoolSize, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("最大连接池大小必须在1-1000之间");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        [InlineData(100)]
        public void MinPoolSize_Should_BeValid_When_InValidRange(int minPoolSize)
        {
            // Arrange
            var options = new ConnectionPoolOptions { MinPoolSize = minPoolSize };
            var context = new ValidationContext(options) { MemberName = nameof(ConnectionPoolOptions.MinPoolSize) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.MinPoolSize, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void MinPoolSize_Should_BeInvalid_When_OutOfRange(int minPoolSize)
        {
            // Arrange
            var options = new ConnectionPoolOptions { MinPoolSize = minPoolSize };
            var context = new ValidationContext(options) { MemberName = nameof(ConnectionPoolOptions.MinPoolSize) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.MinPoolSize, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("最小连接池大小必须在0-100之间");
        }
    }

    public class DatabaseMonitoringOptionsTests
    {
        [Fact]
        public void DatabaseMonitoringOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new DatabaseMonitoringOptions();

            // Assert
            options.EnablePerformanceMonitoring.Should().BeTrue();
            options.SlowQueryThreshold.Should().Be(1000);
            options.LogQueryStatistics.Should().BeTrue();
            options.EnableDeadlockDetection.Should().BeTrue();
        }

        [Theory]
        [InlineData(100)]
        [InlineData(30000)]
        [InlineData(60000)]
        public void SlowQueryThreshold_Should_BeValid_When_InValidRange(int threshold)
        {
            // Arrange
            var options = new DatabaseMonitoringOptions { SlowQueryThreshold = threshold };
            var context = new ValidationContext(options) { MemberName = nameof(DatabaseMonitoringOptions.SlowQueryThreshold) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.SlowQueryThreshold, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(99)]
        [InlineData(60001)]
        [InlineData(-1)]
        public void SlowQueryThreshold_Should_BeInvalid_When_OutOfRange(int threshold)
        {
            // Arrange
            var options = new DatabaseMonitoringOptions { SlowQueryThreshold = threshold };
            var context = new ValidationContext(options) { MemberName = nameof(DatabaseMonitoringOptions.SlowQueryThreshold) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.SlowQueryThreshold, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("慢查询阈值必须在100-60000毫秒之间");
        }
    }

    public class DatabaseBackupOptionsTests
    {
        [Fact]
        public void DatabaseBackupOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new DatabaseBackupOptions();

            // Assert
            options.EnableAutoBackup.Should().BeFalse();
            options.BackupInterval.Should().Be(24);
            options.BackupRetentionDays.Should().Be(30);
            options.BackupPath.Should().Be("Backups");
            options.CompressBackup.Should().BeTrue();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(84)]
        [InlineData(168)]
        public void BackupInterval_Should_BeValid_When_InValidRange(int interval)
        {
            // Arrange
            var options = new DatabaseBackupOptions { BackupInterval = interval };
            var context = new ValidationContext(options) { MemberName = nameof(DatabaseBackupOptions.BackupInterval) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.BackupInterval, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(169)]
        [InlineData(-1)]
        public void BackupInterval_Should_BeInvalid_When_OutOfRange(int interval)
        {
            // Arrange
            var options = new DatabaseBackupOptions { BackupInterval = interval };
            var context = new ValidationContext(options) { MemberName = nameof(DatabaseBackupOptions.BackupInterval) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.BackupInterval, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("备份间隔必须在1-168小时之间");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(180)]
        [InlineData(365)]
        public void BackupRetentionDays_Should_BeValid_When_InValidRange(int days)
        {
            // Arrange
            var options = new DatabaseBackupOptions { BackupRetentionDays = days };
            var context = new ValidationContext(options) { MemberName = nameof(DatabaseBackupOptions.BackupRetentionDays) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.BackupRetentionDays, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(366)]
        [InlineData(-1)]
        public void BackupRetentionDays_Should_BeInvalid_When_OutOfRange(int days)
        {
            // Arrange
            var options = new DatabaseBackupOptions { BackupRetentionDays = days };
            var context = new ValidationContext(options) { MemberName = nameof(DatabaseBackupOptions.BackupRetentionDays) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.BackupRetentionDays, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("备份保留天数必须在1-365天之间");
        }

        [Fact]
        public void BackupPath_Should_BeSettable_When_ValidStringProvided()
        {
            // Arrange
            var options = new DatabaseBackupOptions();
            var path = "C:\\DatabaseBackups";

            // Act
            options.BackupPath = path;

            // Assert
            options.BackupPath.Should().Be(path);
        }
    }
}