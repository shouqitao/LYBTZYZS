using System.ComponentModel.DataAnnotations;
using LYBT.Infrastructure.Configuration.Options;
using Xunit;
using FluentAssertions;

namespace LYBT.Infrastructure.Tests.Configuration.Options
{
    public class SysAdminOptionsTests
    {
        [Fact]
        public void SysAdminOptions_Should_HaveCorrectSectionName_When_Accessed()
        {
            // Act & Assert
            SysAdminOptions.SectionName.Should().Be("SysAdminOptions");
        }

        [Fact]
        public void SysAdminOptions_Should_HaveDefaultValues_When_Created()
        {
            // Act
            var options = new SysAdminOptions();

            // Assert
            options.Username.Should().Be("sysadmin");
            options.DefaultPassword.Should().Be("LybtAdmin2025@SecurePass!");
            options.RequirePasswordChangeOnFirstLogin.Should().BeTrue();
            options.EnableAccountLockout.Should().BeFalse();
        }

        [Theory]
        [InlineData("admin")]
        [InlineData("sysadmin")]
        [InlineData("administrator")]
        [InlineData("root")]
        [InlineData("superuser123456")]
        public void Username_Should_BeValid_When_ValidUsernameProvided(string username)
        {
            // Arrange
            var options = new SysAdminOptions { Username = username };
            var context = new ValidationContext(options) { MemberName = nameof(SysAdminOptions.Username) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.Username, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("ab")]
        [InlineData("this_username_is_way_too_long_and_exceeds_fifty_characters")]
        public void Username_Should_BeInvalid_When_InvalidUsernameProvided(string username)
        {
            // Arrange
            var options = new SysAdminOptions { Username = username };
            var context = new ValidationContext(options) { MemberName = nameof(SysAdminOptions.Username) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.Username, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().NotBeEmpty();
        }

        [Fact]
        public void Username_Should_RequireValue_When_NullProvided()
        {
            // Arrange
            var options = new SysAdminOptions { Username = null! };
            var context = new ValidationContext(options) { MemberName = nameof(SysAdminOptions.Username) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.Username, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("系统管理员用户名不能为空");
        }

        [Theory]
        [InlineData("ValidPassword123!")]
        [InlineData("SecureAdminPass@2025")]
        [InlineData("SuperStrongPwd#987")]
        public void DefaultPassword_Should_BeValid_When_ValidPasswordProvided(string password)
        {
            // Arrange
            var options = new SysAdminOptions { DefaultPassword = password };
            var context = new ValidationContext(options) { MemberName = nameof(SysAdminOptions.DefaultPassword) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.DefaultPassword, context, results);

            // Assert
            isValid.Should().BeTrue();
            results.Should().BeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("short")]
        [InlineData("1234567")]
        public void DefaultPassword_Should_BeInvalid_When_InvalidPasswordProvided(string password)
        {
            // Arrange
            var options = new SysAdminOptions { DefaultPassword = password };
            var context = new ValidationContext(options) { MemberName = nameof(SysAdminOptions.DefaultPassword) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.DefaultPassword, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().NotBeEmpty();
        }

        [Fact]
        public void DefaultPassword_Should_RequireValue_When_NullProvided()
        {
            // Arrange
            var options = new SysAdminOptions { DefaultPassword = null! };
            var context = new ValidationContext(options) { MemberName = nameof(SysAdminOptions.DefaultPassword) };

            // Act
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateProperty(options.DefaultPassword, context, results);

            // Assert
            isValid.Should().BeFalse();
            results.Should().HaveCount(1);
            results[0].ErrorMessage.Should().Be("系统管理员默认密码不能为空");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BooleanProperties_Should_BeSettable_When_ValidBooleanProvided(bool value)
        {
            // Arrange
            var options = new SysAdminOptions();

            // Act
            options.RequirePasswordChangeOnFirstLogin = value;
            options.EnableAccountLockout = value;

            // Assert
            options.RequirePasswordChangeOnFirstLogin.Should().Be(value);
            options.EnableAccountLockout.Should().Be(value);
        }

        [Fact]
        public void Validate_Should_NotThrow_When_ValidConfiguration()
        {
            // Arrange
            var options = new SysAdminOptions
            {
                Username = "administrator",
                DefaultPassword = "ValidPassword123!"
            };

            // Act & Assert
            var action = () => options.Validate();
            action.Should().NotThrow();
        }

        [Fact]
        public void Validate_Should_ThrowInvalidOperationException_When_UsernameIsEmpty()
        {
            // Arrange
            var options = new SysAdminOptions { Username = "" };

            // Act & Assert
            var action = () => options.Validate();
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("系统管理员用户名不能为空");
        }

        [Fact]
        public void Validate_Should_ThrowInvalidOperationException_When_UsernameIsWhitespace()
        {
            // Arrange
            var options = new SysAdminOptions { Username = "   " };

            // Act & Assert
            var action = () => options.Validate();
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("系统管理员用户名不能为空");
        }

        [Theory]
        [InlineData("ab")]
        [InlineData("this_username_is_definitely_way_too_long_and_exceeds_the_fifty_character_limit")]
        public void Validate_Should_ThrowInvalidOperationException_When_UsernameIsInvalidLength(string username)
        {
            // Arrange
            var options = new SysAdminOptions { Username = username };

            // Act & Assert
            var action = () => options.Validate();
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("系统管理员用户名长度必须在3-50字符之间");
        }

        [Fact]
        public void Validate_Should_ThrowInvalidOperationException_When_DefaultPasswordIsEmpty()
        {
            // Arrange
            var options = new SysAdminOptions { DefaultPassword = "" };

            // Act & Assert
            var action = () => options.Validate();
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("系统管理员默认密码不能为空");
        }

        [Fact]
        public void Validate_Should_ThrowInvalidOperationException_When_DefaultPasswordIsWhitespace()
        {
            // Arrange
            var options = new SysAdminOptions { DefaultPassword = "   " };

            // Act & Assert
            var action = () => options.Validate();
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("系统管理员默认密码不能为空");
        }

        [Theory]
        [InlineData("short")]
        [InlineData("1234567")]
        [InlineData("pwd")]
        public void Validate_Should_ThrowInvalidOperationException_When_DefaultPasswordIsTooShort(string password)
        {
            // Arrange
            var options = new SysAdminOptions { DefaultPassword = password };

            // Act & Assert
            var action = () => options.Validate();
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("系统管理员默认密码长度至少8个字符");
        }

        [Fact]
        public void SecurityConfiguration_Should_BeSecureByDefault_When_DefaultCreated()
        {
            // Arrange & Act
            var options = new SysAdminOptions();

            // Assert
            options.RequirePasswordChangeOnFirstLogin.Should().BeTrue("应要求首次登录时更改密码");
            options.EnableAccountLockout.Should().BeFalse("默认应禁用账户锁定以避免管理员被锁定");
            options.DefaultPassword.Should().NotBeNullOrEmpty().And.HaveLengthGreaterThanOrEqualTo(8, "默认密码应满足最小长度要求");
        }

        [Fact]
        public void DefaultPassword_Should_MeetComplexityRequirements_When_DefaultCreated()
        {
            // Arrange & Act
            var options = new SysAdminOptions();

            // Assert
            options.DefaultPassword.Should().MatchRegex(@"[A-Z]", "默认密码应包含大写字母");
            options.DefaultPassword.Should().MatchRegex(@"[a-z]", "默认密码应包含小写字母");
            options.DefaultPassword.Should().MatchRegex(@"\d", "默认密码应包含数字");
            options.DefaultPassword.Should().MatchRegex(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\?]", "默认密码应包含特殊字符");
        }
    }
}