using System.ComponentModel.DataAnnotations;
using LYBT.Infrastructure.Configuration.Options;
using Xunit;
using FluentAssertions;

namespace LYBT.Infrastructure.Tests.Configuration.Options
{
    public class JwtOptionsTests
    {
        [Fact]
        public void JwtOptions_Should_HaveCorrectSectionName_When_Accessed()
        {
            // Act & Assert
            JwtOptions.SectionName.Should().Be("JwtOptions");
        }

        [Fact]
        public void JwtOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new JwtOptions();

            // Assert
            options.Secret.Should().Be(string.Empty);
            options.Issuer.Should().Be("LYBT.WebAPI");
            options.Audience.Should().Be("LYBT.Client");
            options.ExpireMinutes.Should().Be(480);
            options.RememberMeExpireMinutes.Should().Be(43200);
            options.ClockSkewSeconds.Should().Be(300);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Secret_Should_BeInvalid_When_EmptyOrNull(string secret)
        {
            // Arrange
            var options = new JwtOptions { Secret = secret };
            var context = new ValidationContext(options) { MemberName = nameof(JwtOptions.Secret) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.Secret, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("JWT密钥不能为空");
        }

        [Theory]
        [InlineData("short")]
        [InlineData("1234567890123456789012345678901")] // 31 characters
        public void Secret_Should_BeInvalid_When_TooShort(string secret)
        {
            // Arrange
            var options = new JwtOptions { Secret = secret };
            var context = new ValidationContext(options) { MemberName = nameof(JwtOptions.Secret) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.Secret, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("JWT密钥长度至少32个字符");
        }

        [Theory]
        [InlineData("12345678901234567890123456789012")] // 32 characters
        [InlineData("123456789012345678901234567890123456789012345678901234567890")] // 60 characters
        public void Secret_Should_BeValid_When_LongEnough(string secret)
        {
            // Arrange
            var options = new JwtOptions { Secret = secret };
            var context = new ValidationContext(options) { MemberName = nameof(JwtOptions.Secret) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.Secret, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Issuer_Should_BeInvalid_When_EmptyOrNull(string issuer)
        {
            // Arrange
            var options = new JwtOptions { Issuer = issuer };
            var context = new ValidationContext(options) { MemberName = nameof(JwtOptions.Issuer) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.Issuer, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("JWT签发者不能为空");
        }

        [Theory]
        [InlineData("ValidIssuer")]
        [InlineData("LYBT.WebAPI")]
        [InlineData("Another.Valid.Issuer")]
        public void Issuer_Should_BeValid_When_NotEmpty(string issuer)
        {
            // Arrange
            var options = new JwtOptions { Issuer = issuer };
            var context = new ValidationContext(options) { MemberName = nameof(JwtOptions.Issuer) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.Issuer, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Audience_Should_BeInvalid_When_EmptyOrNull(string audience)
        {
            // Arrange
            var options = new JwtOptions { Audience = audience };
            var context = new ValidationContext(options) { MemberName = nameof(JwtOptions.Audience) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.Audience, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("JWT受众不能为空");
        }

        [Theory]
        [InlineData("ValidAudience")]
        [InlineData("LYBT.Client")]
        [InlineData("Another.Valid.Audience")]
        public void Audience_Should_BeValid_When_NotEmpty(string audience)
        {
            // Arrange
            var options = new JwtOptions { Audience = audience };
            var context = new ValidationContext(options) { MemberName = nameof(JwtOptions.Audience) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.Audience, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(480)]
        [InlineData(1440)]
        public void ExpireMinutes_Should_BeValid_When_InValidRange(int expireMinutes)
        {
            // Arrange
            var options = new JwtOptions { ExpireMinutes = expireMinutes };
            var context = new ValidationContext(options) { MemberName = nameof(JwtOptions.ExpireMinutes) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.ExpireMinutes, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1441)]
        [InlineData(-1)]
        public void ExpireMinutes_Should_BeInvalid_When_OutOfRange(int expireMinutes)
        {
            // Arrange
            var options = new JwtOptions { ExpireMinutes = expireMinutes };
            var context = new ValidationContext(options) { MemberName = nameof(JwtOptions.ExpireMinutes) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.ExpireMinutes, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("Token过期时间必须在1-1440分钟之间");
        }

        [Theory]
        [InlineData(1440)] // 1 day
        [InlineData(43200)] // 30 days
        [InlineData(525600)] // 1 year
        public void RememberMeExpireMinutes_Should_BeValid_When_InValidRange(int rememberMeExpireMinutes)
        {
            // Arrange
            var options = new JwtOptions { RememberMeExpireMinutes = rememberMeExpireMinutes };
            var context = new ValidationContext(options) { MemberName = nameof(JwtOptions.RememberMeExpireMinutes) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.RememberMeExpireMinutes, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(1439)] // Less than 1 day
        [InlineData(525601)] // More than 1 year
        [InlineData(0)]
        [InlineData(-1)]
        public void RememberMeExpireMinutes_Should_BeInvalid_When_OutOfRange(int rememberMeExpireMinutes)
        {
            // Arrange
            var options = new JwtOptions { RememberMeExpireMinutes = rememberMeExpireMinutes };
            var context = new ValidationContext(options) { MemberName = nameof(JwtOptions.RememberMeExpireMinutes) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.RememberMeExpireMinutes, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("记住我Token过期时间必须在1天-1年之间");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(300)]
        [InlineData(3600)]
        public void ClockSkewSeconds_Should_BeValid_When_InValidRange(int clockSkewSeconds)
        {
            // Arrange
            var options = new JwtOptions { ClockSkewSeconds = clockSkewSeconds };
            var context = new ValidationContext(options) { MemberName = nameof(JwtOptions.ClockSkewSeconds) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.ClockSkewSeconds, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(3601)]
        [InlineData(-1)]
        public void ClockSkewSeconds_Should_BeInvalid_When_OutOfRange(int clockSkewSeconds)
        {
            // Arrange
            var options = new JwtOptions { ClockSkewSeconds = clockSkewSeconds };
            var context = new ValidationContext(options) { MemberName = nameof(JwtOptions.ClockSkewSeconds) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.ClockSkewSeconds, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("时钟偏差必须在0-3600秒之间");
        }

        [Fact]
        public void AllProperties_Should_BeSettable_When_ValidValuesProvided()
        {
            // Arrange
            var options = new JwtOptions();
            var secret = "ThisIsAVeryLongSecretKeyForJWTTokenSigning123456";
            var issuer = "TestIssuer";
            var audience = "TestAudience";

            // Act
            options.Secret = secret;
            options.Issuer = issuer;
            options.Audience = audience;
            options.ExpireMinutes = 120;
            options.RememberMeExpireMinutes = 10080; // 7 days
            options.ClockSkewSeconds = 600; // 10 minutes

            // Assert
            options.Secret.Should().Be(secret);
            options.Issuer.Should().Be(issuer);
            options.Audience.Should().Be(audience);
            options.ExpireMinutes.Should().Be(120);
            options.RememberMeExpireMinutes.Should().Be(10080);
            options.ClockSkewSeconds.Should().Be(600);
        }
    }
}