using System.ComponentModel.DataAnnotations;
using LYBT.Infrastructure.Configuration.Options;
using Xunit;
using FluentAssertions;

namespace LYBT.Infrastructure.Tests.Configuration.Options
{
    public class UserOptionsTests
    {
        [Fact]
        public void UserOptions_Should_HaveCorrectSectionName_When_Accessed()
        {
            // Act & Assert
            UserOptions.SectionName.Should().Be("UserOptions");
        }

        [Fact]
        public void UserOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new UserOptions();

            // Assert
            options.EnableUserCache.Should().BeTrue();
            options.UserCacheExpirationMinutes.Should().Be(30);
            options.MaxBatchOperationSize.Should().Be(100);
            options.EnableDetailedAuditLogging.Should().BeTrue();
            options.SendPasswordResetNotification.Should().BeFalse();
            options.SessionTimeoutMinutes.Should().Be(480);
            options.EnableOnlineStatusTracking.Should().BeTrue();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(30)]
        [InlineData(120)]
        [InlineData(1440)]
        public void UserCacheExpirationMinutes_Should_BeValid_When_InValidRange(int minutes)
        {
            // Arrange
            var options = new UserOptions { UserCacheExpirationMinutes = minutes };
            var context = new ValidationContext(options) { MemberName = nameof(UserOptions.UserCacheExpirationMinutes) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.UserCacheExpirationMinutes, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1441)]
        [InlineData(-1)]
        public void UserCacheExpirationMinutes_Should_BeInvalid_When_OutOfRange(int minutes)
        {
            // Arrange
            var options = new UserOptions { UserCacheExpirationMinutes = minutes };
            var context = new ValidationContext(options) { MemberName = nameof(UserOptions.UserCacheExpirationMinutes) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.UserCacheExpirationMinutes, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("用户缓存过期时间必须在1-1440分钟之间");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(500)]
        [InlineData(1000)]
        public void MaxBatchOperationSize_Should_BeValid_When_InValidRange(int size)
        {
            // Arrange
            var options = new UserOptions { MaxBatchOperationSize = size };
            var context = new ValidationContext(options) { MemberName = nameof(UserOptions.MaxBatchOperationSize) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.MaxBatchOperationSize, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1001)]
        [InlineData(-1)]
        public void MaxBatchOperationSize_Should_BeInvalid_When_OutOfRange(int size)
        {
            // Arrange
            var options = new UserOptions { MaxBatchOperationSize = size };
            var context = new ValidationContext(options) { MemberName = nameof(UserOptions.MaxBatchOperationSize) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.MaxBatchOperationSize, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("批量操作最大数量必须在1-1000之间");
        }

        [Theory]
        [InlineData(5)]
        [InlineData(60)]
        [InlineData(480)]
        [InlineData(1440)]
        public void SessionTimeoutMinutes_Should_BeValid_When_InValidRange(int minutes)
        {
            // Arrange
            var options = new UserOptions { SessionTimeoutMinutes = minutes };
            var context = new ValidationContext(options) { MemberName = nameof(UserOptions.SessionTimeoutMinutes) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.SessionTimeoutMinutes, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(4)]
        [InlineData(1441)]
        [InlineData(-1)]
        public void SessionTimeoutMinutes_Should_BeInvalid_When_OutOfRange(int minutes)
        {
            // Arrange
            var options = new UserOptions { SessionTimeoutMinutes = minutes };
            var context = new ValidationContext(options) { MemberName = nameof(UserOptions.SessionTimeoutMinutes) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.SessionTimeoutMinutes, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("用户会话超时时间必须在5-1440分钟之间");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BooleanProperties_Should_BeSettable_When_ValidBooleanProvided(bool value)
        {
            // Arrange
            var options = new UserOptions();

            // Act
            options.EnableUserCache = value;
            options.EnableDetailedAuditLogging = value;
            options.SendPasswordResetNotification = value;
            options.EnableOnlineStatusTracking = value;

            // Assert
            options.EnableUserCache.Should().Be(value);
            options.EnableDetailedAuditLogging.Should().Be(value);
            options.SendPasswordResetNotification.Should().Be(value);
            options.EnableOnlineStatusTracking.Should().Be(value);
        }

        [Fact]
        public void DefaultConfiguration_Should_BeOptimizedForPerformance_When_DefaultCreated()
        {
            // Arrange & Act
            var options = new UserOptions();

            // Assert
            options.EnableUserCache.Should().BeTrue("缓存应默认启用以提高性能");
            options.UserCacheExpirationMinutes.Should().BeGreaterThan(0).And.BeLessOrEqualTo(60, "缓存过期时间应合理");
            options.MaxBatchOperationSize.Should().BeGreaterThan(1).And.BeLessOrEqualTo(1000, "批量操作大小应在合理范围内");
        }

        [Fact]
        public void DefaultConfiguration_Should_BeSecurityAware_When_DefaultCreated()
        {
            // Arrange & Act
            var options = new UserOptions();

            // Assert
            options.SessionTimeoutMinutes.Should().BeGreaterThan(0).And.BeLessOrEqualTo(480, "会话超时应在安全范围内");
            options.EnableDetailedAuditLogging.Should().BeTrue("应默认启用详细审计日志");
            options.SendPasswordResetNotification.Should().BeFalse("默认应禁用密码重置通知避免信息泄露");
        }

        [Fact]
        public void DefaultConfiguration_Should_SupportMonitoring_When_DefaultCreated()
        {
            // Arrange & Act
            var options = new UserOptions();

            // Assert
            options.EnableOnlineStatusTracking.Should().BeTrue("应默认启用在线状态跟踪");
            options.EnableDetailedAuditLogging.Should().BeTrue("应默认启用详细审计日志");
        }

        [Fact]
        public void AllProperties_Should_BeSettable_When_ValidValuesProvided()
        {
            // Arrange
            var options = new UserOptions();

            // Act
            options.EnableUserCache = false;
            options.UserCacheExpirationMinutes = 60;
            options.MaxBatchOperationSize = 200;
            options.EnableDetailedAuditLogging = false;
            options.SendPasswordResetNotification = true;
            options.SessionTimeoutMinutes = 720;
            options.EnableOnlineStatusTracking = false;

            // Assert
            options.EnableUserCache.Should().BeFalse();
            options.UserCacheExpirationMinutes.Should().Be(60);
            options.MaxBatchOperationSize.Should().Be(200);
            options.EnableDetailedAuditLogging.Should().BeFalse();
            options.SendPasswordResetNotification.Should().BeTrue();
            options.SessionTimeoutMinutes.Should().Be(720);
            options.EnableOnlineStatusTracking.Should().BeFalse();
        }
    }
}