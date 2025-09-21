using System.ComponentModel.DataAnnotations;
using LYBT.Infrastructure.Configuration.Options;
using Xunit;
using FluentAssertions;

namespace LYBT.Infrastructure.Tests.Configuration.Options
{
    public class SecurityOptionsTests
    {
        [Fact]
        public void SecurityOptions_Should_HaveCorrectSectionName_When_Accessed()
        {
            // Act & Assert
            SecurityOptions.SectionName.Should().Be("Security");
        }

        [Fact]
        public void SecurityOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new SecurityOptions();

            // Assert
            options.Https.Should().NotBeNull();
            options.SecurityHeaders.Should().NotBeNull();
            options.PasswordPolicy.Should().NotBeNull();
            options.RateLimit.Should().NotBeNull();
            options.Environment.Should().NotBeNull();
        }
    }

    public class HttpsOptionsTests
    {
        [Fact]
        public void HttpsOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new HttpsOptions();

            // Assert
            options.RequireHttps.Should().BeFalse();
            options.HstsMaxAgeDays.Should().Be(365);
            options.HstsIncludeSubdomains.Should().BeTrue();
            options.HstsPreload.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(365)]
        [InlineData(3650)]
        public void HstsMaxAgeDays_Should_BeValid_When_InValidRange(int maxAgeDays)
        {
            // Arrange
            var options = new HttpsOptions { HstsMaxAgeDays = maxAgeDays };
            var context = new ValidationContext(options) { MemberName = nameof(HttpsOptions.HstsMaxAgeDays) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.HstsMaxAgeDays, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3651)]
        public void HstsMaxAgeDays_Should_BeInvalid_When_OutOfRange(int maxAgeDays)
        {
            // Arrange
            var options = new HttpsOptions { HstsMaxAgeDays = maxAgeDays };
            var context = new ValidationContext(options) { MemberName = nameof(HttpsOptions.HstsMaxAgeDays) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.HstsMaxAgeDays, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("HSTS最大存活天数必须在0-3650之间");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BooleanProperties_Should_BeSettable_When_ValidBooleanProvided(bool value)
        {
            // Arrange
            var options = new HttpsOptions();

            // Act
            options.RequireHttps = value;
            options.HstsIncludeSubdomains = value;
            options.HstsPreload = value;

            // Assert
            options.RequireHttps.Should().Be(value);
            options.HstsIncludeSubdomains.Should().Be(value);
            options.HstsPreload.Should().Be(value);
        }
    }

    public class SecurityHeadersOptionsTests
    {
        [Fact]
        public void SecurityHeadersOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new SecurityHeadersOptions();

            // Assert
            options.ContentSecurityPolicy.Should().Be("default-src 'self'");
            options.XFrameOptions.Should().Be("SAMEORIGIN");
            options.XContentTypeOptions.Should().Be("nosniff");
            options.ReferrerPolicy.Should().Be("strict-origin-when-cross-origin");
            options.PermissionsPolicy.Should().Be("camera=(), microphone=(), geolocation=()");
        }

        [Fact]
        public void SecurityHeadersProperties_Should_BeSettable_When_ValidValuesProvided()
        {
            // Arrange
            var options = new SecurityHeadersOptions();

            // Act
            options.ContentSecurityPolicy = "default-src 'none'";
            options.XFrameOptions = "DENY";
            options.XContentTypeOptions = "nosniff";
            options.ReferrerPolicy = "no-referrer";
            options.PermissionsPolicy = "camera=()";

            // Assert
            options.ContentSecurityPolicy.Should().Be("default-src 'none'");
            options.XFrameOptions.Should().Be("DENY");
            options.XContentTypeOptions.Should().Be("nosniff");
            options.ReferrerPolicy.Should().Be("no-referrer");
            options.PermissionsPolicy.Should().Be("camera=()");
        }
    }

    public class PasswordPolicyOptionsTests
    {
        [Fact]
        public void PasswordPolicyOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new PasswordPolicyOptions();

            // Assert
            options.MinLength.Should().Be(8);
            options.RequireUppercase.Should().BeTrue();
            options.RequireLowercase.Should().BeTrue();
            options.RequireDigit.Should().BeTrue();
            options.RequireSpecialChar.Should().BeTrue();
            options.ForbiddenPatterns.Should().NotBeNull();
            options.ForbiddenPatterns.Should().Contain("password");
            options.PasswordHistoryCount.Should().Be(5);
            options.PasswordExpiryDays.Should().Be(90);
        }

        [Theory]
        [InlineData(6)]
        [InlineData(64)]
        [InlineData(128)]
        public void MinLength_Should_BeValid_When_InValidRange(int minLength)
        {
            // Arrange
            var options = new PasswordPolicyOptions { MinLength = minLength };
            var context = new ValidationContext(options) { MemberName = nameof(PasswordPolicyOptions.MinLength) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.MinLength, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(129)]
        public void MinLength_Should_BeInvalid_When_OutOfRange(int minLength)
        {
            // Arrange
            var options = new PasswordPolicyOptions { MinLength = minLength };
            var context = new ValidationContext(options) { MemberName = nameof(PasswordPolicyOptions.MinLength) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.MinLength, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("密码最小长度必须在6-128之间");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(20)]
        public void PasswordHistoryCount_Should_BeValid_When_InValidRange(int historyCount)
        {
            // Arrange
            var options = new PasswordPolicyOptions { PasswordHistoryCount = historyCount };
            var context = new ValidationContext(options) { MemberName = nameof(PasswordPolicyOptions.PasswordHistoryCount) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.PasswordHistoryCount, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(21)]
        public void PasswordHistoryCount_Should_BeInvalid_When_OutOfRange(int historyCount)
        {
            // Arrange
            var options = new PasswordPolicyOptions { PasswordHistoryCount = historyCount };
            var context = new ValidationContext(options) { MemberName = nameof(PasswordPolicyOptions.PasswordHistoryCount) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.PasswordHistoryCount, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("密码历史记录数量必须在0-20之间");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(180)]
        [InlineData(365)]
        public void PasswordExpiryDays_Should_BeValid_When_InValidRange(int expiryDays)
        {
            // Arrange
            var options = new PasswordPolicyOptions { PasswordExpiryDays = expiryDays };
            var context = new ValidationContext(options) { MemberName = nameof(PasswordPolicyOptions.PasswordExpiryDays) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.PasswordExpiryDays, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(366)]
        public void PasswordExpiryDays_Should_BeInvalid_When_OutOfRange(int expiryDays)
        {
            // Arrange
            var options = new PasswordPolicyOptions { PasswordExpiryDays = expiryDays };
            var context = new ValidationContext(options) { MemberName = nameof(PasswordPolicyOptions.PasswordExpiryDays) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.PasswordExpiryDays, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("密码过期天数必须在0-365之间");
        }
    }

    public class RateLimitOptionsTests
    {
        [Fact]
        public void RateLimitOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new RateLimitOptions();

            // Assert
            options.Enabled.Should().BeTrue();
            options.General.Should().NotBeNull();
            options.Authentication.Should().NotBeNull();
            options.ApiKey.Should().NotBeNull();
        }

        [Fact]
        public void RateLimitRules_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new RateLimitOptions();

            // Assert
            options.General.RequestsPerMinute.Should().Be(100);
            options.Authentication.RequestsPerMinute.Should().Be(5);
            options.ApiKey.RequestsPerMinute.Should().Be(300);
        }
    }

    public class RateLimitRuleTests
    {
        [Fact]
        public void RateLimitRule_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var rule = new RateLimitRule();

            // Assert
            rule.RequestsPerMinute.Should().Be(60);
            rule.RequestsPerHour.Should().Be(1000);
            rule.RequestsPerDay.Should().Be(10000);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5000)]
        [InlineData(10000)]
        public void RequestsPerMinute_Should_BeValid_When_InValidRange(int requests)
        {
            // Arrange
            var rule = new RateLimitRule { RequestsPerMinute = requests };
            var context = new ValidationContext(rule) { MemberName = nameof(RateLimitRule.RequestsPerMinute) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(rule.RequestsPerMinute, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10001)]
        public void RequestsPerMinute_Should_BeInvalid_When_OutOfRange(int requests)
        {
            // Arrange
            var rule = new RateLimitRule { RequestsPerMinute = requests };
            var context = new ValidationContext(rule) { MemberName = nameof(RateLimitRule.RequestsPerMinute) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(rule.RequestsPerMinute, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("每分钟请求数必须在1-10000之间");
        }
    }

    public class EnvironmentOptionsTests
    {
        [Fact]
        public void EnvironmentOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new EnvironmentOptions();

            // Assert
            options.HideServerInfo.Should().BeFalse();
            options.HideDetailedErrors.Should().BeFalse();
            options.EnableSensitiveDataLogging.Should().BeFalse();
            options.AllowedHosts.Should().NotBeNull();
            options.AllowedHosts.Should().Contain("localhost");
            options.TrustedProxies.Should().NotBeNull();
            options.TrustedProxies.Should().Contain("127.0.0.1");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BooleanProperties_Should_BeSettable_When_ValidBooleanProvided(bool value)
        {
            // Arrange
            var options = new EnvironmentOptions();

            // Act
            options.HideServerInfo = value;
            options.HideDetailedErrors = value;
            options.EnableSensitiveDataLogging = value;

            // Assert
            options.HideServerInfo.Should().Be(value);
            options.HideDetailedErrors.Should().Be(value);
            options.EnableSensitiveDataLogging.Should().Be(value);
        }

        [Fact]
        public void ListProperties_Should_BeModifiable_When_Accessed()
        {
            // Arrange
            var options = new EnvironmentOptions();

            // Act
            options.AllowedHosts.Add("example.com");
            options.TrustedProxies.Add("192.168.1.1");

            // Assert
            options.AllowedHosts.Should().Contain("example.com");
            options.TrustedProxies.Should().Contain("192.168.1.1");
        }
    }
}