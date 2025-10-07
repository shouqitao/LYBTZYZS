using System.ComponentModel.DataAnnotations;
using LYBT.Infrastructure.Configuration.Options;
using Xunit;
using FluentAssertions;

namespace LYBT.Infrastructure.Tests.Configuration.Options
{
    public class DefaultPasswordOptionsTests
    {
        [Fact]
        public void DefaultPasswordOptions_Should_HaveCorrectSectionName_When_Accessed()
        {
            // Act & Assert
            DefaultPasswordOptions.SectionName.Should().Be("DefaultPasswords");
        }

        [Fact]
        public void DefaultPasswordOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new DefaultPasswordOptions();

            // Assert
            options.SystemAdmin.Should().Be("LybtAdmin2025@SecurePass!");
            options.NewUser.Should().Be("LybtUser2025#InitPass!");
            options.EnableInDevelopment.Should().BeTrue();
            options.AllowInProduction.Should().BeFalse();
            options.OnlyWhenDatabaseEmpty.Should().BeTrue();
            options.ExpiryDays.Should().Be(30);
        }

        [Theory]
        [InlineData("ValidPassword123!")]
        [InlineData("SecurePass@2025")]
        [InlineData("StrongPwd#987")]
        public void SystemAdmin_Should_BeValid_When_ValidPasswordProvided(string password)
        {
            // Arrange
            var options = new DefaultPasswordOptions { SystemAdmin = password };
            var context = new ValidationContext(options) { MemberName = nameof(DefaultPasswordOptions.SystemAdmin) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.SystemAdmin, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("short")]
        [InlineData("1234567")]
        public void SystemAdmin_Should_BeInvalid_When_InvalidPasswordProvided(string password)
        {
            // Arrange
            var options = new DefaultPasswordOptions { SystemAdmin = password };
            var context = new ValidationContext(options) { MemberName = nameof(DefaultPasswordOptions.SystemAdmin) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.SystemAdmin, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().NotBeEmpty();
        }

        [Fact]
        public void SystemAdmin_Should_RequireValue_When_NullProvided()
        {
            // Arrange
            var options = new DefaultPasswordOptions { SystemAdmin = null! };
            var context = new ValidationContext(options) { MemberName = nameof(DefaultPasswordOptions.SystemAdmin) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.SystemAdmin, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("系统管理员默认密码不能为空");
        }

        [Theory]
        [InlineData("ValidPassword123!")]
        [InlineData("UserPass@2025")]
        [InlineData("DefaultPwd#456")]
        public void NewUser_Should_BeValid_When_ValidPasswordProvided(string password)
        {
            // Arrange
            var options = new DefaultPasswordOptions { NewUser = password };
            var context = new ValidationContext(options) { MemberName = nameof(DefaultPasswordOptions.NewUser) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.NewUser, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("weak")]
        [InlineData("1234567")]
        public void NewUser_Should_BeInvalid_When_InvalidPasswordProvided(string password)
        {
            // Arrange
            var options = new DefaultPasswordOptions { NewUser = password };
            var context = new ValidationContext(options) { MemberName = nameof(DefaultPasswordOptions.NewUser) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.NewUser, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().NotBeEmpty();
        }

        [Fact]
        public void NewUser_Should_RequireValue_When_NullProvided()
        {
            // Arrange
            var options = new DefaultPasswordOptions { NewUser = null! };
            var context = new ValidationContext(options) { MemberName = nameof(DefaultPasswordOptions.NewUser) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.NewUser, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("新建用户默认密码不能为空");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(30)]
        [InlineData(90)]
        [InlineData(365)]
        public void ExpiryDays_Should_BeValid_When_InValidRange(int days)
        {
            // Arrange
            var options = new DefaultPasswordOptions { ExpiryDays = days };
            var context = new ValidationContext(options) { MemberName = nameof(DefaultPasswordOptions.ExpiryDays) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.ExpiryDays, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(366)]
        [InlineData(-1)]
        public void ExpiryDays_Should_BeInvalid_When_OutOfRange(int days)
        {
            // Arrange
            var options = new DefaultPasswordOptions { ExpiryDays = days };
            var context = new ValidationContext(options) { MemberName = nameof(DefaultPasswordOptions.ExpiryDays) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.ExpiryDays, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("默认密码过期天数必须在1-365天之间");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BooleanProperties_Should_BeSettable_When_ValidBooleanProvided(bool value)
        {
            // Arrange
            var options = new DefaultPasswordOptions();

            // Act
            options.EnableInDevelopment = value;
            options.AllowInProduction = value;
            options.OnlyWhenDatabaseEmpty = value;

            // Assert
            options.EnableInDevelopment.Should().Be(value);
            options.AllowInProduction.Should().Be(value);
            options.OnlyWhenDatabaseEmpty.Should().Be(value);
        }

        [Fact]
        public void SecurityConfiguration_Should_BeSecureByDefault_When_DefaultCreated()
        {
            // Arrange & Act
            var options = new DefaultPasswordOptions();

            // Assert
            options.AllowInProduction.Should().BeFalse("生产环境应默认禁用默认密码");
            options.OnlyWhenDatabaseEmpty.Should().BeTrue("应仅在数据库为空时使用默认密码");
            options.ExpiryDays.Should().BeGreaterThan(0).And.BeLessOrEqualTo(90, "默认密码应有合理的过期时间");
        }

        [Fact]
        public void PasswordComplexity_Should_MeetSecurityRequirements_When_DefaultCreated()
        {
            // Arrange & Act
            var options = new DefaultPasswordOptions();

            // Assert
            options.DefaultUserPassword.Should().NotBeNullOrEmpty().And.HaveLengthGreaterThanOrEqualTo(8, "默认用户密码应满足最小长度要求");
            options.SystemAdmin.Should().MatchRegex(@"[A-Z]", "管理员密码应包含大写字母");
            options.SystemAdmin.Should().MatchRegex(@"[a-z]", "管理员密码应包含小写字母");
            options.SystemAdmin.Should().MatchRegex(@"\d", "管理员密码应包含数字");
            options.SystemAdmin.Should().MatchRegex(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\?]", "管理员密码应包含特殊字符");

            options.DefaultAdminPassword.Should().NotBeNullOrEmpty().And.HaveLengthGreaterThanOrEqualTo(8, "默认管理员密码应满足最小长度要求");
            options.NewUser.Should().MatchRegex(@"[A-Z]", "新用户密码应包含大写字母");
            options.NewUser.Should().MatchRegex(@"[a-z]", "新用户密码应包含小写字母");
            options.NewUser.Should().MatchRegex(@"\d", "新用户密码应包含数字");
            options.NewUser.Should().MatchRegex(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\?]", "新用户密码应包含特殊字符");
        }
    }
}