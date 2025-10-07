using System.ComponentModel.DataAnnotations;
using LYBT.Infrastructure.Configuration.Options;
using Xunit;
using FluentAssertions;

namespace LYBT.Infrastructure.Tests.Configuration.Options
{
    public class AuthOptionsTests
    {
        [Fact]
        public void AuthOptions_Should_HaveCorrectSectionName_When_Accessed()
        {
            // Act & Assert
            AuthOptions.SectionName.Should().Be("AuthOptions");
        }

        [Fact]
        public void AuthOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new AuthOptions();

            // Assert
            options.MaxFailedLoginAttempts.Should().Be(5);
            options.AccountLockoutDuration.Should().Be(TimeSpan.FromMinutes(15));
            options.EnableDetailedLoginLogging.Should().BeTrue();
            options.SupportedLoginTypes.Should().ContainSingle("Password");
            options.PasswordPolicy.Should().NotBeNull();
            options.SessionOptions.Should().NotBeNull();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(100)]
        public void MaxFailedLoginAttempts_Should_BeValid_When_InValidRange(int attempts)
        {
            // Arrange
            var options = new AuthOptions { MaxFailedLoginAttempts = attempts };
            var context = new ValidationContext(options) { MemberName = nameof(AuthOptions.MaxFailedLoginAttempts) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.MaxFailedLoginAttempts, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        [InlineData(-1)]
        public void MaxFailedLoginAttempts_Should_BeInvalid_When_OutOfRange(int attempts)
        {
            // Arrange
            var options = new AuthOptions { MaxFailedLoginAttempts = attempts };
            var context = new ValidationContext(options) { MemberName = nameof(AuthOptions.MaxFailedLoginAttempts) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.MaxFailedLoginAttempts, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("最大登录失败次数必须在1-100之间");
        }

        [Fact]
        public void AccountLockoutDuration_Should_BeSettable_When_ValidTimeSpanProvided()
        {
            // Arrange
            var options = new AuthOptions();
            var duration = TimeSpan.FromMinutes(30);

            // Act
            options.AccountLockoutDuration = duration;

            // Assert
            options.AccountLockoutDuration.Should().Be(duration);
        }

        [Fact]
        public void EnableDetailedLoginLogging_Should_BeSettable_When_BooleanProvided()
        {
            // Arrange
            var options = new AuthOptions();

            // Act
            options.EnableDetailedLoginLogging = false;

            // Assert
            options.EnableDetailedLoginLogging.Should().BeFalse();
        }

        [Fact]
        public void SupportedLoginTypes_Should_BeModifiable_When_Accessed()
        {
            // Arrange
            var options = new AuthOptions();

            // Act
            options.SupportedLoginTypes.Add("OAuth");

            // Assert
            options.SupportedLoginTypes.Should().Contain("OAuth");
            options.SupportedLoginTypes.Should().HaveCount(2);
        }

        [Fact]
        public void PasswordPolicy_Should_NotBeNull_When_DefaultCreated()
        {
            // Arrange & Act
            var options = new AuthOptions();

            // Assert
            options.PasswordPolicy.Should().NotBeNull();
            options.PasswordPolicy.Should().BeOfType<PasswordPolicy>();
        }

        [Fact]
        public void SessionOptions_Should_NotBeNull_When_DefaultCreated()
        {
            // Arrange & Act
            var options = new AuthOptions();

            // Assert
            options.SessionOptions.Should().NotBeNull();
            options.SessionOptions.Should().BeOfType<SessionOptions>();
        }
    }

    public class PasswordPolicyTests
    {
        [Fact]
        public void PasswordPolicy_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var policy = new PasswordPolicy();

            // Assert
            policy.MinLength.Should().Be(8);
            policy.RequireUppercase.Should().BeTrue();
            policy.RequireLowercase.Should().BeTrue();
            policy.RequireDigit.Should().BeTrue();
            policy.RequireSpecialChar.Should().BeTrue();
            policy.PasswordHistoryCount.Should().Be(5);
            policy.PasswordExpireDays.Should().Be(90);
        }

        [Theory]
        [InlineData(4)]
        [InlineData(64)]
        [InlineData(128)]
        public void MinLength_Should_BeValid_When_InValidRange(int minLength)
        {
            // Arrange
            var policy = new PasswordPolicy { MinLength = minLength };
            var context = new ValidationContext(policy) { MemberName = nameof(PasswordPolicy.MinLength) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(policy.MinLength, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(3)]
        [InlineData(129)]
        [InlineData(0)]
        public void MinLength_Should_BeInvalid_When_OutOfRange(int minLength)
        {
            // Arrange
            var policy = new PasswordPolicy { MinLength = minLength };
            var context = new ValidationContext(policy) { MemberName = nameof(PasswordPolicy.MinLength) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(policy.MinLength, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("密码最小长度必须在4-128之间");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(25)]
        [InlineData(50)]
        public void PasswordHistoryCount_Should_BeValid_When_InValidRange(int historyCount)
        {
            // Arrange
            var policy = new PasswordPolicy { PasswordHistoryCount = historyCount };
            var context = new ValidationContext(policy) { MemberName = nameof(PasswordPolicy.PasswordHistoryCount) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(policy.PasswordHistoryCount, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(51)]
        public void PasswordHistoryCount_Should_BeInvalid_When_OutOfRange(int historyCount)
        {
            // Arrange
            var policy = new PasswordPolicy { PasswordHistoryCount = historyCount };
            var context = new ValidationContext(policy) { MemberName = nameof(PasswordPolicy.PasswordHistoryCount) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(policy.PasswordHistoryCount, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("密码历史记录数量必须在0-50之间");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(365)]
        [InlineData(3650)]
        public void PasswordExpireDays_Should_BeValid_When_InValidRange(int expireDays)
        {
            // Arrange
            var policy = new PasswordPolicy { PasswordExpireDays = expireDays };
            var context = new ValidationContext(policy) { MemberName = nameof(PasswordPolicy.PasswordExpireDays) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(policy.PasswordExpireDays, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3651)]
        public void PasswordExpireDays_Should_BeInvalid_When_OutOfRange(int expireDays)
        {
            // Arrange
            var policy = new PasswordPolicy { PasswordExpireDays = expireDays };
            var context = new ValidationContext(policy) { MemberName = nameof(PasswordPolicy.PasswordExpireDays) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(policy.PasswordExpireDays, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("密码过期天数必须在0-3650之间");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BooleanProperties_Should_BeSettable_When_ValidBooleanProvided(bool value)
        {
            // Arrange
            var policy = new PasswordPolicy();

            // Act
            policy.RequireUppercase = value;
            policy.RequireLowercase = value;
            policy.RequireDigit = value;
            policy.RequireSpecialChar = value;

            // Assert
            policy.RequireUppercase.Should().Be(value);
            policy.RequireLowercase.Should().Be(value);
            policy.RequireDigit.Should().Be(value);
            policy.RequireSpecialChar.Should().Be(value);
        }
    }

    public class SessionOptionsTests
    {
        [Fact]
        public void SessionOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new SessionOptions();

            // Assert
            options.TimeoutMinutes.Should().Be(30);
            options.SlidingExpiration.Should().BeTrue();
            options.AllowConcurrentSessions.Should().BeFalse();
            options.MaxConcurrentSessions.Should().Be(1);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(720)]
        [InlineData(1440)]
        public void TimeoutMinutes_Should_BeValid_When_InValidRange(int timeoutMinutes)
        {
            // Arrange
            var options = new SessionOptions { TimeoutMinutes = timeoutMinutes };
            var context = new ValidationContext(options) { MemberName = nameof(SessionOptions.TimeoutMinutes) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.TimeoutMinutes, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1441)]
        [InlineData(-1)]
        public void TimeoutMinutes_Should_BeInvalid_When_OutOfRange(int timeoutMinutes)
        {
            // Arrange
            var options = new SessionOptions { TimeoutMinutes = timeoutMinutes };
            var context = new ValidationContext(options) { MemberName = nameof(SessionOptions.TimeoutMinutes) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.TimeoutMinutes, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("会话超时时间必须在1-1440分钟之间");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void MaxConcurrentSessions_Should_BeValid_When_InValidRange(int maxSessions)
        {
            // Arrange
            var options = new SessionOptions { MaxConcurrentSessions = maxSessions };
            var context = new ValidationContext(options) { MemberName = nameof(SessionOptions.MaxConcurrentSessions) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.MaxConcurrentSessions, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(11)]
        [InlineData(-1)]
        public void MaxConcurrentSessions_Should_BeInvalid_When_OutOfRange(int maxSessions)
        {
            // Arrange
            var options = new SessionOptions { MaxConcurrentSessions = maxSessions };
            var context = new ValidationContext(options) { MemberName = nameof(SessionOptions.MaxConcurrentSessions) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.MaxConcurrentSessions, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("最大并发会话数必须在1-10之间");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BooleanProperties_Should_BeSettable_When_ValidBooleanProvided(bool value)
        {
            // Arrange
            var options = new SessionOptions();

            // Act
            options.SlidingExpiration = value;
            options.AllowConcurrentSessions = value;

            // Assert
            options.SlidingExpiration.Should().Be(value);
            options.AllowConcurrentSessions.Should().Be(value);
        }
    }
}